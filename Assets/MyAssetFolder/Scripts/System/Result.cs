using System.Collections;
using TMPro;
using UnityEngine;

public class Result : MonoBehaviour
{
    [Header("リザルト表示用TMP")]
    [SerializeField] private TextMeshProUGUI timeInCave;
    [SerializeField] private TextMeshProUGUI numberOfTorch;

    [Header("Space案内用TMP")]
    [SerializeField] private TextMeshProUGUI spacePromptText;
    [SerializeField] private float spaceFadeDuration = 2.0f;
    [SerializeField, Range(0f, 1f)] private float spaceMaxAlpha = 0.6f;

    [Header("演出設定")]
    [SerializeField] private float textSpeed = 0.1f;

    private CaveTimer caveTimer;

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
        if (timeInCave != null)
        {
            timeInCave.transform.SetAsLastSibling();
            timeInCave.gameObject.SetActive(true);
            timeInCave.text = "";

            float time = (caveTimer != null) ? caveTimer.caveTime : 0f;
            string timeText = "Time: " + time.ToString("F2") + "s";

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

            (int activeCount, int totalCount) = CountTorches();

            string torchText = $"Torches: {activeCount} / {totalCount}";

            foreach (char c in torchText)
            {
                numberOfTorch.text += c;
                yield return new WaitForSeconds(textSpeed);
            }
        }
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

    // ★変更：直下だけでなく、孫やひ孫オブジェクトまで奥深く「Fire」を探すように改良
    // ★大文字小文字を合わせ、親の「TorchSets」を除外して「TorchSet (○)」だけを正確に数える
    private (int active, int total) CountTorches()
    {
        int activeCount = 0;
        int totalCount = 0;

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            // 「TorchSet」を含み、かつ親フォルダの「TorchSets」ではないものを探す
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

        Debug.Log($"松明カウント結果: {activeCount} / {totalCount}");
        return (activeCount, totalCount);
    }
}