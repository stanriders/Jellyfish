
namespace Jellyfish
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var window = new MainWindow(1280, 720, "hi"))
            {
                window.Run();
            }
        }
    }
}
