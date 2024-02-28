namespace GR8Tech.TestUtils.SignalRClient.Abstractions;

public interface ISubscribable
{
    /// <summary>
    /// Method name the Subscription is subscribed for
    /// </summary>
    string MethodName { get; }
    
    /// <summary>
    /// Arguments of the method the Subscription is subscribed for
    /// </summary>
    object[] Args { get; }
    
    /// <summary>
    /// The bool status of the Subscription
    /// </summary>
    bool IsSubscribed { get; }
    
    /// <summary>
    /// Activate the Subscription.
    /// Note: method not thread safe. It is assumed that it will be called only by one thread
    /// </summary>
    void Subscribe();
    
    /// <summary>
    /// Unsubscribe (stop) the Subscription
    /// </summary>
    void Unsubscribe();
}