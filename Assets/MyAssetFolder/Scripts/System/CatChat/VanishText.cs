using System;
using UnityEngine;

public class VanishText : MonoBehaviour
{
    [SerializeField] private float vanishTime = 0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Ok");
        // 接触した相手が "Player" タグを持っているか判定
        if (other.CompareTag("Player"))
        {
            // 自分の1つ上の親が存在するかチェック
            if (transform.parent != null)
            {
                // 親オブジェクトを削除（※子である自分自身も一緒に消滅します）
                Destroy(transform.parent.gameObject, vanishTime);
            }
            else
            {
                // 万が一親が存在しない（ルート階層にいる）場合は、自分自身だけを削除
                Destroy(gameObject, vanishTime);
            }
        }
    }
}