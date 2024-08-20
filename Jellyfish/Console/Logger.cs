using Serilog;

namespace Jellyfish.Console
{
    public static class Log
    {
        public static ILogger Context(string context)
        {
            return Serilog.Log.Logger
                .ForContext("Context", context);
        }

        public static ILogger Context(object context)
        {
            return Serilog.Log.Logger
                .ForContext("Context", context.GetType().Name);
        }
    }

}
