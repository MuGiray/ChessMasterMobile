using UnityEngine;
using UnityEngine.SceneManagement;
using Chess.Core.Models;

namespace Chess.Unity.Managers
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _confirmationPanel; // "Devam mı Yeni mi?" paneli

        private const string GAME_SCENE_NAME = "SampleScene";
        private GameMode _pendingMode; // Kullanıcının girmek istediği mod (Geçici hafıza)

        private void Start()
        {
            // Paneli garanti olsun diye başta kapat
            if (_confirmationPanel != null) _confirmationPanel.SetActive(false);
        }

        // ANA MENÜ BUTONU: 1 Player (AI)
        public void PlayVsAI()
        {
            CheckSaveAndProceed(GameMode.HumanVsAI);
        }

        // ANA MENÜ BUTONU: 2 Players
        public void PlayVsFriend()
        {
            CheckSaveAndProceed(GameMode.HumanVsHuman);
        }

        // Yardımcı Metod: Kayıt kontrolü yapar
        private void CheckSaveAndProceed(GameMode mode)
        {
            _pendingMode = mode; // Seçimi hafızaya al

            // Bu mod için kayıt dosyası var mı?
            if (SaveManager.HasSave(mode))
            {
                // VAR -> Paneli aç, kullanıcıya sor
                _confirmationPanel.SetActive(true);
            }
            else
            {
                // YOK -> Direkt başlat
                LaunchGame();
            }
        }

        // PANEL BUTONU: "DEVAM ET" (Resume)
        public void OnContinueClicked()
        {
            // Dosyayı silme, sahneyi yükle.
            // GameManager dosyayı görüp otomatik yükleyecek.
            LaunchGame();
        }

        // PANEL BUTONU: "YENİ OYUN" (New Game)
        public void OnNewGameClicked()
        {
            // Eski kaydı sil
            SaveManager.DeleteSave(_pendingMode);
            
            // Şimdi sahneyi yükle (GameManager dosya bulamayacak ve sıfırdan açacak)
            LaunchGame();
        }

        // PANEL BUTONU: "İPTAL" (Cancel/X)
        public void OnCancelClicked()
        {
            _confirmationPanel.SetActive(false);
        }

        private void LaunchGame()
        {
            GameSettings.CurrentMode = _pendingMode;
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }

        public void QuitGame()
        {
            Debug.Log("QUIT APP");
            Application.Quit();
        }
    }
}