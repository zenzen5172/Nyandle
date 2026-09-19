using UnityEngine;

public class ReJumpStone : MonoBehaviour
{

    private SpriteRenderer sr;
    private Collider2D col;
    private Player playerRef;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }
    
    private void Update()
    {
        if (playerRef != null && playerRef.isGround) //プレイヤーがこの石を取った後、地面についたら
        {
            sr.color = Color.yellow;
            col.enabled = true;

            playerRef = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            if (player != null)
            {
                player.jumpable = true;

                // 地面についたら再表示するように
                sr.color = Color.gray;
                col.enabled = false;

                playerRef = player;
            }
        }
    }

}
