using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TitleSceneManager : MonoBehaviour
{
    [Header("遷移先")]
    [SerializeField] private string nextScene = "MainScene";

    private bool isLoading;

    void Update()
    {
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) StartSceneBtn();
    }

    public void StartSceneBtn()
    {
        if (isLoading) return;

        isLoading = true;
        SceneManager.LoadSceneAsync(nextScene);
    }
}
