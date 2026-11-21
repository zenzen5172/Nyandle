using UnityEngine;

public class GameQuitManager : MonoBehaviour
{
    void Update()
    {
        // Shiftキー（左または右）が押されているかを判定
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.Escape);
        // Shiftキーを押している状態で、Escキーが押された瞬間
        if (isShiftPressed && Input.GetKeyDown(KeyCode.LeftControl))
        {
            QuitGame();
        }
    }

    private void QuitGame()
    {
        // Unityエディタ上での実行時は、プレイモードを停止する
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ビルドされた実際のゲーム（exeファイルなど）を終了する
        Application.Quit();
#endif
    }
}