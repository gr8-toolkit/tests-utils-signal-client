using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GR8Tech.TestUtils.SignalRClient.Abstractions;
using GR8Tech.TestUtils.SignalRClient.Common;
using GR8Tech.TestUtils.SignalRClient.Common.Contracts;
using GR8Tech.TestUtils.SignalRClient.Common.Logging;
using GR8Tech.TestUtils.SignalRClient.Settings;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace GR8Tech.TestUtils.SignalRClient.Implementation.Features;

internal class Subscription<T> : ISubscription<T>
{
    private ILogger _logger;
    private int _bufferSize;
    private int _waiterRetries;
    private TimeSpan _waiterInterval;
    private int _messageOffset;
    private HubConnection _hubConnection;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _subscriptionTask;
    private ConcurrentDictionary<string, Func<MessageHolder<T>, bool>> _filters = new();
    private ConcurrentDictionary<int, MessageHolder<T>> _messages = new();

    public string Name { get; }
    public string MethodName { get; }
    public object[] Args { get; }
    public bool IsSubscribed { get; private set; }

    internal Subscription(
        SubscriptionConfig subscriptionConfig,
        string subscriptionName,
        string methodName,
        object[] args)
    {
        _logger = Log.Logger
            .ForContext<Subscription<T>>()
            .AddPayload("HubConnectionId", subscriptionConfig.ConnectionId)
            .AddPayload("SubscriptionName", subscriptionName)
            .AddPayload("MethodName", methodName)
            .AddPrefix("HubClient/Subscription");

        var waiter = subscriptionConfig.WaiterSettings ?? new WaiterSettings();
        _waiterInterval = waiter.Interval ?? TimeSpan.FromSeconds(1);
        _waiterRetries = waiter.RetryCount ?? 30;
        _bufferSize = subscriptionConfig.BufferSize ?? 5000;
        _hubConnection = subscriptionConfig.HubConnection;
        _filters.TryAdd("default", _ => true);

        Name = subscriptionName;
        MethodName = methodName;
        Args = args;
    }

    public void Subscribe()
    {
        if (_hubConnection.State == HubConnectionState.Connected && IsSubscribed)
        {
            _logger.Warning("Subscription has already been started");

            return;
        }

        if (_hubConnection.State != HubConnectionState.Connected)
        {
            IsSubscribed = false;
            _logger.Warning("Client is not connected. Start connection first");

            return;
        }

        try
        {
            _messages = new ConcurrentDictionary<int, MessageHolder<T>>();
            _cancellationTokenSource = new CancellationTokenSource();
            _subscriptionTask = ReadStream(_cancellationTokenSource.Token);

            _logger.Information(
                "Subscription '{Name}' for a method '{MethodName}' has been started", Name, MethodName);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to Subscribe to a method '{MethodName}'", MethodName);
            throw;
        }

        IsSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            _logger.Warning("Cannot Unsubscribe as Client is not connected");
            IsSubscribed = false;

            return;
        }

        if (!IsSubscribed && (_subscriptionTask == null! || _subscriptionTask.IsCompleted))
        {
            _logger.Warning("Subscription has already been stopped");

            return;
        }

