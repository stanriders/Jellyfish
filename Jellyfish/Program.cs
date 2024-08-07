﻿using Serilog;

namespace Jellyfish;

public class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            //.MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        using var game = new MainWindow();
        game.Run();
    }
}