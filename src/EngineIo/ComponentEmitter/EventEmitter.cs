using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace EngineIo.ComponentEmitter
{
    /// <remarks>
    /// The event emitter which is ported from the JavaScript module.
    /// <see href="https://github.com/component/emitter">https://github.com/component/emitter</see>
    /// </remarks>
    public class EventEmitter
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<IListener>> _callbacks = new ConcurrentDictionary<string, ConcurrentBag<IListener>>();
        // private readonly IDictionary<IListener, IListener> _onceCallbacks;

        public EventEmitter()
        {

        }

        /// <summary>
        /// Executes each of listeners with the given args.
        /// </summary>
        /// <param name="eventString">Event name</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual EventEmitter Emit(string eventString, params object[] args)
        {
            var listeners = _callbacks.GetOrAdd(eventString, new ConcurrentBag<IListener>());

            foreach (var listener in listeners)
            {
                listener.Call(args);
            }

            return this;
        }

        /// <summary>
        /// Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public virtual EventEmitter On(string eventString, IListener action)
        {
            var listeners = _callbacks.GetOrAdd(eventString, new ConcurrentBag<IListener>());

            listeners.Add(action);
            /*
                if (!_callbacks.ContainsKey(eventString))
                {
                    // callbacks[eventString] = ImmutableList<IListener>.Empty;
                    _callbacks = _callbacks.Add(eventString, ImmutableList<IListener>.Empty);
                }

                var callbacksLocal = _callbacks[eventString];
                callbacksLocal = callbacksLocal.Add(fn);
                // callbacks[eventString] = callbacksLocal;
                _callbacks = _callbacks.Remove(eventString).Add(eventString, callbacksLocal);

                return this;
            */
            return this;
        }

    }
}
