using Serilog;
using Serilog.Enrichers.WithCaller;

namespace Jellyfish;

public class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithCaller()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Caller}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        using var game = new MainWindow(1920, 1080, "Game");
        game.Run();
    }
}