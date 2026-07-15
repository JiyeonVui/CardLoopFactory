using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.Engine.Manager.Pooling;
using UnityEngine;

namespace Extension {
    public static class ServiceInitializer {
        private static readonly object Lock = new();
        private static UniTask? _initializeTask;

        /// <summary>
        /// Init with async.
        /// </summary>
        public static UniTask InitializeAsync() {
            lock (Lock) {
                _initializeTask ??= InitializeImpl().Preserve();
                return _initializeTask.Value;
            }
        }

        private static async UniTask InitializeImpl() {
            try {
                await CreateServices();
            } catch (Exception ex) {
                Debug.LogError($"[Init][Exception] {ex}\n{ex.StackTrace}");
                throw;
            }
        }
        
        
        private static async UniTask CreateServices() {

            await UniTask.SwitchToMainThread();
            var services = ServiceLocator.Instance;
            
            var poolingService = new PoolingService();
            services.Provide(poolingService);
            
            var beltController = new BeltController();
            services.Provide(beltController);
            
            var trayController = new TrayController();
            services.Provide(trayController);

            var matchColorController = new MatchColorController();
            services.Provide(matchColorController);
            
            var managerAudio = new ManagerAudio();
            await managerAudio.Initialize();
            services.Provide(managerAudio);

        }
    }
}
