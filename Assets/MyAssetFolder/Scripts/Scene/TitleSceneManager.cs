using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class TitleSceneManager: MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("参照")]
    [SerializeField] private Button startBtn;

    [Header("遷移先")]
    [SerializeField] private string nextScene = "MainScene";

    //呼び出し
    private Image startBtnImage;
    private Color btnColor;
    private bool isSpacePressed = false;
    [SerializeField] private bool isBtnPressed = false;
    [SerializeField] private bool isBtnReleased = false;

    void Start()
    {
        startBtn = GetComponent<Button>();
        startBtnImage = GetComponent<Image>();
        btnColor = startBtnImage.color;
    }


    void Update()
    {
        
    }

    public void StartSceneBtn()
    {
        
    }

    // マウスクリックの処理
    public void OnPointerDown(PointerEventData eventData)
    {
        if (startBtnImage != null)
        {
            startBtnImage.color = Color.black;
            isBtnPressed = true;

        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        isBtnPressed = false;
        isBtnReleased = true;
        SceneManager.LoadScene(nextScene);
    }
}
