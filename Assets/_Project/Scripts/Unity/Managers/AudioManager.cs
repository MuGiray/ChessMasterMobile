using UnityEngine;

namespace Chess.Unity.Managers
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _moveSound;
        [SerializeField] private AudioClip _captureSound;
        [SerializeField] private AudioClip _notifySound; // Şah çekme vs.
        [SerializeField] private AudioClip _gameOverSound;

        private AudioSource _source;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            _source = GetComponent<AudioSource>();
        }

        public void PlayMove()
        {
            PlayClip(_moveSound);
        }

        public void PlayCapture()
        {
            PlayClip(_captureSound);
        }

        public void PlayNotify()
        {
            PlayClip(_notifySound);
        }

        public void PlayGameOver()
        {
            PlayClip(_gameOverSound);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null)
            {
                // PlayOneShot: Üst üste ses çalmaya izin verir (Hızlı hamlelerde kesilmez)
                _source.PlayOneShot(clip); 
            }
        }
    }
}