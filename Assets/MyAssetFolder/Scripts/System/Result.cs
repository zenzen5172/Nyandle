using System.Collections;
using TMPro;
using UnityEngine;

public class Result : MonoBehaviour
{
    [Header("リザルト表示用TMP")]
    [SerializeField] private TextMeshProUGUI timeInCave;
    [SerializeField] private TextMeshProUGUI numberOfTorch;

    [Header("Space案内用TMP")] // ← ※これはもう使わないかもしれませんが念のため残しておきます
    [SerializeField] private TextMeshProUGUI spacePromptText;
    [SerializeField] private float spaceFadeDuration = 2.0f;
    [SerializeField, Range(0f, 1f)] private float spaceMaxAlpha = 0.6f;

    [Header("演出設定")]
    [SerializeField] private float textSpeed = 0.1f;

    private CaveTimer caveTimer;

    // ★保存用に変数をクラス全体で保持するように変更
    private float finalTime;
    private int finalActiveCount;
    private int finalTotalCount;

    void Start()
    {
        caveTimer = GetComponent<CaveTimer>();
        if (caveTimer == null) caveTimer = FindObjectOfType<CaveTimer>();

        if (timeInCave != null) timeInCave.gameObject.SetActive(false);
        if (numberOfTorch != null) numberOfTorch.gameObject.SetActive(false);
        if (spacePromptText != null) spacePromptText.gameObject.SetActive(false);
    }

    public IEnumerator ShowResultUI()
    {
        // 時間と松明の数を計算して保持しておく
        finalTime = (caveTimer != null) ? caveTimer.caveTime : 0f;
        (finalActiveCount, finalTotalCount) = CountTorches();

        if (timeInCave != null)
        {
            timeInCave.transform.SetAsLastSibling();
            timeInCave.gameObject.SetActive(true);
            timeInCave.text = "";

            string timeText = "Time: " + finalTime.ToString("F2") + "s";

            foreach (char c in timeText)
            {
                timeInCave.text += c;
                yield return new WaitForSeconds(textSpeed);
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (numberOfTorch != null)
        {
            numberOfTorch.transform.SetAsLastSibling();
            numberOfTorch.gameObject.SetActive(true);
            numberOfTorch.text = "";

            string torchText = $"Torches: {finalActiveCount} / {finalTotalCount}";

            foreach (char c in torchText)
            {
                numberOfTorch.text += c;
                yield return new WaitForSeconds(textSpeed);
            }
        }

        // ★ 注意：ここにあった RankingManager.AddRecord は削除しました！
    }

    // ★追加：Elevator.csから、名前入力が終わったタイミングで呼ばれる保存用メソッド
    public void SaveRecordWithName(string playerName)
    {
        RankingManager.AddRecord(playerName, finalTime, finalActiveCount, finalTotalCount);
    }

    public void ShowSpacePrompt()
    {
        if (spacePromptText != null)
        {
            StartCoroutine(FadeInSpaceText());
        }
    }

    private IEnumerator FadeInSpaceText()
    {
        spacePromptText.transform.SetAsLastSibling();
        spacePromptText.gameObject.SetActive(true);

        Color c = spacePromptText.color;
        c.a = 0f;
        spacePromptText.color = c;

        float timer = 0f;
        while (timer < spaceFadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, spaceMaxAlpha, timer / spaceFadeDuration);
            spacePromptText.color = c;
            yield return null;
        }

        c.a = spaceMaxAlpha;
        spacePromptText.color = c;
    }

    private (int active, int total) CountTorches()
    {
        int activeCount = 0;
        int totalCount = 0;

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("TorchSet") && obj.name != "TorchSets")
            {
                totalCount++;

                Transform fire = obj.transform.Find("Fire");
                if (fire != null && fire.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
            }
        }
        return (activeCount, totalCount);
    }
}