#region using

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#endregion

namespace Dry.Common {
    public class ProducerConsumerQueue : IDisposable {
        readonly object _queuelock = new object();
        readonly ConcurrentDictionary<string, Thread> _workers;
        readonly Queue<Action> _queue = new Queue<Action>();
        readonly List<Exception> _exceptions = new List<Exception>();

        public int Threadcount { get; private set; }
        public string Name { get; private set; }
        public IList<Exception> Exceptions { get { return _exceptions; } }

        public ProducerConsumerQueue(string name) : this(name, Environment.ProcessorCount) {}

        public ProducerConsumerQueue(string name, int threadcount) {
            Name = name ?? "PCQueue";
            this.Threadcount = threadcount;
            _workers = new ConcurrentDictionary<string, Thread>(threadcount, threadcount);
            for (var i = 0; i < this.Threadcount; i++) {
                var key = Name + "#" + i;
                _workers.TryAdd(key, null);
            }
        }

        void BuildThreads() {
            var tc = _queue.Count < Threadcount ? _queue.Count : Threadcount;
            for (var i = 0; i < tc; i++) {
                var key = Name + "#" + i;
                if (_workers[key] != null) continue;

                _workers[key] = new Thread(Runner) {Name = key};
                System.Diagnostics.Debug.WriteLine("Starting : " + key);
                _workers[key].Start();
            }
        }

        public bool HasItems {
            get {
                return _queue.Count > 1;
            }
        }


        public void Enqueue(params Action[] item) {
            lock (_queuelock) {
                foreach (var action in item) {
                    _queue.Enqueue(action);
                }
                _wait = true;
                BuildThreads();
            }
        }

        public void Dispose() {
            foreach (var worker in _workers) {
                _queue.Enqueue(null);
            }
        }

        bool _wait = true;
        public void Wait() {
            lock(_queuelock) {
                while (_wait)
                    Monitor.Wait(_queuelock);
            }
        }

        void Runner() {
            while (true) {
                Action action = null;
                lock (_queuelock) {
                    if (_queue.Count > 0) {
                        action = _queue.Dequeue();
                    }
                    if (action == null) {
                        _workers[Thread.CurrentThread.Name] = null;
                        _wait = _workers.Any(t => t.Value != null) || _queue.Count > 0;
                        Monitor.Pulse(_queuelock);
                        return;
                    }
                }
                try {
                    action();
                } catch (Exception e) {
                    Exceptions.Add(e);
                }
            }
        }
    }
}
