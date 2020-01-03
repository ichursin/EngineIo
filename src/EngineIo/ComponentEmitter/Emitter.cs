using System.Collections.Immutable;
using EngineIo.Modules;
using System;
using System.Linq;

namespace EngineIo.ComponentEmitter
{
    /// <remarks>
    /// The event emitter which is ported from the JavaScript module.
    /// <see href="https://github.com/component/emitter">https://github.com/component/emitter</see>
    /// </remarks>
    public class Emitter
    {
        private IImmutableDictionary<string, IImmutableList<IListener>> _callbacks;
        private IImmutableDictionary<IListener, IListener> _onceCallbacks;

        public Emitter()
        {
            Off();
        }

        /// <summary>
        /// Executes each of listeners with the given args.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="args"></param>
        /// <returns>a reference to this object.</returns>
        public virtual Emitter Emit(string eventString, params object[] args)
        {
            // var log = LogManager.GetLogger(Global.CallerName());
            // log.Info("Emitter emit event = " + eventString);
            if (_callbacks.ContainsKey(eventString))
            {
                var listeners = _callbacks[eventString];

                foreach (var listener in listeners)
                {
                    listener.Call(args);
                }
            }

            return this;
        }

        /// <summary>
        /// Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, IListener fn)
        {
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
        }

        /// <summary>
        /// Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, Action fn)
        {
            var listener = new ListenerImpl(fn);
            return On(eventString, listener);
        }

        /// <summary>
        ///  Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, Action<object> fn)
        {
            var listener = new ListenerImpl(fn);
            return On(eventString, listener);
        }


        /// <summary>
        /// Adds a one time listener for the event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter Once(string eventString, IListener fn)
        {
            var on = new OnceListener(eventString, fn, this);
            _onceCallbacks = _onceCallbacks.Add(fn, on);
            On(eventString, on);

            return this;
        }

        /// <summary>
        /// Adds a one time listener for the event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter Once(string eventString, Action fn)
        {
            var listener = new ListenerImpl(fn);
            return Once(eventString, listener);
        }

        /// <summary>
        /// Removes all registered listeners.
        /// </summary>
        /// <returns>a reference to this object.</returns>
        public Emitter Off()
        {
            _callbacks = ImmutableDictionary.Create<string, IImmutableList<IListener>>();
            _onceCallbacks = ImmutableDictionary.Create<IListener, IListener>();

            return this;
        }

        /// <summary>
        /// Removes all listeners of the specified event.
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <returns>a reference to this object.</returns>
        public Emitter Off(string eventString)
        {
            try
            {
                if (!_callbacks.TryGetValue(eventString, out IImmutableList<IListener> retrievedValue))
                {
                    var log = LogManager.GetLogger(Global.CallerName());
                    log.Info(string.Format("Emitter.Off Could not remove {0}", eventString));
                }

                if (retrievedValue != null)
                {
                    _callbacks = _callbacks.Remove(eventString);

                    foreach (var listener in retrievedValue)
                    {
                        _onceCallbacks.Remove(listener);
                    }
                }
            }
            catch (Exception)
            {
                Off();
            }

            return this;
        }

        /// <summary>
        /// Removes the listener
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object.</returns>
        public Emitter Off(string eventString, IListener fn)
        {
            try
            {
                if (_callbacks.ContainsKey(eventString))
                {
                    var callbacks = _callbacks[eventString];
                    _onceCallbacks.TryGetValue(fn, out IListener offListener);
                    _onceCallbacks = _onceCallbacks.Remove(fn);

                    if (callbacks.Count > 0 && callbacks.Contains(offListener ?? fn))
                    {
                        callbacks = callbacks.Remove(offListener ?? fn);
                        _callbacks = _callbacks.Remove(eventString);
                        _callbacks = _callbacks.Add(eventString, callbacks);
                    }
                }
            }
            catch (Exception)
            {
                Off();
            }

            return this;
        }

        /// <summary>
        ///  Returns a list of listeners for the specified event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <returns>a reference to this object</returns>
        public IImmutableList<IListener> Listeners(string eventString)
        {
            if (_callbacks.ContainsKey(eventString))
            {
                var callbacks = _callbacks[eventString];
                return callbacks ?? ImmutableList<IListener>.Empty;
            }

            return ImmutableList<IListener>.Empty;
        }

        /// <summary>
        /// Check if this emitter has listeners for the specified event.
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <returns>bool</returns>
        public bool HasListeners(string eventString)
        {
            return Listeners(eventString).Count > 0;
        }
    }

    public interface IListener : IComparable<IListener>
    {
        int Id { get; }

        void Call(params object[] args);
    }

    public class ListenerImpl : IListener
    {
        private static int id_counter = 0;

        private readonly Action<object> _fn;
        private readonly Action _fn1;

        public int Id { get; }

        public ListenerImpl(Action<object> fn)
        {
            _fn = fn;
            Id = id_counter++;
        }

        public ListenerImpl(Action fn)
        {
            _fn1 = fn;
            Id = id_counter++;
        }

        public void Call(params object[] args)
        {
            if (_fn != null)
            {
                var arg = args.Length > 0 ? args[0] : null;
                _fn.Invoke(arg);
            }
            else
            {
                _fn1.Invoke();
            }
        }

        public int CompareTo(IListener other)
            => Id.CompareTo(other.Id);
    }

    public class OnceListener : IListener
    {
        private static int id_counter = 0;
        private readonly string _eventString;
        private readonly IListener _fn;
        private readonly Emitter _emitter;

        public int Id { get; }

        public OnceListener(string eventString, IListener fn, Emitter emitter)
        {
            _eventString = eventString;
            _fn = fn;
            _emitter = emitter;
            Id = id_counter++;
        }

        void IListener.Call(params object[] args)
        {
            _emitter.Off(_eventString, this);
            _fn.Call(args);
        }

        public int CompareTo(IListener other)
            => Id.CompareTo(other.Id);
    }
}
