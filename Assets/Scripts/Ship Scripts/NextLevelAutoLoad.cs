using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelAutoLoad : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadNext();
        }
    }

    private void LoadNext()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(next);
        }
        else
        {
            Debug.LogWarning("NextLevelAutoLoad: следующей сцены нет в Build Settings.");
        }
    }
}
