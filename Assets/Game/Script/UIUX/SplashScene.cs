using System;
using Cysharp.Threading.Tasks;
using Extension;


using UnityEngine;

using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Extension {
    public class SplashScene : MonoBehaviour {
        protected async void Start() {
            try
            {
                await ServiceInitializer.InitializeAsync();
                await UniTask.SwitchToMainThread();
                SceneManager.LoadScene("GameScene");
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }
    
}