using UnityEngine;

namespace Chess.Unity.Managers
{
    public class HapticsManager : MonoBehaviour
    {
        public static HapticsManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // --- PUBLIC API ---

        // 1. Hafif (Normal Hamle): 20ms
        public void VibrateLight() => VibrateAndroid(20);

        // 2. Orta (Taş Yeme / Şah / Tuş Sesi): 50ms
        public void VibrateMedium() => VibrateAndroid(50);

        // 3. Ağır (Mat / Oyun Sonu): 100ms
        public void VibrateHeavy() => VibrateAndroid(100);

        // 4. Hata (Geçersiz Hamle): 30ms
        public void VibrateError() => VibrateAndroid(30);


        // --- ANDROID NATIVE KÖPRÜSÜ ---
        private void VibrateAndroid(long milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Unity'nin ana aktivitesine (Current Activity) eriş
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    // Cihazın titreşimi var mı?
                    if (vibrator.Call<bool>("hasVibrator"))
                    {
                        // Titret (Milisaniye cinsinden)
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
            catch (System.Exception)
            {
                // Hata olursa oyunu durdurma, sessizce geç
            }
#endif
        }
    }
}