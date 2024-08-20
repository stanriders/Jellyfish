using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Serilog;
using Serilog.Configuration;

namespace Jellyfish.Console
{
    public class ConsoleLine
    {
        public string Text { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public Color4 Color { get; set; }
        public string? Context { get; set; }
        public bool Unimportant { get; set; }
    }
    public class ConsoleSink : ILogEventSink
    {
        private readonly IFormatProvider? _formatProvider;

        public static readonly List<ConsoleLine> Buffer = new(1024);

        public ConsoleSink(IFormatProvider? formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            // TODO: colored params
            var message = logEvent.RenderMessage(_formatProvider);

            if (Buffer.Count >= 1024)
                Buffer.RemoveAt(0);

            Buffer.Add(new ConsoleLine
            {
                Text = message,
                Timestamp = logEvent.Timestamp.DateTime,
                Color = SeverityToColor(logEvent.Level),
                Context = logEvent.Properties.ContainsKey("Context") ? logEvent.Properties["Context"].ToString().Replace("\"", string.Empty) : null,
                Unimportant = logEvent.Level is LogEventLevel.Verbose or LogEventLevel.Debug
            });
        }

        private Color4 SeverityToColor(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => Color4.DimGray,
                LogEventLevel.Debug => Color4.DimGray,
                LogEventLevel.Information => Color4.DarkGray,
                LogEventLevel.Warning => Color4.Yellow,
                LogEventLevel.Error => Color4.Red,
                LogEventLevel.Fatal => Color4.Red,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
        }
    }

    public static class ConsoleSinkExtensions
    {
        public static LoggerConfiguration GameConsole(this LoggerSinkConfiguration loggerConfiguration, 
            IFormatProvider? formatProvider = null)
        {
            return loggerConfiguration.Sink(new ConsoleSink(formatProvider));
        }
    }
}
