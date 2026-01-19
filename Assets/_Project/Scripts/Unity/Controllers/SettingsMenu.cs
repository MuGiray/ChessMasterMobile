using UnityEngine;
using UnityEngine.UI;
using Chess.Core.Models;

namespace Chess.Unity.Controllers
{
    public class SettingsMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _hapticsToggle;

        private void Start()
        {
            // Başlangıçta panel kapalı olsun
            if (_panel != null) _panel.SetActive(false);
        }

        // Panel açıldığında değerleri yükle
        public void OpenSettings()
        {
            if (_panel == null) return;
            
            _panel.SetActive(true);

            // Mevcut ayarları UI'a yansıt (Loop'a girmemesi için listener'ı geçici durdurabiliriz ama basit toggle'da gerek yok)
            if (_musicToggle) _musicToggle.isOn = GameSettings.MusicEnabled;
            if (_sfxToggle) _sfxToggle.isOn = GameSettings.SfxEnabled;
            if (_hapticsToggle) _hapticsToggle.isOn = GameSettings.HapticsEnabled;
        }

        public void CloseSettings()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        // --- UNITY EVENT BAĞLANTILARI ---
        
        public void OnMusicToggled(bool value)
        {
            GameSettings.MusicEnabled = value;
            // Varsa arkaplan müziğini anında durdur/başlat mantığı buraya eklenebilir
        }

        public void OnSfxToggled(bool value)
        {
            GameSettings.SfxEnabled = value;
        }

        public void OnHapticsToggled(bool value)
        {
            GameSettings.HapticsEnabled = value;
            if (value) 
            {
                // Kullanıcı açtığında çalıştığını hissetsin diye ufak bir titreşim verelim
                Managers.HapticsManager.Instance?.VibrateLight(); 
            }
        }
    }
}