using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Elevator : MonoBehaviour
{
    [Header("初期位置・移動設定")]
    [SerializeField] private float elevatorX;
    [SerializeField] private float elevatorY;
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("目標地点")]
    [SerializeField] private Vector2 openingTargetPosition;
    [SerializeField] private Vector2 endingTargetPosition;

    [Header("エンディング演出設定(カメラ)")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float targetCameraSize = 15.0f;
    [SerializeField] private Vector3 panStartPosition = new Vector3(-30f, 0f, -10f);
    [SerializeField] private Vector3 panEndPosition = new Vector3(30f, 0f, -10f);
    [SerializeField] private float zoomOutDuration = 0.5f;
    [SerializeField] private float panDuration = 5.0f;
    [SerializeField] private float zoomInDuration = 0.5f;

    [Header("エンディング演出設定(タイミング・フェード)")]
    [SerializeField] private float resultTextDelay = 1.0f;
    [SerializeField] private float delayBeforeAscend = 1.0f;
    [SerializeField] private float delayBeforeFade = 2.0f;
    [SerializeField] private float fullFadeDuration = 4.0f;

    [SerializeField] private Image fadeImage;
    [SerializeField] private string nextSceneName = "TitleScene";

    [Header("名前入力設定")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField, Range(0f, 1f)] private float inputPanelShowTiming = 0.7f;

    // ★追加：一文字ずつ表示する案内テキストの設定
    [Header("案内テキスト設定")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string promptMessage = "なまえを入力してEnter";
    [SerializeField] private float promptTextSpeed = 0.1f;

    [Header("状態フラグ")]
    public bool isOpening = true;
    public bool canEnding = false;
    public bool isEnding = false;

    private bool movingSwitch = true;
    private bool hasStartedEndingSequence = false;
    private bool canAscend = false;
    private bool isCameraLocked = false;
    private Vector3 lockedCameraPosition;
    private float lockedCameraSize;

    [Header("サウンド設定")]
    [SerializeField] private AudioClip elevatorSound;
    [SerializeField] private float audioStartDelay = 0.5f;
    [SerializeField] private float audioFadeDuration = 1.0f;
    public AudioSource audioSource;
    private Rigidbody2D elevatorRb;
    private float initialVolume = 1.0f;

    void Start()
    {
        elevatorRb = GetComponent<Rigidbody2D>();
        if (elevatorRb == null) elevatorRb = gameObject.AddComponent<Rigidbody2D>();
        elevatorRb.bodyType = RigidbodyType2D.Kinematic;

        transform.position = new Vector2(elevatorX, elevatorY);

        audioSource = GetComponent<AudioSource>();
        if (elevatorSound != null) audioSource.clip = elevatorSound;

        if (audioSource != null)
        {
            initialVolume = audioSource.volume;
            audioSource.Stop();
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (promptText != null) promptText.text = ""; // ★最初は文字を空にしておく

        if (mainCamera == null) mainCamera = Camera.main;
    }

    void FixedUpdate()
    {
        if (isOpening) OpeningElevator();
        else if (isEnding) EndingElevator();
    }

    void LateUpdate()
    {
        if (isCameraLocked && mainCamera != null)
        {
            mainCamera.transform.position = lockedCameraPosition;
            mainCamera.orthographicSize = lockedCameraSize;
        }
    }

    private void OpeningElevator()
    {
        MoveElevator(openingTargetPosition, () => { isOpening = false; canEnding = true; });
    }

    public void EndingElevator()
    {
        if (!hasStartedEndingSequence)
        {
            hasStartedEndingSequence = true;
            StartCoroutine(PlayEndingAudioImmediately());
            StartCoroutine(EndingSequence());
        }

        if (canAscend)
        {
            MoveElevator(endingTargetPosition, () => { isEnding = false; });
        }
    }

    private IEnumerator PlayEndingAudioImmediately()
    {
        yield return new WaitForSeconds(audioStartDelay);
        if (audioSource != null)
        {
            audioSource.volume = initialVolume;
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private IEnumerator EndingSequence()
    {
        if (fadeImage != null) fadeImage.transform.SetAsFirstSibling();

        if (mainCamera != null)
        {
            mainCamera.transform.SetParent(null);
        }

        StartCoroutine(StartResultTextWithDelay());

        if (mainCamera != null)
        {
            float originalSize = mainCamera.orthographicSize;
            Vector3 originalPos = mainCamera.transform.position;

            float timer = 0f;
            while (timer < zoomOutDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / zoomOutDuration);
                mainCamera.orthographicSize = Mathf.Lerp(originalSize, targetCameraSize, t);
                mainCamera.transform.position = Vector3.Lerp(originalPos, panStartPosition, t);
                yield return null;
            }
            timer = 0f;
            while (timer < panDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / panDuration);
                mainCamera.transform.position = Vector3.Lerp(panStartPosition, panEndPosition, t);
                yield return null;
            }
            timer = 0f;
            while (timer < zoomInDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / zoomInDuration);
                mainCamera.orthographicSize = Mathf.Lerp(targetCameraSize, originalSize, t);
                mainCamera.transform.position = Vector3.Lerp(panEndPosition, originalPos, t);
                yield return null;
            }

            lockedCameraPosition = mainCamera.transform.position;
            lockedCameraSize = mainCamera.orthographicSize;
            isCameraLocked = true;
        }

        yield return new WaitForSeconds(delayBeforeAscend);
        canAscend = true;
        yield return new WaitForSeconds(delayBeforeFade);

        if (fadeImage != null) fadeImage.gameObject.SetActive(true);

        float fadeTimer = 0f;
        bool isPanelShown = false;

        while (fadeTimer < fullFadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float progress = fadeTimer / fullFadeDuration;

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = Mathf.Clamp01(progress);
                fadeImage.color = c;
            }

            if (audioSource != null && audioSource.isPlaying && isEnding)
            {
                audioSource.volume = Mathf.Lerp(initialVolume, 0f, progress);
            }

            if (!isPanelShown && progress >= inputPanelShowTiming)
            {
                isPanelShown = true;
                if (nameInputPanel != null)
                {
                    nameInputPanel.SetActive(true);

                    // ★追加：パネルが出た瞬間に、一文字ずつ表示するコルーチンをスタート
                    if (promptText != null)
                    {
                        StartCoroutine(ShowPromptTextCoroutine());
                    }

                    if (nameInputField != null)
                    {
                        nameInputField.Select();
                        nameInputField.ActivateInputField();
                    }
                }
            }

            yield return null;
        }

        if (!isPanelShown && nameInputPanel != null)
        {
            nameInputPanel.SetActive(true);
            if (promptText != null) StartCoroutine(ShowPromptTextCoroutine());
            if (nameInputField != null)
            {
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }
        }

        if (nameInputPanel != null)
        {
            bool isNameEntered = false;
            while (!isNameEntered)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    isNameEntered = true;
                }
                yield return null;
            }

            string enteredName = (nameInputField != null) ? nameInputField.text : "";
            Result result = GetComponent<Result>();
            if (result == null) result = FindObjectOfType<Result>();
            if (result != null) result.SaveRecordWithName(enteredName);
        }
        else
        {
            Result result = GetComponent<Result>();
            if (result == null) result = FindObjectOfType<Result>();
            if (result != null) result.SaveRecordWithName("");
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // ★追加：一文字ずつ表示するための処理
    private IEnumerator ShowPromptTextCoroutine()
    {
        promptText.text = "";
        foreach (char c in promptMessage)
        {
            promptText.text += c;
            yield return new WaitForSeconds(promptTextSpeed);
        }
    }

    private IEnumerator StartResultTextWithDelay()
    {
        yield return new WaitForSeconds(resultTextDelay);

        Result result = GetComponent<Result>();
        if (result == null) result = FindObjectOfType<Result>();
        if (result != null) StartCoroutine(result.ShowResultUI());
    }

    private void MoveElevator(Vector2 target, System.Action onArrived)
    {
        if (movingSwitch)
        {
            movingSwitch = false;
            if (isOpening)
            {
                StartCoroutine(PlayElevatorAudioWithDelay());
            }
        }

        Vector2 nextPos = Vector2.MoveTowards(elevatorRb.position, target, moveSpeed * Time.fixedDeltaTime);
        elevatorRb.MovePosition(nextPos);

        if (Vector2.Distance(elevatorRb.position, target) < 0.01f)
        {
            elevatorRb.position = target;
            movingSwitch = true;

            StartCoroutine(FadeOutArrivalAudio());
            onArrived?.Invoke();
        }
    }

    private IEnumerator PlayElevatorAudioWithDelay()
    {
        yield return new WaitForSeconds(audioStartDelay);
        if (!movingSwitch && audioSource != null)
        {
            audioSource.volume = initialVolume;
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private IEnumerator FadeOutArrivalAudio()
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < audioFadeDuration)
        {
            timer += Time.deltaTime;
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / audioFadeDuration);
            }
            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}