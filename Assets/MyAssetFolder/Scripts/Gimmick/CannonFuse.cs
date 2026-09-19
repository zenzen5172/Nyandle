using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CannonFuse : MonoBehaviour
{
    [Header("導火線の通過ポイント（曲がり角に配置）")]
    [SerializeField] Transform[] waypoints;

    [Header("燃えきる時間（秒）")]
    [SerializeField] float burnTime = 3f;

    [Header("着火判定を持つ子オブジェクト（指定がなければ自身を非表示）")]
    [SerializeField] GameObject triggerObject;

    [Header("失敗時（時間切れ）に復活するまでの時間（秒）")]
    [SerializeField] float reviveDelay = 1f;

    // 🌟【追加】成功時（大砲発射後）に導火線が復活するまでの時間（秒）
    [Header("成功時に復活するまでの時間（秒）")]
    [SerializeField] float successReviveDelay = 3f;

    public bool isLit = false;
    private bool isCompleted = false;

    private LineRenderer lineRenderer;
    private float currentBurnTimer = 0f;
    private Vector3[] originalPositions;

    private Collider2D myCollider;
    private SpriteRenderer mySprite;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (triggerObject == null)
        {
            myCollider = GetComponent<Collider2D>();
            mySprite = GetComponent<SpriteRenderer>();
        }

        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
        lineRenderer.sortingOrder = 10;

        if (waypoints != null && waypoints.Length > 0)
        {
            originalPositions = new Vector3[waypoints.Length];
            lineRenderer.positionCount = waypoints.Length;

            for (int i = 0; i < waypoints.Length; i++)
            {
                originalPositions[i] = waypoints[i].position;
                lineRenderer.SetPosition(i, waypoints[i].position);
            }
        }
    }

    void Update()
    {
        if (isLit && !isCompleted)
        {
            currentBurnTimer += Time.deltaTime;
            float progress = currentBurnTimer / burnTime;

            if (progress >= 1.0f)
            {
                isLit = false;
                lineRenderer.positionCount = 0;

                Invoke(nameof(ResetFuse), reviveDelay);
            }
            else
            {
                UpdateFuseLine(progress);
            }
        }
    }

    private void UpdateFuseLine(float progress)
    {
        if (originalPositions == null || originalPositions.Length < 2) return;

        float totalLength = 0f;
        float[] segmentLengths = new float[originalPositions.Length - 1];
        for (int i = 0; i < originalPositions.Length - 1; i++)
        {
            segmentLengths[i] = Vector3.Distance(originalPositions[i], originalPositions[i + 1]);
            totalLength += segmentLengths[i];
        }

        float burnDistance = totalLength * progress;
        float accumulatedDistance = 0f;
        int startSegmentIndex = 0;

        for (int i = 0; i < segmentLengths.Length; i++)
        {
            if (accumulatedDistance + segmentLengths[i] >= burnDistance)
            {
                startSegmentIndex = i;
                break;
            }
            accumulatedDistance += segmentLengths[i];
        }

        float remainingInSegment = burnDistance - accumulatedDistance;
        float t = remainingInSegment / segmentLengths[startSegmentIndex];
        Vector3 burningPoint = Vector3.Lerp(originalPositions[startSegmentIndex], originalPositions[startSegmentIndex + 1], t);

        int remainingPointCount = originalPositions.Length - startSegmentIndex;
        lineRenderer.positionCount = remainingPointCount;
        lineRenderer.SetPosition(0, burningPoint);

        for (int i = 1; i < remainingPointCount; i++)
        {
            lineRenderer.SetPosition(i, originalPositions[startSegmentIndex + i]);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isLit)
        {
            Player player = collision.GetComponent<Player>();

            if (player != null && player.IsOnFire())
            {
                isLit = true;
                currentBurnTimer = 0f;

                isCompleted = false;
                HideTrigger();
            }
        }
    }

    private void HideTrigger()
    {
        if (triggerObject != null)
        {
            triggerObject.SetActive(false);
        }
        else
        {
            if (myCollider != null) myCollider.enabled = false;
            if (mySprite != null) mySprite.enabled = false;
        }
    }

    private void ShowTrigger()
    {
        if (triggerObject != null)
        {
            triggerObject.SetActive(true);
        }
        else
        {
            if (myCollider != null) myCollider.enabled = true;
            if (mySprite != null) mySprite.enabled = true;
        }
    }

    public void CannonEntered()
    {
        isCompleted = true;
        isLit = false;
        lineRenderer.positionCount = 0;

        CancelInvoke(nameof(ResetFuse)); // 失敗用のタイマーをキャンセル

        // 🌟【追加】大砲発射の成功後、指定した時間（デフォルト3秒）が経過したら復活させる
        Invoke(nameof(ResetFuse), successReviveDelay);
    }

    public void ResetFuse()
    {
        isLit = false;
        isCompleted = false;
        currentBurnTimer = 0f;
        ShowTrigger();

        if (originalPositions != null)
        {
            lineRenderer.positionCount = originalPositions.Length;
            for (int i = 0; i < originalPositions.Length; i++)
            {
                lineRenderer.SetPosition(i, originalPositions[i]);
            }
        }
    }
}