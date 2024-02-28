using GR8Tech.TestUtils.SignalRClient.Common.Logging;
using NUnit.Framework;
using Serilog;
using Tests.SetUpFixtures;

namespace Tests;

[SetUpFixture]
public static class GlobalSetUp
{
    private static ILogger _logger = SerilogDecorator.Logger.ForContextStaticClass(typeof(GlobalSetUp));
    internal static TestServiceFactory TestServiceFactory { get; private set; }

    [OneTimeSetUp]
    public static async Task SetUp()
    {
        Log.Logger = SerilogDecorator.Logger;
        TestServiceFactory = new TestServiceFactory();
        TestServiceFactory.CreateClient();
        
        _logger.Information("TestService has been started");
    }

    [OneTimeTearDown]
    public static void TearDown()
    {
        TestServiceFactory.Dispose();
        
        _logger.Information("Global OneTimeTearDown");
    }
}