using Serilog;

namespace Jellyfish;

public class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        using var game = new Game();
        game.GameLoop();
    }
}