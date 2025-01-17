using System;
using System.Collections.Generic;
using Chris.Schedulers;
using R3.Chris;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
namespace Chris.Pool
{
    /// <summary>
    /// Wrapper for pooling gameObject
    /// </summary>
    public class PooledGameObject : IDisposable, IDisposableUnregister
    {
        public PooledGameObject() { }
        private readonly static _ObjectPool<PooledGameObject> pool = new(() => new());
        public GameObject GameObject { get; protected set; }
        public Transform Transform { get; protected set; }
        public bool IsDisposed { get; protected set; }
        /// <summary>
        /// Disposable managed by pooling scope
        /// </summary>
        /// <returns></returns>
        private readonly List<IDisposable> disposables = new();
        private List<SchedulerHandle> schedulerHandles;
        /// <summary>
        /// Key to GameObject pool
        /// </summary>
        /// <value></value>
        public PoolKey PoolKey { get; protected set; }
        public static void SetMaxSize(int size)
        {
            pool.MaxSize = size;
        }
        /// <summary>
        /// Get or create empty pooled gameObject by address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static PooledGameObject Get(string address, Transform parent = null)
        {
            var pooledObject = pool.Get();
            pooledObject.PoolKey = new(address);
            pooledObject.GameObject = GameObjectPoolManager.Get(pooledObject.PoolKey, out _, parent);
            pooledObject.Init();
            return pooledObject;
        }

