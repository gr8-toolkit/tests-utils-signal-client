using GR8Tech.TestUtils.SignalRClient.Common.Logging;
using Serilog;
using TestService.Hubs;

namespace TestService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = SerilogDecorator.Logger;
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
        builder.Services.AddSingleton(Log.Logger);

        builder.Services.AddSignalR()
            .AddJsonProtocol()
            .AddNewtonsoftJsonProtocol()
            .AddMessagePackProtocol();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<TestServiceHub>("/testServiceHub");

        app.Run();
    }
}