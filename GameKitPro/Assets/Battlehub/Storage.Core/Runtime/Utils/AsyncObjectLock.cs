// https://stackoverflow.com/questions/31138179/asynchronous-locking-based-on-a-key

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Battlehub.Storage
{
    public class AsyncObjectLock
    {
        private static readonly Dictionary<object, Ref<SemaphoreSlim>> s_semaphores = new Dictionary<object, Ref<SemaphoreSlim>>();

        private SemaphoreSlim GetOrCreate(object key)
        {
            Ref<SemaphoreSlim> item;
            lock (s_semaphores)
            {
                if (s_semaphores.TryGetValue(key, out item))
                {
                    ++item.RefCount;
                }
                else
                {
                    item = new Ref<SemaphoreSlim>(new SemaphoreSlim(1, 1));
                    s_semaphores[key] = item;
                }
            }
            return item.Value;
        }

        public IDisposable Lock(object key)
        {
            GetOrCreate(key).Wait();
            return new LockReleaser { Key = key };
        }

        public async Task<IDisposable> LockAsync(object key)
        {
            await GetOrCreate(key).WaitAsync().ConfigureAwait(false);
            return new LockReleaser { Key = key };
        }

        private class LockReleaser : IDisposable
        {
            public object Key { get; set; }

            public void Dispose()
            {
                Ref<SemaphoreSlim> item;
                lock (s_semaphores)
                {
                    item = s_semaphores[Key];
                    --item.RefCount;
                    if (item.RefCount == 0)
                        s_semaphores.Remove(Key);
                }
                item.Value.Release();
            }
        }

        private sealed class Ref<T>
        {
            public Ref(T value)
            {
                RefCount = 1;
                Value = value;
            }

            public int RefCount { get; set; }
            public T Value { get; private set; }
        }
    }
}