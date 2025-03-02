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
            Actions.Add(action);
        }

        public static void Run()
        {
            if (Actions.Count <= 0) 
                return;

            lock (Lock)
            {
                foreach (var action in Actions)
                {
                    action.Invoke();
                }

                Actions.Clear();
            }
        }
    }
}
