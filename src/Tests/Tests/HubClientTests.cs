using GR8Tech.TestUtils.SignalRClient.Abstractions;
using GR8Tech.TestUtils.SignalRClient.Common;
using GR8Tech.TestUtils.SignalRClient.Common.Logging;
using GR8Tech.TestUtils.SignalRClient.Implementation;
using GR8Tech.TestUtils.SignalRClient.Settings;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Tests.SetUpFixtures;
using TestService.Hubs;
using ILogger = Serilog.ILogger;

namespace Tests.Tests;

public class HubClientTests : TestBase
{
    private ILogger _logger = SerilogDecorator.Logger.ForContext<HubClientTests>();
    private ISignalRClientFactory _signalRClientFactory = new SignalRClientFactory();
    private TestServer _testServer = GlobalSetUp.TestServiceFactory.Server;

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Create_HubClient_from_settings_file_and_connect_and_disconnect()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);

        // Act 
        await hubClient.ConnectAsync();

        // Assert
        Assert.That(hubClient.HubClientId, Is.Not.Empty);
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Create_HubClient_from_code_and_connect_and_disconnect()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            Url = "http://localhost:5026/testServiceHub"
        };
        var hubClient = _signalRClientFactory.CreateClient(config: connectionConfig, testServer: _testServer);

        // Act 
        await hubClient.ConnectAsync();

        // Assert
        Assert.That(hubClient.HubClientId, Is.Not.Empty);
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Create_HubClient_with_NewtonsoftJson_serialization()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            Url = "http://localhost:5026/testServiceHub",
            MessageProtocol = MessageProtocol.NewtonsoftJson
        };
        var hubClient = _signalRClientFactory.CreateClient(config: connectionConfig, testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessage(x => x.Message.Sell == 39d);

        // Assert
        Assert.That(result.Message.Sell, Is.EqualTo(39d));
        Assert.That(hubClient.HubClientId, Is.Not.Empty);
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Create_HubClient_with_MessagePack_serialization()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            Url = "http://localhost:5026/testServiceHub",
            MessageProtocol = MessageProtocol.MessagePack
        };
        var hubClient = _signalRClientFactory.CreateClient(config: connectionConfig, testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessage(x => x.Message.Sell == 39d);

        // Assert
        Assert.That(result.Message.Sell, Is.EqualTo(39d));
        Assert.That(hubClient.HubClientId, Is.Not.Empty);
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task ConnectionId_is_changed_after_reconnection()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);

        // Act 
        await hubClient.ConnectAsync();
        var firstConnectionId = hubClient.HubClientId;
        Assert.That(firstConnectionId, Is.Not.Empty);
        await hubClient.DisconnectAsync();

        await hubClient.ConnectAsync();
        var secondConnectionId = hubClient.HubClientId;

        // Assert
        Assert.That(secondConnectionId, Is.Not.EqualTo(firstConnectionId));
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_subscription_and_get_it()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        var subscription = hubClient.GetSubscription<CurrencyRate>("USDRate");
        subscription!.Subscribe();
        var result = await subscription.WaitAndGetMessage(x => x.Message.Currency == Currency.USD);

        // Assert
        Assert.That(result, Is.Not.Null);
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_two_subscriptions_and_get_their_names_to_delete()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName_1 = "USDRate_1";
        var subName_2 = "USDRate_2";
        var methodName = "CurrencyRateStream";

        // Act 
        hubClient.AddSubscription<CurrencyRate>(subName_1, methodName, [Currency.USD]);
        hubClient.AddSubscription<CurrencyRate>(subName_2, methodName, [Currency.USD]);
        var names = hubClient.GetSubscriptionsNames();
        Assert.That(names.Count, Is.EqualTo(2));
        names.ToList().ForEach(x => hubClient.RemoveSubscription(x));

        // Assert
        Assert.That(hubClient.GetSubscriptionsNames().Count, Is.EqualTo(0));
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_subscription_and_wait_for_all_messages()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessages(
            x => x.Message.Currency == Currency.USD,
            r => r.Count == 3);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_subscription_and_wait_for_one_specific_message()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";
        var args = new object[] { Currency.USD };

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, args);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessage(x => x.Message.Sell == 39d);

        // Assert
        Assert.Multiple((() =>
        {
            Assert.That(subscription.IsSubscribed, Is.True);
            Assert.That(subscription.Name, Is.EqualTo(subName));
            Assert.That(subscription.MethodName, Is.EqualTo(methodName));
            Assert.That(subscription.Args, Is.EqualTo(args));
            Assert.That(result.Message.Sell, Is.EqualTo(39d));
        }));

        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_filter_to_the_subscription()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.AddFilter("myFilter", messageHolder => messageHolder.Message.Sell == 39d);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessages(x => x.Message.Currency == Currency.USD);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Message.Sell, Is.EqualTo(39d));
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_filter_to_the_subscription_and_then_remove_it_and_check_default_filter()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";
        var filterName = "myFilter";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.AddFilter(filterName, messageHolder => messageHolder.Message.Sell == 39d);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessages(
            x => x.Message.Currency == Currency.USD,
            retries: 40,
            interval: TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Message.Sell, Is.EqualTo(39d));

        // Act 
        subscription.RemoveFilter(filterName);
        result = await subscription.WaitAndGetMessages(
            x => x.Message.Currency == Currency.USD,
            r => r.Count == 2);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_subscription_and_wait_for_no_message()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.Subscribe();

        // Assert
        await subscription.WaitForNoMessage(
            x => x.Message.Sell == 42d,
            6, TimeSpan.FromSeconds(1));
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_subscription_and_wait_for_all_message_and_remove_all_from_buffer()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessages(
            x => x.Message.Currency == Currency.USD,
            r => r.Count == 3);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));

        // Act 
        subscription.RemoveAllMessages();
        result = await subscription.WaitAndGetMessages(
            x => x.Message.Currency == Currency.USD,
            r => r.Count == 3, 2, TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(result, Is.Null);
        await hubClient.DisconnectAsync();
    }

    [Test]
    public async Task Add_subscription_and_wait_for_all_message_and_remove_one_message_from_buffer()
    {
        // Arrange
        var hubClient = _signalRClientFactory.CreateClient(testServer: _testServer);
        await hubClient.ConnectAsync();
        var subName = "USDRate";
        var methodName = "CurrencyRateStream";

        // Act 
        var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, [Currency.USD]);
        subscription.Subscribe();
        var result = await subscription.WaitAndGetMessages(
            x => x.Message.Currency == Currency.USD,
            r => r.Count == 3);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));

        // Act 
        subscription.RemoveMessages(x => x.Message.Sell == 39d);
        result = await subscription.WaitAndGetMessages(
            x => x.Message.Currency == Currency.USD,
            r => r.Count == 2, 2, TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        await hubClient.DisconnectAsync();
    }
}