using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Opening : MonoBehaviour
{
    [SerializeField] List<GameObject> openingScenes = new List<GameObject>();
    public GameObject cat;
    private int openingCount = 0;
    private bool isBGM = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.Stop();
        isBGM = false;
        openingCount = 0;
        foreach (GameObject openingScene in openingScenes)
        {
            openingScene.SetActive(false);
        }
    }

    void Update()
    {
        // 修正1: openingCount がリストの要素数より小さい時だけ実行する（範囲外エラーを防止）
        if (openingCount < openingScenes.Count)
        {
            if (!openingScenes[openingCount].activeSelf)
            {
                openingScenes[openingCount].SetActive(true);
            }
        }

        if (openingCount >= 2)
        {
            cat.SetActive(false);
        }

        // 修正2: countが2以下の時はOpeningChange、3以上の時はSceneChangeを呼ぶように分岐
        if (openingCount > 2)
        {
            SceneChange();
        }
        else
        {
            OpeningChange();
        }
    }

    private void OpeningChange()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            // 要素数を超えるエラーを防ぐための念の為のチェック
            if (openingCount < openingScenes.Count)
            {
                openingScenes[openingCount].SetActive(false);
            }
            openingCount++;

            if (!isBGM)
            {
                audioSource.time = 0f;
                audioSource.Play();
                isBGM = true;
            }
        }
    }

    private void SceneChange()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene("MainScene");
        }
    }
}