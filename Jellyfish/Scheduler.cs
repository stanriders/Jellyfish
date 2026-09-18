using System;
using System.Collections.Generic;
using System.Threading;

namespace Jellyfish
{
    public static class Scheduler
    {
        private static readonly List<Action> RenderActions = new();
        private static readonly Lock RenderLock = new();

        private static readonly List<Action> AudioActions = new();
        private static readonly Lock AudioLock = new();

        private static readonly List<Action> PhysicsActions = new();
        private static readonly Lock PhysicsLock = new();

        public static void RenderSchedule(Action action)
        {
            lock (RenderLock)
            {
                RenderActions.Add(action);
            }
        }

        public static void RenderRun()
        {
            lock (RenderLock)
            {
                if (RenderActions.Count <= 0)
                    return;

                foreach (var action in RenderActions)
                {
                    action.Invoke();
                }

                RenderActions.Clear();
            }
        }

        public static void AudioSchedule(Action action)
        {
            lock (AudioLock)
            {
                AudioActions.Add(action);
            }
        }

        public static void AudioRun()
        {
            lock (AudioLock)
            {
                if (AudioActions.Count <= 0)
                    return;

                foreach (var action in AudioActions)
                {
                    action.Invoke();
                }

                AudioActions.Clear();
            }
        }


        public static void PhysicsSchedule(Action action)
        {
            lock (PhysicsLock)
            {
                PhysicsActions.Add(action);
            }
        }

        public static void PhysicsRun()
        {
            lock (PhysicsLock)
            {
                if (PhysicsActions.Count <= 0)
                    return;

                foreach (var action in PhysicsActions)
                {
                    action.Invoke();
                }

                PhysicsActions.Clear();
            }
        }
    }
}
