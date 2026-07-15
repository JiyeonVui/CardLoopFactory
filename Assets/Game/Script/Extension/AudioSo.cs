namespace Script.ScriptableObject.Audio {
    using UnityEngine;
    [CreateAssetMenu(fileName = "Audio", menuName = "Scriptable Objects/AudioSo")]
    public class AudioSo : ScriptableObject {
        public AudioClip clickSound;
        public AudioClip distributeSound;
        public AudioClip collectionSound;
        public AudioClip matchSound;
        public AudioClip backgroundSound;

    }
}