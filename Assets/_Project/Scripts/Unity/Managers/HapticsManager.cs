using UnityEngine;
using Chess.Core.Models; // GameSettings için ŞART

namespace Chess.Unity.Managers
{
    public class HapticsManager : MonoBehaviour
    {
        public static HapticsManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // --- TİTREŞİM FONKSİYONLARI (AYAR KONTROLLÜ) ---

        public void VibrateLight()
        {
            if (!GameSettings.HapticsEnabled) return;

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Android için basit titreşim (veya Handheld.Vibrate)
            // Daha gelişmişi için Android native çağrıları gerekir ama MVP için bu yeterli.
            // Unity'nin eski sistemi sadece tek tip destekler:
            Handheld.Vibrate(); 
            #else
            Debug.Log("Haptic: Light");
            #endif
        }

        public void VibrateMedium()
        {
            if (!GameSettings.HapticsEnabled) return;

            #if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
            #else
            Debug.Log("Haptic: Medium");
            #endif
        }

        public void VibrateHeavy()
        {
            if (!GameSettings.HapticsEnabled) return;

            #if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
            #else
            Debug.Log("Haptic: Heavy");
            #endif
        }

        public void VibrateError()
        {
            if (!GameSettings.HapticsEnabled) return;

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Hata için peş peşe 2 kez titreşim simülasyonu
            StartCoroutine(VibratePattern());
            #else
            Debug.Log("Haptic: Error");
            #endif
        }

        private System.Collections.IEnumerator VibratePattern()
        {
            Handheld.Vibrate();
            yield return new WaitForSeconds(0.1f);
            Handheld.Vibrate();
        }
    }
}