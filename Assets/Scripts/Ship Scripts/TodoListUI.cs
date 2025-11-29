using UnityEngine;
using TMPro;

public class TodoListUI : MonoBehaviour
{
    public static TodoListUI Instance;

    [SerializeField] private TMP_Text todoText;

    private void Awake()
    {
        Instance = this;
        todoText.gameObject.SetActive(false);
    }

    public void SetTask(string text)
    {
        todoText.text = "• " + text;
        todoText.gameObject.SetActive(true);
    }

    public void ClearTask()
    {
        todoText.gameObject.SetActive(false);
    }
}
