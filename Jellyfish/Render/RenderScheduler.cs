using System;
using System.Collections.Generic;

namespace Jellyfish.Render
{
    public static class RenderScheduler
    {
        private static readonly List<Action> Actions = new();
        private static readonly object Lock = new();

        public static void Schedule(Action action)
        {
            lock (Lock)
            {
                Actions.Add(action);
            }
        }

        public static void Run()
        {
            lock (Lock)
            {
                if (Actions.Count <= 0)
                    return;

                foreach (var action in Actions)
                {
                    action.Invoke();
                }

                Actions.Clear();
            }
        }
    }
}
