using UnityEngine;
using UnityEngine.InputSystem;

public class AllOverCameraManeger : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    [Header("対象のカメラ（未設定ならMain Camera）")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private StageCameraManager stageCamera;

    [Header("俯瞰時のカメラの大きさ（Orthographic Size）")]
    [SerializeField] private float overviewSize = 20f;
    [SerializeField] private float zoomDuration = 0.4f;

    [Header("俯瞰中のマウスホイールでの拡大縮小")]
    [SerializeField] private bool scrollZoom = true;
    [SerializeField] private float minOverviewSize = 8f;
    [SerializeField] private float maxOverviewSize = 40f;
    [SerializeField] private float scrollZoomStep = 0.15f;
    [SerializeField] private float scrollZoomSmoothness = 12f;
    [SerializeField] private bool rememberZoom = true;

    [Header("俯瞰中の視点移動速度（引いているほど速くなる）")]
    [SerializeField] private float panSpeed = 25f;

    [Header("俯瞰中のゲーム進行を止める")]
    [SerializeField] private bool pauseWhileOverview = true;

    [Header("視点移動の範囲（カメラ中心が動ける範囲）")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 boundsMin = new Vector2(-10f, -40f);
    [SerializeField] private Vector2 boundsMax = new Vector2(150f, 10f);

    [SerializeField] private GameObject blackScreen;

    private float normalSize;
    private Vector3 normalPosition;
    private Vector3 returnStartPosition; // ★追加：戻り開始時の位置を正確に記録
    private float zoomTimer;
    private float zoomFromSize;
    private float zoomToSize;
    private bool overviewOn;
    private bool returning;
    private float overviewTargetSize;
    private float savedTimeScale = 1f;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (stageCamera == null && targetCamera != null)
            stageCamera = targetCamera.GetComponent<StageCameraManager>();
        IsActive = false;
        zoomTimer = zoomDuration;

        if (blackScreen != null) blackScreen.SetActive(false); // ★安全のためnullチェック追加
    }

    void OnDisable()
    {
        if (IsActive) EndOverview(instant: true);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || targetCamera == null) return;

        if (kb.zKey.wasPressedThisFrame)
        {
            if (overviewOn) EndOverview(instant: false);
            else if (!returning) BeginOverview();
        }

        float dt = Time.unscaledDeltaTime;

        // ズーム・戻りのアニメーション
        if (IsActive)
        {
            // ★修正：zoomDurationが0以下の場合でも確実に完了処理へ行くように変更
            if (zoomDuration <= 0f)
            {
                targetCamera.orthographicSize = zoomToSize;
                if (returning) FinishReturn();
            }
            else if (zoomTimer < zoomDuration)
            {
                zoomTimer += dt;
                float t = Mathf.SmoothStep(0f, 1f, zoomTimer / zoomDuration);
                targetCamera.orthographicSize = Mathf.Lerp(zoomFromSize, zoomToSize, t);

                if (returning)
                {
                    // ★修正：毎フレーム現在地からLerpするのではなく、開始位置から目的地へ綺麗に補間
                    targetCamera.transform.position = Vector3.Lerp(returnStartPosition, normalPosition, t);
                    if (zoomTimer >= zoomDuration) FinishReturn();
                }
            }
        }

        if (!overviewOn) return;

        // マウスホイールで拡大縮小
        var mouse = Mouse.current;
        if (scrollZoom && mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                overviewTargetSize *= 1f - Mathf.Sign(scroll) * scrollZoomStep;
                overviewTargetSize = Mathf.Clamp(overviewTargetSize, minOverviewSize, maxOverviewSize);
                if (zoomTimer < zoomDuration) zoomToSize = overviewTargetSize;
            }

            if (zoomTimer >= zoomDuration)
            {
                float k = 1f - Mathf.Exp(-scrollZoomSmoothness * dt);
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, overviewTargetSize, k);
            }
        }

        // 十字キー／WASDで視点移動
        Vector2 move = Vector2.zero;
        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) move.x -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) move.x += 1f;
        if (kb.downArrowKey.isPressed || kb.sKey.isPressed) move.y -= 1f;
        if (kb.upArrowKey.isPressed || kb.wKey.isPressed) move.y += 1f;

        if (move != Vector2.zero)
        {
            float speed = panSpeed * (overviewSize > 0f ? targetCamera.orthographicSize / overviewSize : 1f);
            Vector3 pos = targetCamera.transform.position + (Vector3)(move.normalized * speed * dt);
            if (useBounds)
            {
                pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
                pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
            }
            targetCamera.transform.position = pos;
        }
    }

    private void BeginOverview()
    {
        if (blackScreen != null) blackScreen.SetActive(true);

        IsActive = true;
        overviewOn = true;
        returning = false; // ★念のため明示的にfalseに
        normalSize = targetCamera.orthographicSize;
        normalPosition = targetCamera.transform.position;

        if (stageCamera != null) stageCamera.enabled = false;

        if (!rememberZoom || overviewTargetSize <= 0f) overviewTargetSize = overviewSize;
        overviewTargetSize = Mathf.Clamp(overviewTargetSize, minOverviewSize, maxOverviewSize);

        if (pauseWhileOverview)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        StartZoom(overviewTargetSize);
    }

    private void EndOverview(bool instant)
    {
        if (blackScreen != null) blackScreen.SetActive(false);

        overviewOn = false;
        returning = true;
        returnStartPosition = targetCamera.transform.position; // ★戻り始めた瞬間の位置を保存
        StartZoom(normalSize);
        if (instant) FinishReturn();
    }

    private void FinishReturn()
    {
        IsActive = false;
        returning = false;
        zoomTimer = zoomDuration;
        targetCamera.orthographicSize = normalSize;
        targetCamera.transform.position = normalPosition;

        if (stageCamera != null) stageCamera.enabled = true;
        if (pauseWhileOverview) Time.timeScale = savedTimeScale;
    }

    private void StartZoom(float toSize)
    {
        zoomFromSize = targetCamera.orthographicSize;
        zoomToSize = toSize;
        zoomTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.cyan;
        Vector3 center = (boundsMin + boundsMax) / 2f;
        Vector3 size = boundsMax - boundsMin;
        Gizmos.DrawWireCube(center, size);
    }
}