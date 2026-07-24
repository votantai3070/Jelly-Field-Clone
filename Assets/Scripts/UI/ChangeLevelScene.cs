using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChangeLevelScene : MonoBehaviour
{
    private Button button;
    [SerializeField] private string scene;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(NextLevel);
    }

    public void NextLevel()
    {
        SceneManager.LoadScene(scene);
    }
}