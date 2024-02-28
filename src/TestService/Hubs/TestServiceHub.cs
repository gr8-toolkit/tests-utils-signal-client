using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace TestService.Hubs
{
    public class TestServiceHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Log.Information("User {ContextConnectionId} is connected", Context.ConnectionId);
            return Task.CompletedTask;
        }

        // Stream method for "Subscription"
        public ChannelReader<CurrencyRate> CurrencyRateStream(Currency currency)
        {
            var channel = Channel.CreateUnbounded<CurrencyRate>();

            Task.Run(async () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var obj = new CurrencyRate
                    {
                        Currency = currency,
                        Sell = 38 + i,
                        Buy = 37 + i
                    };

                    await Task.Delay(1500);
                    await channel.Writer.WriteAsync(obj);
                }

                channel.Writer.Complete();
            });

            return channel.Reader;
        }
    }

    public class CurrencyRate
    {
        public Currency Currency { get; set; }
        public double Sell { get; set; }
        public double Buy { get; set; }
    }

    public enum Currency
    {
        USD,
        EUR,
        UAH
    }
}