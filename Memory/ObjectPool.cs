using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace neutrino {
    public class ObjectPool<T> {
        private Func<T> _instanceFactory;
        private ConcurrentBag<T> _instanceItems;

        public ObjectPool(Func<T> instanceFactory) {
            _instanceFactory = instanceFactory ??
              throw new ArgumentNullException(nameof(instanceFactory));
            _instanceItems = new ConcurrentBag<T>();
        }

        public T New() {
            T item;
            if (_instanceItems.TryTake(out item)) return item;
            return _instanceFactory();
        }

        public void Free(T item) {
            _instanceItems.Add(item);
        }
    }
}
