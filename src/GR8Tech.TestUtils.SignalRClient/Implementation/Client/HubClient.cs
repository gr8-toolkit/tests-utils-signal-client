using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using GR8Tech.TestUtils.SignalRClient.Abstractions;
using GR8Tech.TestUtils.SignalRClient.Common.Contracts;
using GR8Tech.TestUtils.SignalRClient.Common.Logging;
using GR8Tech.TestUtils.SignalRClient.Implementation.Features;
using GR8Tech.TestUtils.SignalRClient.Settings;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace GR8Tech.TestUtils.SignalRClient.Implementation.Client
{
    internal class HubClient : IHubClient
    {
        private ILogger _logger;
        private int? _bufferSize;
        private WaiterSettings? _waiterSettings;
        private ConcurrentDictionary<string, ISubscribable> _subscriptions = new();
        private HubConnection Connection { get; set; }

        public string HubClientId { get; private set; }

        internal HubClient(HubConnection connection, int? bufferSize, WaiterSettings? waiterSettings)
        {
            InitializeLogger(HubClientId);

            Connection = connection;
            _bufferSize = bufferSize;
            _waiterSettings = waiterSettings;
        }

        private void InitializeLogger(string hubClientId)
        {
            _logger = Log.Logger
                .ForContext<HubClient>()
                .AddPrefix("HubClient")
                .AddHiddenPrefix(hubClientId);
        }

        public async Task ConnectAsync()
        {
            await Connection.StartAsync();
            HubClientId = Guid.NewGuid().ToString("N");
            InitializeLogger(HubClientId);

            _logger.Information("Client has connected");
        }

        public async Task DisconnectAsync()
        {
            if (!_subscriptions.IsEmpty)
            {
                foreach (var keyValuePair in _subscriptions)
                    RemoveSubscription(keyValuePair.Key);
            }

            await Connection.StopAsync();

            _logger.Information("Client has disconnected and removed all subscriptions");

            HubClientId = null!;
            InitializeLogger(HubClientId);
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
            Connection.DisposeAsync().AsTask().Wait();
        }

        public ISubscription<T> AddSubscription<T>(string subscriptionName, string methodName, object[] args)
        {
            var subscriptionConfig = new SubscriptionConfig
            {
                HubConnection = Connection,
                ConnectionId = HubClientId,
                BufferSize = _bufferSize,
                WaiterSettings = _waiterSettings
            };

            var subscription = new Subscription<T>(subscriptionConfig, subscriptionName, methodName, args);

            _subscriptions.AddOrUpdate(
                subscriptionName,
                _ => subscription,
                (_, _) => subscription);

            _logger.Information("New subscription \"{subscriptionName}\" has been added", subscriptionName);

            return subscription;
        }

        public ISubscription<T>? GetSubscription<T>(string name)
        {
            return _subscriptions[name] as ISubscription<T>;
        }

        public ICollection<string> GetSubscriptionsNames()
        {
            return _subscriptions.Keys;
        }

        public void RemoveSubscription(string name)
        {
            if (!_subscriptions.ContainsKey(name)) return;

            if (_subscriptions.TryRemove(name, out ISubscribable? subscription))
            {
                subscription?.Unsubscribe();

                _logger.Information("Subscription \"{name}\" has been deleted successfully", name);
            }
            else
                _logger.Warning(
                    "Subscription \"{name}\" has NOT been deleted successfully. It might have been deleted by another thread",
                    name);
        }
    }
}