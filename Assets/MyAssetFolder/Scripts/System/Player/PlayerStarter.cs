using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStarter : MonoBehaviour
{
    [SerializeField] private bool spawnStartPoint = true;
    private void Awake()
    {
#if UNITY_EDITOR
        this.enabled = spawnStartPoint;
#else
        this.enabled = true; //UnityEditor起動中のため、灰色でOK
#endif
    }


    [SerializeField] private float spawnX = -45f;
    [SerializeField] private float spawnY = -2.5f;
    // 初期スポーン用
    void Start()
    {
        Vector2 spawnPoint = new Vector2(spawnX, spawnY);
        transform.position = new Vector3(spawnX, spawnY, transform.position.z);
        if (TryGetComponent(out Player player)) player.respawnPoint = spawnPoint;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name + " scene loaded");
    }

}
