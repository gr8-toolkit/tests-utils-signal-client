using System;
using System.IO;
using GR8Tech.TestUtils.SignalRClient.Common.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace GR8Tech.TestUtils.SignalRClient.Settings
{
    internal static class SettingsProvider
    {
        static SettingsProvider()
        {
            var configFileName = File.Exists("test-settings.json") ? "test-settings" : "appsettings";
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"{configFileName}.json", false, true)
                .AddJsonFile(
                    $"{configFileName}.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .AddEnvironmentVariables()
                .Build()
                .GetSection("SignalRSettings")
                .Get<SignalRSettings>()!
                .SetCascadeSettings();

            Log.Logger
                .ForContextStaticClass(typeof(SettingsProvider))
                .AddPayload("SignalRSettings", Config)
                .Information("SignalRSettings have been read and initialized");
        }

        internal static SignalRSettings Config { get; }

        private static SignalRSettings SetCascadeSettings(this SignalRSettings settings)
        {
            var defaultConnectionConfig = settings.DefaultConnectionConfig ?? new ConnectionConfig();
            var defaultWaiterOptions = defaultConnectionConfig.WaiterSettings ?? new WaiterSettings();

            foreach (var connection in settings.Connections)
            {
                connection.Value.WaiterSettings ??= defaultWaiterOptions;
                connection.Value.WaiterSettings.RetryCount ??= defaultWaiterOptions.RetryCount;
                connection.Value.WaiterSettings.Interval ??= defaultWaiterOptions.Interval;
                connection.Value.BufferSize ??= defaultConnectionConfig.BufferSize;
                connection.Value.MessageProtocol ??= defaultConnectionConfig.MessageProtocol;
                connection.Value.ServerTimeout ??= defaultConnectionConfig.ServerTimeout;
                connection.Value.HandshakeTimeout ??= defaultConnectionConfig.HandshakeTimeout;
                connection.Value.KeepAliveInterval ??= defaultConnectionConfig.KeepAliveInterval;
            }

            return settings;
        }
    }
}