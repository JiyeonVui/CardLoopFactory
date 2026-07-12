using System.Collections;

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace Script.Engine.Manager.Pooling {
    public class ObjectPoolingReturn : MonoBehaviour {
        private IPoolingService _pooling;
        private float _time;
        private bool _isInitTime;

        public void Set(float time, IPoolingService pooling) {
            _time = time;
            _pooling = pooling;
            _isInitTime = true;
        }

        protected async void OnEnable() {
            if (!_isInitTime)
                await UniTask.WaitUntil(() => _isInitTime);
            StartCoroutine(StartCountDown());
        }

        private IEnumerator StartCountDown() {
            var elapsedTime = 0f;
            while (elapsedTime < _time) {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _pooling.ReturnObjectToPool(gameObject);
        }
    }

    public class ObjectPoolingTransformReturn : MonoBehaviour {
        private IPoolingService _pooling;
        private float _time;
        private Transform _transform;
        private bool _isInitTime;

        public void Set(float time, Transform component, IPoolingService pooling) {
            _time = time;
            _transform = component;
            _pooling = pooling;
            _isInitTime = true;
        }

        protected async void OnEnable() {
            if (!_isInitTime)
                await UniTask.WaitUntil(() => _isInitTime);
            StartCoroutine(StartCountDown());
        }

        private IEnumerator StartCountDown() {
            var elapsedTime = 0f;
            while (elapsedTime < _time) {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _pooling.ReturnObjectToPool(_transform, gameObject);
        }
    }

    public class ParticlePoolingReturn : MonoBehaviour {
        private IPoolingService _pooling;

        public void SetPooling(IPoolingService pooling) {
            _pooling = pooling;
        }

        private void OnParticleSystemStopped() {
            _pooling.ReturnObjectToPool(gameObject);
        }
    }

    public class ParticlePoolingTypeReturn : MonoBehaviour {
        private IPoolingService _pooling;
        private ParticleSystem _component;

        public void SetComponent(ParticleSystem component, IPoolingService pooling) {
            _component = component;
            _pooling = pooling;
        }

        private void OnParticleSystemStopped() {
            _pooling.ReturnObjectToPool(_component, gameObject);
        }
    }

    public class ParticlePoolingTransformReturn : MonoBehaviour {
        private IPoolingService _pooling;
        private Transform _transform;

        public void SetComponent(Transform component, IPoolingService pooling) {
            _transform = component;
            _pooling = pooling;
        }

        private void OnParticleSystemStopped() {
            _pooling.ReturnObjectToPool(_transform, gameObject);
        }
    }
}
