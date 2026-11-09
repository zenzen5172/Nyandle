using UnityEngine;
using TMPro;

public class CatChat : MonoBehaviour
{
    public TextMeshProUGUI chatTextUI;
    public GameObject backPanel;
    [SerializeField] public int colliderCount = 0;
    void Start()
    {
        HideText();
    }

    // 外部のスクリプトから呼ばれる関数
    public void ShowText(string message)
    {
        chatTextUI.text = message;
        backPanel.SetActive(true);
    }

    public void HideText()
    {
        backPanel.SetActive(false);
    }
}