using UnityEngine;
using UnityEngine.SceneManagement; // シーン遷移に必要
using UnityEngine.UI;              // UI(Image)の操作に必要
using System.Collections;          // コルーチン(IEnumerator)に必要

public class Elevator : MonoBehaviour
{
    [Header("初期位置・移動設定")]
    [SerializeField] private float elevatorX;
    [SerializeField] private float elevatorY;
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("目標地点")]
    [SerializeField] private Vector2 openingTargetPosition;
    [SerializeField] private Vector2 endingTargetPosition;

    [Header("シーン遷移・フェード")]
    [SerializeField] private Image fadeImage; // 画面を覆う真っ黒なUI画像
    [SerializeField] private float fadeDuration = 1.5f; // 暗くなるまでの秒数
    [SerializeField] private string nextSceneName = "TitleScene"; // 戻るシーン名

    [Header("状態フラグ")]
    public bool isOpening = true;
    public bool canEnding = false;
    public bool isEnding = false;

    private bool movingSwitch = true;

    [Header("サウンド設定")]
    [SerializeField] private AudioClip elevatorSound;
    public AudioSource audioSource;

    void Start()
    {
        transform.position = new Vector2(elevatorX, elevatorY);
        audioSource = GetComponent<AudioSource>();

        if (elevatorSound != null)
        {
            audioSource.clip = elevatorSound;
        }
        audioSource.Stop();

        // 最初はフェード画像を透明（見えない状態）にしておく
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void Update()
    {
        if (isOpening)
        {
            OpeningElevator();
        }
        else if (isEnding)
        {
            EndingElevator();
        }
    }

    private void OpeningElevator()
    {
        MoveElevator(openingTargetPosition, () =>
        {
            isOpening = false;
            canEnding = true;
        });
    }

    public void EndingElevator()
    {
        MoveElevator(endingTargetPosition, () =>
        {
            isEnding = false; // 移動終了

            // フェードアウトとシーン遷移を開始
            if (fadeImage != null)
            {
                StartCoroutine(FadeOutAndChangeScene());
            }
            else
            {
                // 画像が設定されていない場合は即座に遷移
                SceneManager.LoadScene(nextSceneName);
            }
        });
    }

    // 画面を徐々に暗くして、完了したらシーンを読み込む処理
    private IEnumerator FadeOutAndChangeScene()
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        color = Color.black; // 色を黒に固定
        color.a = 0f;
        float timer = 0f;

        // 指定した秒数(fadeDuration)かけて透明度(a)を1にしていく
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Clamp01(timer / fadeDuration);
            fadeImage.color = color;
            yield return null; // 1フレーム待つ
        }

        // 完全に暗くなったらシーンを移動
        SceneManager.LoadScene(nextSceneName);
    }

    private void MoveElevator(Vector2 target, System.Action onArrived)
    {
        if (movingSwitch)
        {
            audioSource.time = 0f;
            audioSource.Play();
            movingSwitch = false;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target) < 0.01f)
        {
            transform.position = target;
            audioSource.Stop();
            movingSwitch = true;
            onArrived?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}