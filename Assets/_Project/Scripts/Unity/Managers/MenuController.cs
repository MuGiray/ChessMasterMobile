using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chess.Unity.Managers
{
    public class MenuController : MonoBehaviour
    {
        private const string GAME_SCENE_NAME = "SampleScene";

        public void PlayGame()
        {
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }

        public void QuitGame()
        {
            Debug.Log("QUIT APP");
            Application.Quit();
        }
    }
}