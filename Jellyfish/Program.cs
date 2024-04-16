namespace Jellyfish;

public class Program
{
    private static void Main(string[] args)
    {
        using var game = new Game();
        game.GameLoop();
    }
}