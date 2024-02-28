using GR8Tech.TestUtils.SignalRClient.Settings;
using Microsoft.AspNetCore.SignalR.Client;

namespace GR8Tech.TestUtils.SignalRClient.Common.Contracts;

internal class SubscriptionConfig
{
    public HubConnection HubConnection { get; set; }
    public string ConnectionId { get; set; }
    public int? BufferSize { get; set; }
    public WaiterSettings? WaiterSettings { get; set; }
}