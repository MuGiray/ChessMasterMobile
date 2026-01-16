using UnityEngine;
using UnityEngine.SceneManagement; // Sahne geçişi için şart

namespace Chess.Unity.Managers
{
    public class MenuController : MonoBehaviour
    {
        public void PlayGame()
        {
            // "SampleScene" senin oyun sahnennin adı. 
            // Eğer değiştirdiysen burayı güncelle.
            SceneManager.LoadScene("SampleScene");
        }

        public void QuitGame()
        {
            Debug.Log("QUIT REQUESTED"); // Editörde kapanmaz, log düşer.
            Application.Quit();
        }
    }
}