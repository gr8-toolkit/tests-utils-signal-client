using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GR8Tech.TestUtils.SignalRClient.Abstractions;
using GR8Tech.TestUtils.SignalRClient.Common;
using GR8Tech.TestUtils.SignalRClient.Common.Logging;
using GR8Tech.TestUtils.SignalRClient.Implementation.Client;
using GR8Tech.TestUtils.SignalRClient.Settings;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GR8Tech.TestUtils.SignalRClient.Implementation;

public class SignalRClientFactory : ISignalRClientFactory
{
    private ILogger _logger;
    private readonly Dictionary<string, ConnectionConfig> _connections = new();

    public SignalRClientFactory()
    {
        _logger = Log.Logger
            .ForContext<SignalRClientFactory>()
            .AddPrefix($"{nameof(SignalRClientFactory)}");

        try
        {
            foreach (var connection in SettingsProvider.Config.Connections!)
                _connections.Add(connection.Key, connection.Value);
        }
        catch (Exception ex)
        {
            if (ex is TypeInitializationException || ex is FileNotFoundException)
                _logger.Warning("SignalRSettings is not defined in the setting json file");
            else
                throw;
        }
    }

    public IHubClient CreateClient(
        string? connectionName = null,
        TestServer? testServer = null,
        Action<JsonHubProtocolOptions> jsonOptions = null!,
        Action<NewtonsoftJsonHubProtocolOptions> newtonsoftJsonOptions = null!,
        Action<MessagePackHubProtocolOptions> messagePackOptions = null!)
    {
        if (string.IsNullOrEmpty(connectionName) && _connections.Count == 0)
            throw new Exception("There is no Connection defined in the factory. Create client with ConnectionConfig");

        if (string.IsNullOrEmpty(connectionName) && _connections.Count > 1)
            throw new Exception("There are more than 1 Connection defined in the factory. Please provide a name");

        if (_connections.Count > 1 && _connections.All(x => x.Key != connectionName))
            throw new Exception($"There is no '{connectionName}' Connection defined in the factory yet");

        ConnectionConfig config;

        if (string.IsNullOrEmpty(connectionName) && _connections.Count == 1)
            config = _connections.First(x => true).Value;
        else
            config = _connections.First(x => x.Key == connectionName).Value;

        return BuildHubClient(config, testServer, jsonOptions, newtonsoftJsonOptions, messagePackOptions);
    }

    public IHubClient CreateClient(
        ConnectionConfig config,
        TestServer? testServer = null,
        Action<JsonHubProtocolOptions> jsonOptions = null!,
        Action<NewtonsoftJsonHubProtocolOptions> newtonsoftJsonOptions = null!,
        Action<MessagePackHubProtocolOptions> messagePackOptions = null!)
    {
        return BuildHubClient(config, testServer, jsonOptions, newtonsoftJsonOptions, messagePackOptions);
    }

    private IHubClient BuildHubClient(
        ConnectionConfig config,
        TestServer? testServer = null,
        Action<JsonHubProtocolOptions> jsonOptions = null!,
        Action<NewtonsoftJsonHubProtocolOptions> newtonsoftJsonOptions = null!,
        Action<MessagePackHubProtocolOptions> messagePackOptions = null!)
    {
        var connectionBuilder = new HubConnectionBuilder();

        if (testServer == null)
            connectionBuilder.WithUrl(config.Url, opts =>
            {
                opts.Transports = HttpTransportType.WebSockets;
                opts.SkipNegotiation = true;
            });
        else
        {
            var configUri = new Uri(config.Url);
            var uri = new Uri(testServer.BaseAddress, configUri.AbsolutePath);

            connectionBuilder.WithUrl(uri, opts =>
            {
                opts.Transports = HttpTransportType.WebSockets;
                opts.SkipNegotiation = true;
                opts.HttpMessageHandlerFactory = _ => testServer.CreateHandler();
                opts.WebSocketFactory = async (context, cancellationToken) =>
                    await testServer.CreateWebSocketClient().ConnectAsync(context.Uri, cancellationToken);
            });
        }

        var protocol = config.MessageProtocol ?? MessageProtocol.Json;

        switch (protocol)
        {
            case MessageProtocol.Json:
                _ = jsonOptions == null!
                    ? connectionBuilder.AddJsonProtocol()
                    : connectionBuilder.AddJsonProtocol(jsonOptions);
                break;
            case MessageProtocol.NewtonsoftJson:
                _ = jsonOptions == null!
                    ? connectionBuilder.AddNewtonsoftJsonProtocol()
                    : connectionBuilder.AddNewtonsoftJsonProtocol(newtonsoftJsonOptions);
                break;
            case MessageProtocol.MessagePack:
                _ = jsonOptions == null!
                    ? connectionBuilder.AddMessagePackProtocol()
                    : connectionBuilder.AddMessagePackProtocol(messagePackOptions);
                break;
        }

        var connection = connectionBuilder.Build();
        connection.ServerTimeout = config.ServerTimeout ?? TimeSpan.FromMinutes(1);
        connection.HandshakeTimeout = config.HandshakeTimeout ?? TimeSpan.FromSeconds(10);
        connection.KeepAliveInterval = config.KeepAliveInterval ?? TimeSpan.FromSeconds(10);

        return new HubClient(connection, config.BufferSize, config.WaiterSettings);
    }
}