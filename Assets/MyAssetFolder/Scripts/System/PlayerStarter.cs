using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStarter : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_EDITOR
        this.enabled = false;
#else
        this.enabled = true; //UnityEditor起動中のため、灰色でOK
#endif
    }


    [SerializeField] private float spawnX = -45f;
    [SerializeField] private float spawnY = -2.5f;
    // 初期スポーン用
    void Start()
    {
        transform.position = new Vector2(spawnX, spawnY);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name + " scene loaded");
    }

}
