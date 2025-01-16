using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Pool;
namespace Chris
{
    public class UniParallel : List<UniTask>, IDisposable
    {
        private static readonly ObjectPool<UniParallel> pool = new(() => new(), null, (e) => e.Clear());
        public static UniParallel Get()
        {
            return pool.Get();
        }
        public static UniParallel Create(UniTask task1, UniTask task2)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            return task;
        }
        public static UniParallel Create(UniTask task1, UniTask task2, UniTask task3)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            return task;
        }
        public static UniParallel Create(UniTask task1, UniTask task2, UniTask task3, UniTask task4)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            task.Add(task4);
            return task;
        }
        public static UniParallel Create(IEnumerable<UniTask> uniTasks)
        {
            var task = Get();
            task.AddRange(uniTasks);
            return task;
        }
        public void Dispose()
        {
            pool.Release(this);
        }
        public UniTask.Awaiter GetAwaiter()
        {
            return UniTask.WhenAll(this).GetAwaiter();
        }
        public void Forget()
        {
            UniTask.WhenAll(this).Forget();
        }
    }
    public class UniParallel<T> : List<UniTask<T>>, IDisposable
    {
        private static readonly ObjectPool<UniParallel<T>> pool = new(() => new(), null, (e) => e.Clear());
        public static UniParallel<T> Get()
        {
            return pool.Get();
        }
        public static UniParallel<T> Create(UniTask<T> task1, UniTask<T> task2)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            return task;
        }
        public static UniParallel<T> Create(UniTask<T> task1, UniTask<T> task2, UniTask<T> task3)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            return task;
        }
        public static UniParallel<T> Create(UniTask<T> task1, UniTask<T> task2, UniTask<T> task3, UniTask<T> task4)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            task.Add(task4);
            return task;
        }
        public static UniParallel<T> Create(IEnumerable<UniTask<T>> uniTasks)
        {
            var task = Get();
            task.AddRange(uniTasks);
            return task;
        }
        public void Dispose()
        {
            pool.Release(this);
        }
        public UniTask<T[]>.Awaiter GetAwaiter()
        {
            return UniTask.WhenAll(this).GetAwaiter();
        }
        public void Forget()
        {
            UniTask.WhenAll(this).Forget();
        }
    }
    public class UniSequence : List<UniTask>, IDisposable
    {
        private static readonly ObjectPool<UniSequence> pool = new(() => new(), null, (e) => e.Clear());
        public static UniSequence Get()
        {
            return pool.Get();
        }
        public static UniSequence Create(UniTask task1, UniTask task2)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            return task;
        }
        public static UniSequence Create(UniTask task1, UniTask task2, UniTask task3)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            return task;
        }
        public static UniSequence Create(UniTask task1, UniTask task2, UniTask task3, UniTask task4)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            task.Add(task4);
            return task;
        }
        public static UniSequence Create(IEnumerable<UniTask> uniTasks)
        {
            var task = Get();
            task.AddRange(uniTasks);
            return task;
        }
        public void Dispose()
        {
            pool.Release(this);
        }
        public UniTask.Awaiter GetAwaiter()
        {
            return AwaitAsSequence().GetAwaiter();
        }
        private async UniTask AwaitAsSequence()
        {
            foreach (var task in this)
            {
                await task;
            };
        }
        public void Forget()
        {
            AwaitAsSequence().Forget();
        }
    }
    public class SequenceTask<T> : List<UniTask<T>>, IDisposable
    {
        private static readonly ObjectPool<SequenceTask<T>> pool = new(() => new(), null, (e) => { e.Clear(); e.results = null; });
        private T[] results;
        public static SequenceTask<T> Get()
        {
            return pool.Get();
        }
        public static SequenceTask<T> Create(UniTask<T> task1, UniTask<T> task2)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            return task;
        }
        public static SequenceTask<T> Create(UniTask<T> task1, UniTask<T> task2, UniTask<T> task3)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            return task;
        }
        public static SequenceTask<T> Create(UniTask<T> task1, UniTask<T> task2, UniTask<T> task3, UniTask<T> task4)
        {
            var task = Get();
            task.Add(task1);
            task.Add(task2);
            task.Add(task3);
            task.Add(task4);
            return task;
        }
        public static SequenceTask<T> Create(IEnumerable<UniTask<T>> uniTasks)
        {
            var task = Get();
            task.AddRange(uniTasks);
            return task;
        }
        public static SequenceTask<T> GetNonAlloc(T[] results)
        {
            var task = pool.Get();
            task.results = results;
            return task;
        }
        public void Dispose()
        {
            pool.Release(this);
        }
        public UniTask<T[]>.Awaiter GetAwaiter()
        {
            return AwaitAsSequence().GetAwaiter();
        }
        private async UniTask<T[]> AwaitAsSequence()
        {
            results ??= new T[Count];
            Assert.IsTrue(results.Length >= Count);
            for (int i = 0; i < Count; ++i)
            {
                results[i] = await this[i];
            };
            return results;
        }
        public void Forget()
        {
            AwaitAsSequence().Forget();
        }
    }
}