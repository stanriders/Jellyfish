using System;

namespace Jellyfish
{
    class Game : IDisposable
    {
        private readonly MainWindow mainWindow;

        public Game()
        {
            mainWindow = new MainWindow(1280,720, "Game");
            mainWindow.Load += OnWindowLoad;
        }

        private void OnWindowLoad(object sender, EventArgs eventArgs)
        {
            MapParser.Parse("maps/test.yml");
        }

        public void GameLoop()
        {
            mainWindow.Run();
        }

        public void Dispose()
        {
            mainWindow?.Dispose();
        }
    }
}
