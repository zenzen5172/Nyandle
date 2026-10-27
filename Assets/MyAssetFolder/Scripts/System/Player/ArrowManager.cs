using UnityEngine;
using TMPro;

public class ArrowManager : MonoBehaviour
{
    public int arrowCount = 0;
    public TextMeshProUGUI countText;

    void Start()
    {
        // ゲーム開始時に画面のテキストを初期化（×0 にする）
        UpdateUI();
    }

    // 矢のアイテム（ItemArrow）を取得した時に呼ばれるメソッド
    public void AddArrow(int amount)
    {
        arrowCount += amount; // 矢の数を増やす
        UpdateUI();           // 矢が増えたので画面の表示を更新する
    }

    // 画面のテキスト表示を書き換える処理
    void UpdateUI()
    {
        if (countText != null)
        {
            countText.text = "×" + arrowCount;
        }
    }
}