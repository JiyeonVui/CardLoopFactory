using System;
using System.Collections.Generic;
using Extension;
using Script.Engine.Manager.Pooling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Manager {
    
    public class GameEntityFactory : IGameEntityFactory {
        private Action<IGameContextSubscriber> _initSubscriber;
        private readonly Dictionary<string, Object> _cache = new();
        private IPoolingService _poolingService;

        public GameEntityFactory(IPoolingService poolingService)
        {
            _poolingService = poolingService;
        }
        
        internal void SetInitializer(Action<IGameContextSubscriber> initSubscriber) {
            _initSubscriber = initSubscriber;
        }

        public GameObject Instantiate(GameObject prefab, Transform parent = null) {
            var go = Object.Instantiate(prefab, parent);
            InitializeGameObject(go);
            return go;
        }

        public GameObject Instantiate(GameObject prefab, Vector3 position, Transform parent = null) {
            var go = Object.Instantiate(prefab,position,Quaternion.identity, parent);
            InitializeGameObject(go);
            return go;
        }

        public GameObject Instantiate(string resourcePath, Transform parent = null) {
            var prefab = Load<GameObject>(resourcePath);
            if (!prefab) {
                Debug.LogError($"[GameEntityFactory] Failed to load: {resourcePath}");
                return null;
            }
            return Instantiate(prefab, parent);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) {
            var go = _poolingService.SpawnObject(prefab, position, rotation);
            InitializeGameObject(go);
            return go;
        }

        public GameObject Spawn(GameObject prefab, Transform parent, bool followParent = true) {
            var go = _poolingService.SpawnObject(prefab, parent, followParent, PoolingType.None);
            InitializeGameObject(go);
            return go;
        }

        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component {
            var component = _poolingService.SpawnObject<T>(prefab, position, rotation, PoolingType.None);
            InitializeGameObject(component.gameObject);
            return component;
        }

        public T Spawn<T>(GameObject prefab, Transform parent, bool followParent = true) where T : Component {
            var component = _poolingService.SpawnObject<T>(prefab, parent, followParent, PoolingType.None);
            InitializeGameObject(component.gameObject);
            return component;
        }

        public void Return(GameObject go) {
            _poolingService.ReturnObjectToPool(go);
        }

        public void InitializeGameObject(GameObject go) {
            var subscribers = go.GetComponentsInChildren<IGameContextSubscriber>(true);
            
            foreach (var sub in subscribers) {
                try { _initSubscriber(sub); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private T Load<T>(string path) where T : Object {
            if (_cache.TryGetValue(path, out var cached))
                return cached as T;
            var loaded = Resources.Load<T>(path);
            if (loaded) _cache[path] = loaded;
            return loaded;
        }

        public void ClearCache() => _cache.Clear();
    }
    

}
