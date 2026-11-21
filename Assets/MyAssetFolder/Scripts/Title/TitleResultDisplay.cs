using System.Text;
using TMPro;
using UnityEngine;

public class TitleResultDisplay : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private GameObject rankingPanel;
    [SerializeField] private TextMeshProUGUI modeTitleText;
    [SerializeField] private TextMeshProUGUI rankingText;

    // ★変更1：初期値を ByTorches（松明順）に変更
    private RankingSortMode currentMode = RankingSortMode.ByTorches;

    // ★変更2：他のスクリプトから「今開いているか？」が分かるように public static にする
    public static bool isRankingOpen = false;

    void Start()
    {
        if (rankingPanel != null) rankingPanel.SetActive(false);
        isRankingOpen = false; // 初期化
    }

    void Update()
    {
        // Zキーで開く / 閉じる
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isRankingOpen = !isRankingOpen;
            rankingPanel.SetActive(isRankingOpen);

            if (isRankingOpen)
            {
                UpdateRankingUI();
            }
        }

        // 開いている時に Spaceキー を押しても閉じる
        if (isRankingOpen && Input.GetKeyDown(KeyCode.Space))
        {
            isRankingOpen = false;
            rankingPanel.SetActive(false);
        }

        // 開いている時に Xキー で切り替え
        if (isRankingOpen && Input.GetKeyDown(KeyCode.X))
        {
            currentMode = (currentMode == RankingSortMode.ByTime)
                ? RankingSortMode.ByTorches
                : RankingSortMode.ByTime;

            UpdateRankingUI();
        }
    }

    public void UpdateRankingUI()
    {
        RankingData data = RankingManager.LoadRanking(currentMode);

        if (modeTitleText != null)
        {
            modeTitleText.text = (currentMode == RankingSortMode.ByTime)
                ? "ランキング：クリア時間順\n<size=50%>Xキーで松明順に変更</size>"
                : "ランキング：松明の数順\n<size=50%>Xキーで時間順に変更</size>";
        }

        if (rankingText != null)
        {
            if (data.records.Count == 0)
            {
                rankingText.text = "まだ記録がありません";
                return;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < data.records.Count; i++)
            {
                ClearRecord rec = data.records[i];
                int rank = i + 1;

                if (currentMode == RankingSortMode.ByTime)
                {
                    // ★名前を表示に追加
                    sb.AppendLine($"{rank}位 : {rec.playerName}  -  {rec.time:F2}秒   (松明: {rec.activeTorches}/{rec.totalTorches})");
                }
                else
                {
                    // ★名前を表示に追加
                    sb.AppendLine($"{rank}位 : {rec.playerName}  -  松明 {rec.activeTorches}/{rec.totalTorches}   ({rec.time:F2}秒)");
                }
            }
            rankingText.text = sb.ToString();
        }
    }
}