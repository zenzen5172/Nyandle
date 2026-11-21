using UnityEngine;
using UnityEngine.SceneManagement;

public class EscEnter : MonoBehaviour
{
    void Update()
    {
        // 左Shift と Esc の両方が押されている状態で、どちらかが押された瞬間に実行
        if ((Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape)) ||
            (Input.GetKey(KeyCode.Escape) && Input.GetKeyDown(KeyCode.LeftShift)))
        {
            Debug.Log("同時押し成功！シーン移動を実行します");
            SceneManager.LoadScene("TitleScene");
        }
    }
}