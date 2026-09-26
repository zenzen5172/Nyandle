using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    //動く速さ
    [SerializeField] float moveSpeed;

    //ジャンプ用 
    [SerializeField] float jumpPower = 5f;
    public bool jumpable = false;
    public bool isGround = true;
    [SerializeField] private float checkRadius = 0.3f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;

    [SerializeField] Color glowColor = Color.yellow;
    [SerializeField] float fireDuration = 0.1f;
    private float fireTimer = 0f;

    //処理用呼び出し
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;

    //リスポーン用
    public Vector2 respawnPoint;

    //大砲用
    public bool canControl = true;
    public bool isLaunched = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        respawnPoint = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        if (canControl)
        {
            jump();
        }

        if (fireTimer > 0)
        {
            fireTimer -= Time.deltaTime;
            sr.color = Color.orange;
        }
        else
        {
            if (jumpable || isGround)
            {
                sr.color = glowColor;
            }
            else
            {
                sr.color = Color.gray;
            }
        }
    }

    private void FixedUpdate()
    {
        isGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (isGround)
        {
            isLaunched = false;
        }

        if (canControl)
        {
            Walk();
        }
        isGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
    }

    // メソッド

    //歩く
    private void Walk()
    {
        float direction = Input.GetAxisRaw("Horizontal"); //入力の左右を取得
        
        if (isGround || direction != 0 || !isLaunched)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }

        //プレイヤーの顔の向き（普通右向き、flipXで左）
        if (direction > 0)
        {
            sr.flipX = false;
        }
        else if (direction < 0)
        {
            sr.flipX = true;
        }

        animator.SetBool("Walk", direction != 0);
    }

    //ジャンプ
    private void jump()
    {
        if (isGround)
        {
            jumpable = false;
        }

        bool canGroundJump = isGround && rb.linearVelocity.y <= 0f;

        if (jumpable || canGroundJump)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                jumpable = false;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);

                fireTimer = fireDuration;

                jumpable = false;
            }
        }
    }

    //リスポーン
    public void Respawn()
    {
        transform.position = respawnPoint;
        rb.linearVelocity = Vector2.zero; //速さのリセット
        jumpable = false;

        canControl = true;
        sr.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    // 今燃えている（火が付けられる状態）
    public bool IsOnFire()
    {
        return fireTimer > 0;
    }
}
