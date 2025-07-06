using UnityEngine;
using UnityEngine.UI;

public class SoftwareCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Canvas cursorCanvas;
    [SerializeField] private Image cursorImage;
    [SerializeField] private Texture2D cursorTexture;

    private Vector2 lockedPosition;
    private bool isLocked = false;
    private bool showSoftwareCursor = false;

    void Start()
    {
        // Canvas für Software-Cursor erstellen
        if (!cursorCanvas)
        {
            GameObject canvasObj = new GameObject("CursorCanvas");
            cursorCanvas = canvasObj.AddComponent<Canvas>();
            cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cursorCanvas.sortingOrder = 1000; // Über allem anderen

            // Cursor Image erstellen
            GameObject cursorObj = new GameObject("Cursor");
            cursorObj.transform.SetParent(cursorCanvas.transform);
            cursorImage = cursorObj.AddComponent<Image>();
            cursorImage.sprite = Sprite.Create(cursorTexture,
                new Rect(0, 0, cursorTexture.width, cursorTexture.height),
                new Vector2(0.5f, 0.5f));
            cursorImage.SetNativeSize();
        }

        // Software-Cursor initial verstecken
        ShowSoftwareCursor(false);
    }

    void Update()
    {
        if (isLocked)
        {
            // Software-Cursor an gesperrter Position halten
            if (showSoftwareCursor)
            {
                cursorImage.rectTransform.position = lockedPosition;
            }
        }
        else if (showSoftwareCursor)
        {
            // Software-Cursor folgt Mausposition
            cursorImage.rectTransform.position = Input.mousePosition;
        }

        // Test-Input
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (isLocked) StopMouseLock();
            else StartMouseLock();
        }
    }

    public void StartMouseLock()
    {
        // Aktuelle Mausposition speichern
        lockedPosition = Input.mousePosition;
        isLocked = true;

        // Hardware-Cursor verstecken
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None; // NICHT Locked verwenden!

        // Software-Cursor anzeigen
        ShowSoftwareCursor(true);

        Debug.Log($"Software cursor locked at: {lockedPosition}");
    }

    public void StopMouseLock()
    {
        isLocked = false;

        // Hardware-Cursor wieder anzeigen
        Cursor.visible = true;

        // Software-Cursor verstecken
        ShowSoftwareCursor(false);

        Debug.Log("Software cursor unlocked");
    }

    void ShowSoftwareCursor(bool show)
    {
        showSoftwareCursor = show;
        if (cursorImage)
        {
            cursorImage.gameObject.SetActive(show);
        }
    }
}
