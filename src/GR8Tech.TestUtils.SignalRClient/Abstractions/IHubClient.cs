using System;
using System.Threading.Tasks;

namespace GR8Tech.TestUtils.SignalRClient.Abstractions
{
    public interface IHubClient : ISubscriptionHandler, IDisposable
    {
        /// <summary>
        /// A unic Id (guid) for each connection of the HubClient. Each call to ConnectAsync() updates HubClientId, while each call to DisconnectAsync set HubClientId to NULL
        /// </summary>
        public string HubClientId { get; }

        /// <summary>
        /// Connect HubClient to the HubServer. HubClientId is updated right after a call of this method
        /// </summary>
        /// <returns></returns>
        public Task ConnectAsync();


        /// <summary>
        /// Disconnect HubClient from the HubServer. It removes all subscriptions and set HubClientId to NULL
        /// </summary>
        /// <returns></returns>
        public Task DisconnectAsync();
    }
}