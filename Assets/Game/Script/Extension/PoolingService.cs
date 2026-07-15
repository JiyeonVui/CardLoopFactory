using System;
using System.Collections.Generic;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Script.Engine.Manager.Pooling {
    public class PoolingService : IPoolingService {
        private readonly GameObject _poolingHolder;
        private readonly Dictionary<int, GameObject> _parentHolder = new();
        private readonly List<PoolInfo> _objectPools = new();
        private readonly Dictionary<Type, object> _typedPools = new();

        public PoolingService() {
            _poolingHolder = new GameObject("Pool Holder");
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private List<PoolInfo<T>> GetTypedPools<T>() {
            if (!_typedPools.TryGetValue(typeof(T), out var list)) {
                list = new List<PoolInfo<T>>();
                _typedPools[typeof(T)] = list;
            }

            return (List<PoolInfo<T>>)list;
        }

        private T GetComponentAndSetReturnable<T>(GameObject obj) {
            var component = obj.GetComponent<T>();
            if (component is IPoolReturnable returnable) {
                returnable.SetPoolingService(this);
            }

            return component;
        }

        private void DetectPoolReturnable(GameObject obj) {
            var returnables = obj.GetComponents<IPoolReturnable>();
            foreach (var returnable in returnables) {
                returnable.SetPoolingService(this);
            }
        }

        private void SetParentHolder(int id, string objectName, GameObject spawnObject) {
            if (_parentHolder.TryGetValue(id, out var value)) {
                spawnObject.transform.SetParent(value.transform);
            } else {
                var objectHolder = new GameObject(objectName);
                objectHolder.transform.SetParent(_poolingHolder.transform);
                spawnObject.transform.SetParent(objectHolder.transform);
                _parentHolder[id] = objectHolder;
            }
        }

        // ──────────────────────────────────────────────
        // Non-Generic SpawnObject
        // ──────────────────────────────────────────────

        public GameObject SpawnObject(GameObject obj, Vector3 pos) {
            return SpawnObject(obj, pos, Quaternion.identity, PoolingType.None, 0);
        }

        public GameObject SpawnObject(GameObject obj, Vector3 pos, PoolingType type, float time = 0) {
            return SpawnObject(obj, pos, Quaternion.identity, type, time);
        }

        public GameObject SpawnObject(GameObject obj, Vector3 pos, Quaternion rot) {
            return SpawnObject(obj, pos, rot, PoolingType.None, 0);
        }

        public GameObject SpawnObject(GameObject obj, Vector3 pos, Quaternion rot, PoolingType type,
            float time = 0) {
            var id = obj.GetInstanceID();
            var pool = _objectPools.Find(s => s.Id == id);
            if (pool == null) {
                pool = new PoolInfo { Id = id };
                _objectPools.Add(pool);
            }

            if (pool.InactiveObjects.TryDequeue(out var spawnObject)) {
                spawnObject.transform.position = pos;
                spawnObject.transform.rotation = rot;
                spawnObject.SetActive(true);
            } else {
                spawnObject = Object.Instantiate(obj, pos, rot);
                if (type == PoolingType.Particle) {
                    var component = spawnObject.AddComponent<ParticlePoolingReturn>();
                    component.SetPooling(this);
                } else if (type == PoolingType.GameObject) {
                    var poolingObject = spawnObject.AddComponent<ObjectPoolingReturn>();
                    poolingObject.Set(time < 0 ? 0 : time, this);
                }

                pool.AllIdList.Add(spawnObject.GetInstanceID());
                SetParentHolder(id, obj.name, spawnObject);
                DetectPoolReturnable(spawnObject);
            }

            return spawnObject;
        }

        public GameObject SpawnObject(GameObject obj, Transform parent, PoolingType type) {
            return SpawnObject(obj, parent, true, type);
        }

        public GameObject SpawnObject(GameObject obj, Transform parent, PoolingType type, float time) {
            return SpawnObject(obj, parent, true, type, time);
        }

        public GameObject SpawnObject(GameObject obj, Transform parent, bool followParent = true,
            PoolingType type = PoolingType.None, float time = 0) {
            var id = obj.GetInstanceID();
            var pool = _objectPools.Find(s => s.Id == id);
            if (pool == null) {
                pool = new PoolInfo { Id = id };
                _objectPools.Add(pool);
            }

            if (pool.InactiveObjects.TryDequeue(out var spawnObject)) {
                if (spawnObject == null) {
                    spawnObject = Object.Instantiate(obj, parent);
                    if (type == PoolingType.Particle) {
                        if (time == 0) {
                            var component = spawnObject.AddComponent<ParticlePoolingReturn>();
                            component.SetPooling(this);
                        } else {
                            var poolingObject = spawnObject.AddComponent<ObjectPoolingReturn>();
                            poolingObject.Set(time < 0 ? 0 : time, this);
                        }
                    } else if (type == PoolingType.GameObject) {
                        var poolingObject = spawnObject.AddComponent<ObjectPoolingReturn>();
                        poolingObject.Set(time < 0 ? 0 : time, this);
                    }

                    pool.AllIdList.Add(spawnObject.GetInstanceID());
                    DetectPoolReturnable(spawnObject);
                    if (!followParent) {
                        spawnObject.transform.SetParent(null);
                        if (_parentHolder.TryGetValue(id, out var value)) {
                            spawnObject.transform.SetParent(value.transform);
                        }
                    }
                } else {
                    spawnObject.SetActive(true);
                    if (!followParent) {
                        spawnObject.transform.SetParent(parent);
                        spawnObject.transform.position = parent.position;
                        spawnObject.transform.SetParent(null);
                        if (_parentHolder.TryGetValue(id, out var value)) {
                            spawnObject.transform.SetParent(value.transform);
                        }
                    }
                }
            } else {
                spawnObject = Object.Instantiate(obj, parent);
                if (type == PoolingType.Particle) {
                    if (time == 0) {
                        var component = spawnObject.AddComponent<ParticlePoolingReturn>();
                        component.SetPooling(this);
                    } else {
                        var poolingObject = spawnObject.AddComponent<ObjectPoolingReturn>();
                        poolingObject.Set(time < 0 ? 0 : time, this);
                    }
                } else if (type == PoolingType.GameObject) {
                    var poolingObject = spawnObject.AddComponent<ObjectPoolingReturn>();
                    poolingObject.Set(time < 0 ? 0 : time, this);
                }

                pool.AllIdList.Add(spawnObject.GetInstanceID());
                DetectPoolReturnable(spawnObject);
                if (!followParent) {
                    spawnObject.transform.SetParent(null);
                    if (_parentHolder.TryGetValue(id, out var value)) {
                        spawnObject.transform.SetParent(value.transform);
                    } else {
                        var objectHolder = new GameObject(spawnObject.name);
                        objectHolder.transform.SetParent(_poolingHolder.transform);
                        spawnObject.transform.SetParent(objectHolder.transform);
                        _parentHolder[id] = objectHolder;
                    }
                }
            }

            return spawnObject;
        }

        public void ReturnObjectToPool(GameObject obj) {
            var pool = _objectPools.Find(s => s.AllIdList.Contains(obj.GetInstanceID()));
            if (pool != null) {
                obj.SetActive(false);
                pool.InactiveObjects.Enqueue(obj);
            } else {
                Debug.LogWarning("This object not in simple pool" + obj.name);
            }
        }

        // ──────────────────────────────────────────────
        // Generic SpawnObject<T>
        // ──────────────────────────────────────────────

        public T SpawnObject<T>(GameObject obj, Vector3 pos, PoolingType type = PoolingType.None, float time = 0) {
            return SpawnObject<T>(obj, pos, Quaternion.identity, type, time);
        }

        public T SpawnObject<T>(GameObject obj, Vector3 pos, Quaternion rot, PoolingType type = PoolingType.None,
            float time = 0) {
            var id = obj.GetInstanceID();
            var pools = GetTypedPools<T>();
            var pool = pools.Find(s => s.Id == id);
            if (pool == null) {
                pool = new PoolInfo<T> { Id = id };
                pools.Add(pool);
            }

            if (pool.InactiveObjects.TryDequeue(out var spawnObject)) {
                spawnObject.transform.position = pos;
                spawnObject.transform.rotation = rot;
                spawnObject.SetActive(true);
            } else {
                spawnObject = Object.Instantiate(obj, pos, rot);
                pool.AllIdList.Add(spawnObject.GetInstanceID());

                AddGenericAutoReturn<T>(spawnObject, type, time);
                SetParentHolder(id, obj.name, spawnObject);
            }

            if (pool.InactiveType.TryDequeue(out var spawnType)) {
                return spawnType;
            }

            return GetComponentAndSetReturnable<T>(spawnObject);
        }

        public T SpawnSimple<T>(GameObject obj, Vector3 pos, Quaternion rot) {
            var id = obj.GetInstanceID();
            var pools = GetTypedPools<T>();
            var pool = pools.Find(s => s.Id == id);
            if (pool == null) {
                pool = new PoolInfo<T> { Id = id };
                pools.Add(pool);
            }

            if (pool.InactiveObjects.TryDequeue(out var spawnObject)) {
                spawnObject.transform.position = pos;
                spawnObject.transform.rotation = rot;
                spawnObject.SetActive(true);
            } else {
                spawnObject = Object.Instantiate(obj, pos, rot);
                pool.AllIdList.Add(spawnObject.GetInstanceID());
                SetParentHolder(id, obj.name, spawnObject);
            }

            if (pool.InactiveType.TryDequeue(out var spawnType)) {
                return spawnType;
            }

            return GetComponentAndSetReturnable<T>(spawnObject);
        }

        public T SpawnObject<T>(GameObject obj, Transform parent, PoolingType type, float time = 0) {
            return SpawnObject<T>(obj, parent, true, type, time);
        }

        public T SpawnObject<T>(GameObject obj, Transform parent, bool followParent = true,
            PoolingType type = PoolingType.None, float time = 0) {
            var id = obj.GetInstanceID();
            var pools = GetTypedPools<T>();
            var pool = pools.Find(s => s.Id == id);
            if (pool == null) {
                pool = new PoolInfo<T> { Id = id };
                pools.Add(pool);
            }

            var isNullRef = false;
            GameObject spawnObject;
            if (pool.InactiveObjects.TryDequeue(out spawnObject)) {
                if (spawnObject == null) {
                    isNullRef = true;
                    spawnObject = Object.Instantiate(obj, parent);
                    pool.AllIdList.Add(spawnObject.GetInstanceID());

                    AddGenericAutoReturn<T>(spawnObject, type, time);

                    if (!followParent) {
                        spawnObject.transform.SetParent(null);
                        if (_parentHolder.TryGetValue(id, out var value)) {
                            spawnObject.transform.SetParent(value.transform);
                        }
                    }
                } else {
                    spawnObject.SetActive(true);
                    if (!followParent) {
                        spawnObject.transform.SetParent(parent);
                        spawnObject.transform.position = parent.position;
                        spawnObject.transform.SetParent(null);
                        if (_parentHolder.TryGetValue(id, out var value)) {
                            spawnObject.transform.SetParent(value.transform);
                        }
                    }
                }
            } else {
                spawnObject = Object.Instantiate(obj, parent);
                pool.AllIdList.Add(spawnObject.GetInstanceID());

                AddGenericAutoReturn<T>(spawnObject, type, time);

                if (!followParent) {
                    spawnObject.transform.SetParent(null);
                    if (_parentHolder.TryGetValue(id, out var value)) {
                        spawnObject.transform.SetParent(value.transform);
                    } else {
                        var objectHolder = new GameObject(spawnObject.name);
                        objectHolder.transform.SetParent(_poolingHolder.transform);
                        spawnObject.transform.SetParent(objectHolder.transform);
                        _parentHolder[id] = objectHolder;
                    }
                }
            }

            if (pool.InactiveType.TryDequeue(out var spawnType)) {
                if (isNullRef)
                    return GetComponentAndSetReturnable<T>(spawnObject);
                return spawnType;
            }

            return GetComponentAndSetReturnable<T>(spawnObject);
        }

        public T SpawnSimple<T>(GameObject obj, Transform parent) {
            var id = obj.GetInstanceID();
            var pools = GetTypedPools<T>();
            var pool = pools.Find(s => s.Id == id);
            if (pool == null) {
                pool = new PoolInfo<T> { Id = id };
                pools.Add(pool);
            }

            var isNullRef = false;
            GameObject spawnObject;
            if (pool.InactiveObjects.TryDequeue(out spawnObject)) {
                if (spawnObject == null) {
                    isNullRef = true;
                    spawnObject = Object.Instantiate(obj, parent);
                    pool.AllIdList.Add(spawnObject.GetInstanceID());
                } else {
                    spawnObject.SetActive(true);
                }
            } else {
                spawnObject = Object.Instantiate(obj, parent);
                pool.AllIdList.Add(spawnObject.GetInstanceID());
            }

            if (pool.InactiveType.TryDequeue(out var spawnType)) {
                if (isNullRef)
                    return GetComponentAndSetReturnable<T>(spawnObject);
                return spawnType;
            }

            return GetComponentAndSetReturnable<T>(spawnObject);
        }

        public void ReturnObjectToPool<T>(T type, GameObject obj) {
            var pools = GetTypedPools<T>();
            var pool = pools.Find(s => s.AllIdList.Contains(obj.GetInstanceID()));
            if (pool != null) {
                obj.SetActive(false);
                // Bất biến: object đã ở trong pool thì nằm dưới Pool Holder. Nhờ vậy các
                // holder trên sân chỉ chứa object đang active, reset duyệt holder không
                // đụng phải object đã pooled.
                obj.transform.SetParent(_poolingHolder.transform, false);
                pool.InactiveObjects.Enqueue(obj);
                pool.InactiveType.Enqueue(type);
            } else {
                Debug.LogWarning("This object not in pool" + obj.name);
            }
        }

        // ──────────────────────────────────────────────
        // Auto-Return Component Helpers
        // ──────────────────────────────────────────────

        private void AddGenericAutoReturn<T>(GameObject spawnObject, PoolingType type, float time) {
            if (type == PoolingType.Particle) {
                if (typeof(T) == typeof(ParticleSystem)) {
                    var component = spawnObject.AddComponent<ParticlePoolingTypeReturn>();
                    var objectParticle = spawnObject.GetComponent<ParticleSystem>();
                    component.SetComponent(objectParticle, this);
                } else if (typeof(T) == typeof(Transform)) {
                    var component = spawnObject.AddComponent<ParticlePoolingTransformReturn>();
                    var objectTransform = spawnObject.GetComponent<Transform>();
                    component.SetComponent(objectTransform, this);
                }
            } else if (type == PoolingType.GameObject) {
                if (typeof(T) == typeof(Transform)) {
                    var component = spawnObject.AddComponent<ObjectPoolingTransformReturn>();
                    var objectTransform = spawnObject.GetComponent<Transform>();
                    component.Set(time < 0 ? 0 : time, objectTransform, this);
                }
            }
        }

        // ──────────────────────────────────────────────
        // Cleanup
        // ──────────────────────────────────────────────

        public void ClearAll() {
            _objectPools.Clear();
            _typedPools.Clear();
            _parentHolder.Clear();
            if (_poolingHolder) {
                Object.Destroy(_poolingHolder);
            }
        }
    }
}