        _cancellationTokenSource.Cancel();
        _subscriptionTask.Wait(TimeSpan.FromSeconds(10));
        IsSubscribed = false;
    }

    public void AddFilter(string key, Func<MessageHolder<T>, bool> filter)
    {
        if (_filters.ContainsKey("default"))
        {
            if (_filters.TryRemove("default", out _))
                _logger.Information("Default filter has been deleted successfully");
            else
                _logger.Warning(
                    "Default filter was NOT deleted successfully. It might have been deleted by another thread");
        }

        _filters.AddOrUpdate(key, filter, (_, _) => filter);

        _logger.Information("Filter \"{filter}\" has been added successfully", key);
    }

    public void RemoveFilter(string key)
    {
        if (!_filters.ContainsKey(key)) return;

        if (_filters.TryRemove(key, out _))
            _logger.Information("Filter \"{name}\" has been deleted successfully", key);
        else
            _logger.Warning(
                "Filter \"{name}\" has NOT been deleted successfully. It might have been deleted by another thread",
                key);

        if (_filters.Count == 0)
            _filters.TryAdd("default", _ => true);
    }

    public async Task WaitForNoMessage(
        Func<MessageHolder<T>, bool> compare, int? retries = null, TimeSpan? interval = null)
    {
        var messages = (await WaitAndGetMessages(compare, null, retries, interval))?.ToArray();

        if (messages != null && messages.Any())
            throw new Exception($"{messages.Length} messages were found for the condition when should NOT have been");
    }

    public async Task<MessageHolder<T>> WaitAndGetMessage(
        Func<MessageHolder<T>, bool> compare, int? retries = null, TimeSpan? interval = null)
    {
        var messages = await WaitAndGetMessages(compare, null, retries, interval);

        if (messages != null! && messages.Any())
        {
            if (messages.Count > 1)
                _logger.Warning("Be aware that more than one message found for your condition");

            return messages.First();
        }

        return null!;
    }

    public async Task<List<MessageHolder<T>>> WaitAndGetMessages(
        Func<MessageHolder<T>, bool> compare,
        Func<List<MessageHolder<T>>, bool>? condition = null,
        int? retries = null,
        TimeSpan? interval = null)
    {
        retries ??= _waiterRetries;
        interval ??= _waiterInterval;

        if (condition == null)
            condition = _ => true;

        var policy = PollyPolicies.AsyncRetryPolicyWithExceptionAndResult<List<MessageHolder<T>>>(
            (int)retries, (TimeSpan)interval);

        return await policy.ExecuteAsync(async () =>
            {
                var result = _messages.Values.Where(compare).ToList();

                return (result.Any() && condition(result) ? result : default)!;
            }
        );
    }

    private int _bufferCounter;

    private int GetIndex()
    {
        Interlocked.CompareExchange(ref _bufferCounter, 0, _bufferSize);
        return Interlocked.Add(ref _bufferCounter, 1);
    }

    private async Task ReadStream(CancellationToken token)
    {
        try
        {
            var stream = _hubConnection.StreamAsyncCore<T>(MethodName, Args, token);

            await foreach (var item in stream)
            {
                _messageOffset++;

                if (_bufferSize == 0)
                {
                    _logger
                        .AddPayload("message", item)
                        .Information("Received a new message (skipped), offset: \"{offset}\"", _messageOffset);

                    continue;
                }

                var messageHolder = new MessageHolder<T>
                {
                    MessageOffset = _messageOffset,
                    Timestamp = DateTime.UtcNow,
                    Message = item
                };

                if (_filters.Any(filter => filter.Value(messageHolder)))
                {
                    _messages.AddOrUpdate(GetIndex(), messageHolder, (oldIndex, oldMessage) => messageHolder);

                    _logger
                        .AddPayload("message", item)
                        .Information("Received a new message (saved), offset: \"{offset}\"", _messageOffset);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Subscription was canceled. Received messages: {_messageCount}", _messageOffset);
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Subscription was unexpectedly stopped. Received messages: {_messageCount}",
                _messageOffset);
        }
        finally
        {
            IsSubscribed = false;
        }
    }

    public void RemoveAllMessages()
    {
        var count = _messages.Count;
        _messages.Clear();
        _logger.Information("All messages ({count}) have been deleted successfully from the buffer", count);
    }

    public void RemoveMessages(Func<MessageHolder<T>, bool> compare)
    {
        var messagesToRemove = _messages.Where(x => compare(x.Value));
        var count = 0;
        
        foreach (var message in messagesToRemove)
        {
            if (!_messages.TryRemove(message.Key, out _))
                _logger.Warning("Failed to delete a message from the buffer");
            else
                count++;
        }
        
        _logger.Information("Messages ({count}) have been deleted successfully from the buffer", count);
    }
}