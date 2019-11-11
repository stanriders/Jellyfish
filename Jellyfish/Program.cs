
namespace Jellyfish
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.GameLoop();
            }
        }


    }
}
