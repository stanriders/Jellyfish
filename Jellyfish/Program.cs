using Jellyfish.Console;
using Serilog;
using Log = Serilog.Log;

namespace Jellyfish;

public class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.GameConsole()
            .CreateLogger();

        using var game = new MainWindow();
        game.Run();
    }
}