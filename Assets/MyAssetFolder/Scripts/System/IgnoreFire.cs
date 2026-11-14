using System.Collections.Generic;
using UnityEngine;

public class IgnoreFire : MonoBehaviour, IIgnitable
{
    public GameObject fire;
    public AudioClip ignoreSound;
    private AudioSource audioSource;

    [Header("コウモリが火に突っ込むと「熱っ！」と怯む")]
    public bool damageBats = true;
    public float batDamageRadius = 3f;       // この範囲に入ったコウモリが怯む（Sceneビューで選択するとオレンジの円で表示）
    public float batDamageMultiplier = 1f;   // じわじわダメージの強さ（BatEnemyの torchContinuousDamage がONのときだけ使う）

    [Header("コウモリが怖がって避ける（たき火など大きな火に）")]
    public bool scaresBats = true;
    public float batFearRadius = 5f;         // この範囲には近寄らない（範囲内のプレイヤーは襲われない）

    // 火が付いている松明の一覧（BatEnemyが参照する）
    public static readonly List<IgnoreFire> All = new List<IgnoreFire>();

    public bool IsLit => fire != null && fire.activeInHierarchy;
    public Vector2 LightCenter => fire != null ? (Vector2)fire.transform.position : (Vector2)transform.position;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (fire != null)
        {
            fire.SetActive(false);
        }
    }

   void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (fire != null)
            {
                fire.SetActive(true);
                audioSource.PlayOneShot(ignoreSound);

            }
        }
    }

    // 火の矢が当たったときも着火する
    public void Ignite(GameObject source)
    {
        if (fire != null)
        {
            fire.SetActive(true);
            audioSource.PlayOneShot(ignoreSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (damageBats)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
            Gizmos.DrawWireSphere(LightCenter, batDamageRadius);
        }
        if (scaresBats)
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f); // 黄色：コウモリが避ける範囲
            Gizmos.DrawWireSphere(LightCenter, batFearRadius);
        }
    }
}
