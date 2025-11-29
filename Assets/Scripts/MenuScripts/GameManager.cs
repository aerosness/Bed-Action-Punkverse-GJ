using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Загрузить следующую сцену (по build index)
    public void LoadNextScene()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(index + 1);
    }

    // Загрузить сцену по имени (если захочешь)
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Выйти из игры
    public void QuitGame()
    {
        // В билде закроет игру, в редакторе — не видно эффекта
        Application.Quit();
    }
}
