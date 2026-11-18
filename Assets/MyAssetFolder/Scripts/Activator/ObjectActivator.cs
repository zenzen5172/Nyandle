using System.Threading.Tasks;
using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
    [SerializeField] GameObject[] targetObjects;
    [SerializeField] int activateTime = 500;
    [SerializeField] private bool wasTouched = false;

    void Start()
    {
        foreach (GameObject target in targetObjects)
        {
            if (target != null) target.SetActive(false);
        }
    }

    // 変更点1: Unityのイベント関数は「async Task」ではなく「async void」にする
    private async void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤー以外が触れた場合は無視する（必要に応じて）
        if (!other.CompareTag("Player")) return;

        // 変更点2: フラグは「待機する前」に立てる
        if (wasTouched) return;
        wasTouched = true;

        await Task.Delay(activateTime);

        // 変更点3: 待機中にこのオブジェクトが破棄されていた場合のエラーを防ぐ
        if (this == null) return;

        foreach (GameObject target in targetObjects)
        {
            if (target != null) target.SetActive(true);
        }

        gameObject.SetActive(false);
    }
}