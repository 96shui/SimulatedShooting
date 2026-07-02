using System;
using System.Collections.Generic;

namespace VRShooting.Application
{
    public sealed class GameEventBus : IGameEventBus
    {
        readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        public void Publish<TEvent>(TEvent evt)
        {
            if (!handlers.TryGetValue(typeof(TEvent), out var list) || list.Count == 0)
            {
                return;
            }

            var snapshot = list.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                ((Action<TEvent>)snapshot[i])(evt);
            }
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);
            if (!handlers.TryGetValue(eventType, out var list))
            {
                list = new List<Delegate>();
                handlers[eventType] = list;
            }

            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        sealed class Subscription : IDisposable
        {
            readonly Action unsubscribe;

            public Subscription(Action unsubscribe)
            {
                this.unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                unsubscribe();
            }
        }
    }
}
