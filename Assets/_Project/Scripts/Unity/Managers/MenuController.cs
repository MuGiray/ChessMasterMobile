using UnityEngine;
using UnityEngine.SceneManagement;
using Chess.Core.Models; // GameSettings erişimi için

namespace Chess.Unity.Managers
{
    public class MenuController : MonoBehaviour
    {
        private const string GAME_SCENE_NAME = "SampleScene";

        // BUTON 1: Yapay Zeka Modu
        public void PlayVsAI()
        {
            GameSettings.CurrentMode = GameMode.HumanVsAI;
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }

        // BUTON 2: Arkadaş Modu
        public void PlayVsFriend()
        {
            GameSettings.CurrentMode = GameMode.HumanVsHuman;
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }

        public void QuitGame()
        {
            Debug.Log("QUIT APP");
            Application.Quit();
        }
    }
}