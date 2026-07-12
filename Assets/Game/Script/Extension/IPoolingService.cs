using Extension;
using UnityEngine;

public enum PoolingType {
    None,
    Particle,
    GameObject,
}

namespace Script.Engine.Manager.Pooling {
    [Service(nameof(IPoolingService))]
    public interface IPoolingService {
        // Non-generic (from PoolingManager)
        GameObject SpawnObject(GameObject obj, Vector3 pos, Quaternion rot, PoolingType type, float time = 0);
        GameObject SpawnObject(GameObject obj, Vector3 pos);
        GameObject SpawnObject(GameObject obj, Vector3 pos, Quaternion rot);
        GameObject SpawnObject(GameObject obj, Vector3 pos, PoolingType type, float time = 0);
        GameObject SpawnObject(GameObject obj, Transform parent, PoolingType type);
        GameObject SpawnObject(GameObject obj, Transform parent, PoolingType type, float time);

        GameObject SpawnObject(GameObject obj, Transform parent, bool followParent = true,
            PoolingType type = PoolingType.None, float time = 0);

        void ReturnObjectToPool(GameObject obj);

        // Generic (from PoolingManager<T>)
        T SpawnObject<T>(GameObject obj, Vector3 pos, PoolingType type = PoolingType.None, float time = 0);

        T SpawnObject<T>(GameObject obj, Vector3 pos, Quaternion rot, PoolingType type = PoolingType.None,
            float time = 0);

        T SpawnSimple<T>(GameObject obj, Vector3 pos, Quaternion rot);
        T SpawnObject<T>(GameObject obj, Transform parent, PoolingType type, float time = 0);

        T SpawnObject<T>(GameObject obj, Transform parent, bool followParent = true,
            PoolingType type = PoolingType.None, float time = 0);

        T SpawnSimple<T>(GameObject obj, Transform parent);
        void ReturnObjectToPool<T>(T type, GameObject obj);

        void ClearAll();
    }

    public interface IPoolReturnable {
        void SetPoolingService(IPoolingService poolingService);
    }
}
