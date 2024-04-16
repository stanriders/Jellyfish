using System;

namespace Jellyfish;

public class Game : IDisposable
{
    private readonly MainWindow _mainWindow;

    public Game()
    {
        _mainWindow = new MainWindow(1280, 720, "Game");
        _mainWindow.Load += OnWindowLoad;
    }

    public void Dispose()
    {
        _mainWindow?.Dispose();
    }

    private void OnWindowLoad()
    {
        MapParser.Parse("maps/test.yml");
    }

    public void GameLoop()
    {
        _mainWindow.Run();
    }
}