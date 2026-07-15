using System;
using System.Collections.Generic;

using Cysharp.Threading.Tasks;
using Extension;
using Script.ScriptableObject.Audio;


using UnityEngine;
using UnityEngine.Assertions;

namespace Extension {
    public enum AudioState {
        Pause,
        Resume,
        Stop
    }

    [Service(nameof(IManagerAudio))]
    public interface IManagerAudio {
        float AudioVolume { get; }
        float MusicVolume { get; }
        UniTask Initialize();
        void SetMusicVolume(float volume);
        void SetAudioVolume(float volume);
        void PlayAudio(AudioClip audioClip, bool loop = false);
        void StopAudio(AudioClip audioClip);
        void PlayAudio(AudioClip audioClip, Vector3 position);
        void PlayMusic(AudioClip audioClip, bool loop = true);
        void StopMusic();
        void PauseMusic();
        void ResumeMusic();
        void SetAudioState(AudioState audioState);
        void SetMusicState(AudioState audioState);
        void PlayClickSound();
        void PlayPopupSound();
        void PlayEarnSound();
        void PlayDistributeSound();
        void PlayCollectionSound();
        void PlayMatchSound();
        void PlayBackgroundMusic();
    }

    public class ManagerAudio : IManagerAudio {
        private const float MinPlayInterval = 0.05f;

        private AudioSource _audio;
        private AudioSource _music;
        private AudioSo _audioSo;
        public float AudioVolume { get; private set; }
        public float MusicVolume { get; private set; }

        private readonly List<AudioSource> spatialSfxSources = new();

        private AudioClip _lastPlayedClip;
        private float _lastPlayedTime;

        public async UniTask Initialize() {
            await UniTask.SwitchToMainThread();
            _audioSo = await Resources.LoadAsync<AudioSo>("Audio/AudioSo") as AudioSo;

            CreateAudioObject();
            UpdateSoundVolume();
        }

        private void CreateAudioObject() {
            var gameObject = new GameObject { name = "audio" };
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.loop = true;
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
        }

        private void UpdateSoundVolume() {
            AudioVolume = PlayerPrefs.GetFloat("audioVolume", 1f);
            MusicVolume = PlayerPrefs.GetFloat("musicVolume", 1f);
            _audio.volume = AudioVolume;
            _music.volume = MusicVolume;
        }

        public void SetMusicVolume(float volume) {
            _music.volume = volume;
            MusicVolume = volume;
            PlayerPrefs.SetFloat("musicVolume", volume);
        }

        public void SetAudioVolume(float volume) {
            _audio.volume = volume;
            AudioVolume = volume;
            for (var i = 0; i < spatialSfxSources.Count; ++i) {
                SetSpatialSfxVolume(i, volume);
            }
            PlayerPrefs.SetFloat("audioVolume", volume);
        }

        public void PlayAudio(AudioClip audioClip, bool loop = false) {
            if (!audioClip) return;

            if (loop) {
                _audio.clip = audioClip;
                _audio.Play();
            } else {
                if (audioClip == _lastPlayedClip &&
                    Time.unscaledTime - _lastPlayedTime < MinPlayInterval) {
                    return;
                }
                _lastPlayedClip = audioClip;
                _lastPlayedTime = Time.unscaledTime;
                _audio.PlayOneShot(audioClip);
            }
        }

        public void StopAudio(AudioClip audioClip) {
            if (!audioClip) return;

            if (_audio.isPlaying && _audio.clip == audioClip) {
                _audio.Stop();
            }
        }

        public async UniTaskVoid PlayAudioAtPosition(AudioClip audioClip, Vector3 position) {
            if (!audioClip) return;

            var channel = GetAvailableSpatialChannel();
            if (channel < 0) return;

            var source = spatialSfxSources[channel];
            source.transform.position = position;
            source.gameObject.SetActive(true);
            source.loop = false;
            source.clip = audioClip;
            source.volume = AudioVolume;
            source.Play();
            await UniTask.Delay(Mathf.CeilToInt(audioClip.length * 1000));
            if (source) {
                source.gameObject.SetActive(false);
            }
        }

        void IManagerAudio.PlayAudio(AudioClip audioClip, Vector3 position) {
            PlayAudioAtPosition(audioClip, position).Forget();
        }

        public void PlayMusic(AudioClip audioClip, bool loop = true) {
            if (_music.isPlaying && _music.clip == audioClip)
                return;
            _music.clip = audioClip;
            _music.loop = loop;
            _music.Play();
        }

        public void StopMusic() {
            _music.Stop();
        }

        public void PauseMusic() {
            _music.Pause();
        }

        public void ResumeMusic() {
            _music.UnPause();
        }

        public void SetAudioState(AudioState audioState) {
            switch (audioState) {
                case AudioState.Pause:
                    _audio.Pause();
                    break;
                case AudioState.Resume:
                    _audio.Play();
                    break;
                case AudioState.Stop:
                    _audio.Stop();
                    _audio.clip = null;
                    break;
            }
        }

        public void SetMusicState(AudioState audioState) {
            switch (audioState) {
                case AudioState.Pause:
                    _music.Pause();
                    break;
                case AudioState.Resume:
                    _music.Play();
                    break;
                case AudioState.Stop:
                    _music.Stop();
                    break;
            }
        }

        public void PlayClickSound() {
            PlayAudio(_audioSo.clickSound);
        }

        public void PlayPopupSound() {
            PlayAudio(_audioSo.clickSound);
        }

        public void PlayEarnSound() {
            // PlayAudio(_audioSo.earnSound);
        }

        public void PlayDistributeSound() {
            PlayAudio(_audioSo.distributeSound);
        }

        public void PlayCollectionSound() {
            PlayAudio(_audioSo.collectionSound);
        }

        public void PlayMatchSound() {
            PlayAudio(_audioSo.matchSound);
        }

        public void PlayBackgroundMusic() {
            PlayMusic(_audioSo.backgroundSound, loop: true);
        }

        private void SetSpatialSfxVolume(int channel, float volume) {
            Assert.IsTrue(channel < spatialSfxSources.Count);
            var source = spatialSfxSources[channel];
            source.volume = volume;
        }

        private int GetAvailableSpatialChannel() {
            for (var i = 0; i < spatialSfxSources.Count; ++i) {
                var source = spatialSfxSources[i];
                if (!source.gameObject.activeSelf) {
                    return i;
                }
            }

            if (spatialSfxSources.Count > 20) {
                return -1;
            }

            var channel = spatialSfxSources.Count;
            var go = new GameObject($"Channel {channel}");
            go.transform.SetParent(_audio.transform, false);
            var component = go.AddComponent<AudioSource>();
            component.spatialBlend = 1.0f;
            spatialSfxSources.Add(component);
            return channel;
        }
    }
}
