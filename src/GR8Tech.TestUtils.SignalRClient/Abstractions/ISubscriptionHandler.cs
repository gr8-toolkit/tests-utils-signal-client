using System.Collections.Generic;

namespace GR8Tech.TestUtils.SignalRClient.Abstractions
{
    public interface ISubscriptionHandler
    {
        /// <summary>
        /// Add a new Subscription to the HubClient, but you still need to active that subscription by calling Subscribe method of the subscription
        /// </summary>
        /// <param name="subscriptionName">A unic name of your subscription</param>
        /// <param name="methodName">Method name on the HubServer you would like to subscribe for</param>
        /// <param name="args">An array of arguments to have to pass for a method on the HubServer</param>
        /// <typeparam name="T">Returning type of a method from the HubServer</typeparam>
        /// <returns>Created subscription attached to this HubClient, but you still need to active it</returns>
        public ISubscription<T> AddSubscription<T>(string subscriptionName, string methodName, object[] args);
        
        /// <summary>
        /// Get Subscription by name from HubClient
        /// </summary>
        /// <param name="name">Subscription name</param>
        /// <typeparam name="T">Returning type of a method from the HubServer</typeparam>
        /// <returns>Found Subscription by name or generates an Exception in case if not</returns>
        public ISubscription<T>? GetSubscription<T>(string name);
        
        /// <summary>
        /// Get all Subscriptions names from HubClient. You then may pass them to GetSubscription() method for any logic you want
        /// </summary>
        /// <returns></returns>
        public ICollection<string> GetSubscriptionsNames();
        
        /// <summary>
        /// Unsubscribes first and the removes the Subscription
        /// </summary>
        /// <param name="name">Subscription name to remove</param>
        public void RemoveSubscription(string name);
    }
}