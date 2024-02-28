using System;

namespace GR8Tech.TestUtils.SignalRClient.Common.Contracts;

public sealed class MessageHolder<T>
{
    public int MessageOffset { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public T Message { get; set; }
}