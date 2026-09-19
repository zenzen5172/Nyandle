using UnityEngine;

public class KeyController : MonoBehaviour
{
    private bool isFollowing = false;
    private Transform playerTransform;
    public float followSpeed = 5f;

    // 2D用にX・Y軸で追従位置を調整（例：プレイヤーの少し左上）
    public Vector3 offset = new Vector3(-1f, 0.5f, 0f);

    void Update()
    {
        if (isFollowing && playerTransform != null)
        {
            // 目標座標を計算して滑らかに移動
            Vector3 targetPosition = playerTransform.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }

    // 2D用のトリガー判定
    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーが触れたら追従を開始
        if (!isFollowing && other.CompareTag("Player"))
        {
            isFollowing = true;
            playerTransform = other.transform;

            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.hasKey = true;
                inventory.currentKey = gameObject;
            }
        }
    }
}