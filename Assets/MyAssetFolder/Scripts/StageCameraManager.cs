using UnityEngine;

public class StageCameraManager : MonoBehaviour
{
    [Header("追従するプレイヤー")]
    [SerializeField] private Transform player;

    [Header("一画面のサイズ")]
    [SerializeField] private float roomWidth = 18f;
    [SerializeField] private float roomHeight = 12f;

    [Header("カメラ移動速度")]
    [SerializeField] private float moveSpeed = 0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;
        float targetX = Mathf.Round(player.position.x / roomWidth) * roomWidth;
        float targetY = Mathf.Round(player.position.y / roomHeight) * roomHeight;

        Vector3 targetPos = new Vector3(targetX, targetY, transform.position.z);

        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }
}
