using System;
using GR8Tech.TestUtils.SignalRClient.Settings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;

namespace GR8Tech.TestUtils.SignalRClient.Abstractions;

public interface ISignalRClientFactory
{
    /// <summary>
    /// Creates HubClient based on a connection name found in settings json file
    /// </summary>
    /// <param name="connectionName">Connection name from settings json file to take config from. If there is
    /// one connection then no need to set name, it will be treated as default connection</param>
    /// <param name="testServer">TestServer from WebApplicationFactory</param>
    /// <param name="jsonOptions">Action to change the config of Json serializer</param>
    /// <param name="newtonsoftJsonOptions">Action to change the config of NewtonsoftJson serializer</param>
    /// <param name="messagePackOptions">Action to change the config of MessagePack serializer</param>
    /// <returns>Created HubClient</returns>
    IHubClient CreateClient(
        string? connectionName = null,
        TestServer? testServer = null,
        Action<JsonHubProtocolOptions> jsonOptions = null!,
        Action<NewtonsoftJsonHubProtocolOptions> newtonsoftJsonOptions = null!,
        Action<MessagePackHubProtocolOptions> messagePackOptions = null!);

    /// <summary>
    /// Creates HubClient based on a connection config passed directly from the code, not from settings json file
    /// </summary>
    /// <param name="config">Connection config to build HubClient</param>
    /// <param name="testServer">TestServer from WebApplicationFactory</param>
    /// <param name="jsonOptions">Action to change the config of Json serializer</param>
    /// <param name="newtonsoftJsonOptions">Action to change the config of NewtonsoftJson serializer</param>
    /// <param name="messagePackOptions">Action to change the config of MessagePack serializer</param>
    /// <returns>Created HubClient</returns>
    IHubClient CreateClient(
        ConnectionConfig config,
        TestServer? testServer = null,
        Action<JsonHubProtocolOptions> jsonOptions = null!,
        Action<NewtonsoftJsonHubProtocolOptions> newtonsoftJsonOptions = null!,
        Action<MessagePackHubProtocolOptions> messagePackOptions = null!);
}