using System;
using Serilog;

namespace Jellyfish;

public sealed class Game : IDisposable
{
    private readonly MainWindow _mainWindow;

    public Game()
    {
        Log.Information("Loading...");
        EntityManager.Load();

        _mainWindow = new MainWindow(1920, 1080, "Game");
        _mainWindow.Load += OnWindowLoad;
    }

    public void Dispose()
    {
        _mainWindow?.Dispose();
    }

    private void OnWindowLoad()
    {
        Log.Information("Finished main window loading");
        MapParser.Parse("maps/test.yml");
    }

    public void GameLoop()
    {
        _mainWindow.Run();
    }
}