        /// <summary>
        /// Should not use AddTo(GameObject gameObject) since gameObject will not be destroyed until pool manager cleanup.
        /// </summary>
        /// <param name="disposable"></param>
        /// <remarks>
        /// Implement of <see cref="IDisposableUnregister"/> to manage <see cref="IDisposable"/> in pooling scope.
        /// </remarks>
        void IDisposableUnregister.Register(IDisposable disposable)
        {
            disposables.Add(disposable);
        }
        /// <summary>
        /// Add <see cref="SchedulerHandle"/> manually to reduce allocation
        /// </summary>
        /// <param name="handle"></param>
        public void Add(SchedulerHandle handle)
        {
            schedulerHandles ??= ListPool<SchedulerHandle>.Get();
            schedulerHandles.Add(handle);
        }
        protected virtual void Init()
        {
            LocalInit();
        }
        private void LocalInit()
        {
            IsDisposed = false;
            Transform = GameObject.transform;
            InitDisposables();
        }
        public virtual void Dispose()
        {
            if (IsDisposed) return;
            ReleaseDisposables();
            if (GameObjectPoolManager.IsInstantiated)
                GameObjectPoolManager.Release(GameObject, PoolKey);
            IsDisposed = true;
            pool.Release(this);
        }
        protected void InitDisposables()
        {
            disposables.Clear();
        }
        protected void ReleaseDisposables()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
            disposables.Clear();
            if (schedulerHandles == null) return;
            foreach (var schedulerHandle in schedulerHandles)
                schedulerHandle.Cancel();
            ListPool<SchedulerHandle>.Release(schedulerHandles);
            schedulerHandles = null;
        }
        public unsafe void Destroy(float t = 0f)
        {
            if (t >= 0f)
                Add(Scheduler.DelayUnsafe(t, new SchedulerUnsafeBinding(this, &Dispose_Imp)));
            else
                Dispose();
        }
        private static void Dispose_Imp(object @object)
        {
            ((IDisposable)@object).Dispose();
        }
    }
    public interface IPooledMetaData { }
    /// <summary>
    /// Struct based pool key without allocation
    /// </summary>
    public readonly struct PoolKey
    {
        public readonly string key;
        public readonly string subKey;
        public readonly int id;
        public PoolKey(string key)
        {
            this.key = key;
            subKey = null;
            id = 0;
        }
        public PoolKey(string key, string subKey)
        {
            this.key = key;
            this.subKey = subKey;
            id = 0;
        }
        public PoolKey(string key, int id)
        {
            this.key = key;
            subKey = null;
            this.id = id;
        }
        public readonly bool IsNull()
        {
            if (string.IsNullOrEmpty(key)) return true;
            bool isNull = true;
            isNull &= !string.IsNullOrEmpty(subKey);
            isNull &= id != 0;
            return isNull;
        }
        public readonly override string ToString()
        {
            if (IsNull()) return string.Empty;
            if (string.IsNullOrEmpty(subKey))
            {
                if (id != 0)
                    return $"{key} {id}";
                return key;
            }
            if (id != 0)
                return $"{key} {subKey} {id}";
            return $"{key} {subKey}";
        }
        public class Comparer : IEqualityComparer<PoolKey>
        {
            public bool Equals(PoolKey x, PoolKey y)
            {
                return x.id == y.id && x.key == y.key && x.subKey == y.subKey;
            }

            public int GetHashCode(PoolKey key)
            {
                return HashCode.Combine(key.id, key.key, key.subKey);
            }
        }
    }
    public sealed class GameObjectPoolManager : MonoBehaviour
    {

        private static GameObjectPoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject managerObject = new() { name = nameof(GameObjectPoolManager) };
                    instance = managerObject.AddComponent<GameObjectPoolManager>();
                }
                return instance;
            }
        }
        public static bool IsInstantiated => instance != null;
        private static GameObjectPoolManager instance;
        private readonly Dictionary<PoolKey, GameObjectPool> poolDic = new(new PoolKey.Comparer());
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            LocalReleaseAll();
        }
        /// <summary>
        /// Get pooled gameObject
        /// </summary>
        /// <param name="address"></param>
        /// <param name="pooledMetaData"></param>
        /// <param name="parent"></param>
        /// <param name="createEmptyIfNotExist"></param>
        /// <returns></returns>
        public static GameObject Get(PoolKey address, out IPooledMetaData pooledMetaData, Transform parent = null, bool createEmptyIfNotExist = true)
        {
            GameObject obj = null;
            pooledMetaData = null;
            if (Instance.poolDic.TryGetValue(address, out GameObjectPool poolData) && poolData.poolQueue.Count > 0)
            {
                obj = poolData.GetObj(parent, out pooledMetaData);
            }
            else if (createEmptyIfNotExist)
            {
                obj = new GameObject(address.ToString());
                obj.transform.SetParent(parent);
            }
            return obj;
        }
        /// <summary>
        /// Release gameObject to pool
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="address"></param>
        /// <param name="pooledMetaData"></param>
        public static void Release(GameObject obj, PoolKey address = default, IPooledMetaData pooledMetaData = null)
        {
            if (address.IsNull())
                address = new(obj.name);
            if (!Instance.poolDic.TryGetValue(address, out GameObjectPool poolData))
            {
                poolData = Instance.poolDic[address] = new GameObjectPool(address, Instance.transform);
            }
            poolData.PushObj(obj, pooledMetaData);
        }
        /// <summary>
        /// Release addressable gameObject pool
        /// </summary>
        /// <param name="address"></param>
        public static void ReleasePool(PoolKey address)
        {
            if (Instance.poolDic.TryGetValue(address, out var pool))
            {
                Destroy(pool.fatherObj);
                Instance.poolDic.Remove(address);
            }
        }
        private void LocalReleaseAll()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            poolDic.Clear();
        }
        /// <summary>
        /// Release all pooled gameObjects
        /// </summary>
        public static void ReleaseAll()
        {
            Instance.LocalReleaseAll();
        }
        private class GameObjectPool
        {
            public readonly PoolKey key;
            public readonly GameObject fatherObj;
            public readonly Queue<GameObject> poolQueue = new();
            private readonly Dictionary<GameObject, IPooledMetaData> metaData = new();
            public GameObjectPool(PoolKey address, Transform poolRoot)
            {
                fatherObj = new GameObject((key = address).ToString());
                fatherObj.transform.SetParent(poolRoot);
            }
            public void PushObj(GameObject obj, IPooledMetaData pooledMetaData)
            {
                poolQueue.Enqueue(obj);
                metaData[obj] = pooledMetaData;
                obj.transform.SetParent(fatherObj.transform);
                obj.SetActive(false);
            }
            public GameObject GetObj(Transform parent, out IPooledMetaData pooledMetaData)
            {
                GameObject obj = poolQueue.Dequeue();
                if (metaData.TryGetValue(obj, out pooledMetaData)) metaData.Remove(obj);
                obj.SetActive(true);
                obj.transform.SetParent(parent);
                if (parent == null)
                {
                    SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
                }
                return obj;
            }
        }
    }
}