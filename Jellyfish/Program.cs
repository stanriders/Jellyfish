using Serilog;

namespace Jellyfish;

public class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        using var game = new MainWindow(1920, 1080, "Game");
        game.Run();
    }
}