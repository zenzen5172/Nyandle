using System.Collections.Generic;
using UnityEngine;

public class IgnoreFire : MonoBehaviour, IIgnitable
{
    public GameObject fire;

    [Header("松明の光でコウモリにダメージ")]
    public bool damageBats = true;
    public float batDamageRadius = 3f;       // この範囲内のコウモリにダメージ（Sceneビューで選択すると範囲が表示される）
    public float batDamageMultiplier = 1f;   // ダメージの強さ（1でPlayerの光の外縁と同じ速さ）

    // 火が付いている松明の一覧（BatEnemyが参照する）
    public static readonly List<IgnoreFire> All = new List<IgnoreFire>();

    public bool IsLit => fire != null && fire.activeInHierarchy;
    public Vector2 LightCenter => fire != null ? (Vector2)fire.transform.position : (Vector2)transform.position;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
            }
        }
    }

    // 火の矢が当たったときも着火する
    public void Ignite(GameObject source)
    {
        if (fire != null)
        {
            fire.SetActive(true);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!damageBats) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(LightCenter, batDamageRadius);
    }
}
