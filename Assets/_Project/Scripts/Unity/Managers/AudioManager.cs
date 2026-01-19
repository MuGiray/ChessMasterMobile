using UnityEngine;
using Chess.Core.Models; // GameSettings'i görmesi için ŞART

namespace Chess.Unity.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _moveClip;
        [SerializeField] private AudioClip _captureClip;
        [SerializeField] private AudioClip _notifyClip;
        [SerializeField] private AudioClip _gameOverClip;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // GlobalManagers altında olduğu için DontDestroyOnLoad gerekebilir veya parent halleder.
            // Zaten GlobalManagers yapısında olduğu için elle DontDestroy'a gerek yok.
            
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        // --- GÜNCELLENEN METODLAR (AYAR KONTROLLÜ) ---

        public void PlayMove()
        {
            if (!GameSettings.SfxEnabled) return;
            PlayClip(_moveClip);
        }

        public void PlayCapture()
        {
            if (!GameSettings.SfxEnabled) return;
            PlayClip(_captureClip);
        }

        public void PlayNotify()
        {
            if (!GameSettings.SfxEnabled) return;
            PlayClip(_notifyClip);
        }

        public void PlayGameOver()
        {
            if (!GameSettings.SfxEnabled) return;
            PlayClip(_gameOverClip);
        }
    }
}