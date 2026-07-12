using System.Collections.Generic;

using UnityEngine;

namespace Script.Engine.Manager.Pooling {
    public class PoolInfo {
        public int Id;
        public readonly List<int> AllIdList = new();
        public readonly Queue<GameObject> InactiveObjects = new();
    }

    public class PoolInfo<T> {
        public int Id;
        public readonly List<int> AllIdList = new();
        public readonly Queue<T> InactiveType = new();
        public readonly Queue<GameObject> InactiveObjects = new();
    }
}
