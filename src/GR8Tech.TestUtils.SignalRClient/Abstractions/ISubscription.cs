using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GR8Tech.TestUtils.SignalRClient.Common.Contracts;

namespace GR8Tech.TestUtils.SignalRClient.Abstractions;

public interface ISubscription<T> : ISubscribable
{
    /// <summary>
    /// Name of the Subscription
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Subscription has internal buffer. By default it saves there all consumed messages.
    /// Your custom filter can define which messages to save to that buffer.
    /// </summary>
    /// <param name="key">Filter name</param>
    /// <param name="filter">A predicate for saving messages with some conditions</param>
    /// <example>
    /// <code>
    /// consumer.AddFilter("MySuperFilter", m =&gt; m.Message.Key = myEvent.Id);
    /// </code>
    /// </example>
    void AddFilter(string key, Func<MessageHolder<T>, bool> filter);

    /// <summary>
    /// Deletes your custom filter by name. Once there are no more custom buffers left, all new messages will be saved to internal buffer
    /// </summary>
    /// <param name="key">Filter name</param>
    void RemoveFilter(string key);

    /// <summary>
    /// Waits for no message to be found by predicate with defined timeout settings.
    /// Throws an Exception when the message found
    /// </summary>
    /// <param name="compare">Predicate for a message</param>
    /// <param name="retries">How many retries to do to make sure there is no message</param>
    /// <param name="interval">Delay TimeSpan between each retry</param>
    Task WaitForNoMessage(
        Func<MessageHolder<T>, bool> compare,
        int? retries = null,
        TimeSpan? interval = null);

    /// <summary>
    /// Waits and returns the first found message received by the Subscription that satisfy the predicate.
    /// Warning is logged when more than one message found based on your predicate
    /// </summary>
    /// <param name="compare">Predicate for a message</param>
    /// <param name="retries">How many retries to do to look for your message</param>
    /// <param name="interval">Delay TimeSpan between each retry</param>
    /// <returns>Returns the first found message received by the Subscription that satisfy the predicate or NULL after timeout</returns>
    Task<MessageHolder<T>> WaitAndGetMessage(
        Func<MessageHolder<T>, bool> compare,
        int? retries = null,
        TimeSpan? interval = null);

    /// <summary>
    /// Waits and returns messages received by the Subscription that satisfy the predicate and additional condition.
    /// When condition == null, then be aware that the first messages found that satisfy the predicate will be returned
    /// </summary>
    /// <param name="compare">Predicate for messages</param>
    /// <param name="condition">Additional condition for the result you expect</param>
    /// <param name="retries">How many retries to do to look for your messages and condition</param>
    /// <param name="interval">Delay TimeSpan between each retry</param>
    /// <returns>A set of messages received by the Subscription that satisfy the predicate or NULL after timeout</returns>
    /// <example>
    /// <code>
    /// await subscription.WaitAndGetMessages(
    ///     m => m.Message.Key == "1" || m.Message.Key == "2",
    ///     r => r.Count == 2);
    /// </code>
    /// </example>
    Task<List<MessageHolder<T>>> WaitAndGetMessages(
        Func<MessageHolder<T>, bool> compare,
        Func<List<MessageHolder<T>>, bool>? condition = null,
        int? retries = null,
        TimeSpan? interval = null);

    /// <summary>
    /// Removes all messages from the internal buffer
    /// </summary>
    void RemoveAllMessages();
    
    /// <summary>
    /// Removes messages from the internal buffer by your predicate
    /// </summary>
    /// <param name="compare"></param>
    void RemoveMessages(Func<MessageHolder<T>, bool> compare);
}