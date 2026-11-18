using UnityEngine;
using UnityEngine.SceneManagement;

public class EscEnter : MonoBehaviour
{
    void Update()
    {
        // テスト1：左Shiftが押された瞬間に反応するか
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Debug.Log("左Shiftキーが認識されました");
        }

        // テスト2：Escが押された瞬間に反応するか
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escキーが認識されました");
        }

        // 本命の処理
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("同時押し成功！シーン移動を実行します");
            SceneManager.LoadScene("TitleScene");
        }
    }
}