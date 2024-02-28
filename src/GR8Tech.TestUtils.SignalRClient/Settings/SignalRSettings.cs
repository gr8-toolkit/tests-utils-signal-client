using System;
using System.Collections.Generic;
using GR8Tech.TestUtils.SignalRClient.Common;

namespace GR8Tech.TestUtils.SignalRClient.Settings
{
    internal class SignalRSettings
    {
        public ConnectionConfig? DefaultConnectionConfig { get; set; }
        public Dictionary<string, ConnectionConfig> Connections { get; set; }
    }

    public class ConnectionConfig
    {
        public string Url { get; set; }
        public MessageProtocol? MessageProtocol { get; set; }
        public TimeSpan? ServerTimeout { get; set; }
        public TimeSpan? HandshakeTimeout { get; set; }
        public TimeSpan? KeepAliveInterval { get; set; }
        public int? BufferSize { get; set; }
        public WaiterSettings? WaiterSettings { get; set; }
    }


    public class WaiterSettings
    {
        public int? RetryCount { get; set; }
        public TimeSpan? Interval { get; set; }
    }
}