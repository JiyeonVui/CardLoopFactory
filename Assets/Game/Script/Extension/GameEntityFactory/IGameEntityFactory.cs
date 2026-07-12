using UnityEngine;

namespace Engine.Manager {
    public interface IGameEntityFactory {
        GameObject Instantiate(GameObject prefab, Transform parent = null);
        GameObject Instantiate(GameObject prefab,Vector3 position, Transform parent = null);
        GameObject Instantiate(string resourcePath, Transform parent = null);

        // Pooling-based spawns (reuse from pool instead of Object.Instantiate).
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
        GameObject Spawn(GameObject prefab, Transform parent, bool followParent = true);
        T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component;
        T Spawn<T>(GameObject prefab, Transform parent, bool followParent = true) where T : Component;
        void Return(GameObject go);

        void InitializeGameObject(GameObject go);
        void ClearCache();
    }
}
