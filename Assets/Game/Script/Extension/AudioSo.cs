namespace Script.ScriptableObject.Audio {
    using UnityEngine;
    [CreateAssetMenu(fileName = "Audio", menuName = "Scriptable Objects/AudioSo")]
    public class AudioSo : ScriptableObject {
        public AudioClip clickSound;
        public AudioClip earnSound;
    }
}