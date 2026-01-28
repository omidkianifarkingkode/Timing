using System;
using System.Collections.Generic;

namespace Timing.Timers
{
    public sealed class TimerCallbackRegistry
    {
        private readonly Dictionary<string, Action> _callbacks = new();

        public void Register(string id, Action cb) => _callbacks[id] = cb;

        public bool TryResolve(string id, out Action cb) => _callbacks.TryGetValue(id, out cb);
    }
}
