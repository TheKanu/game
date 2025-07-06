using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDefaultBuilder : MonoBehaviour
{
    public GameObject messagePrefab; // Prefab für Chat-Nachrichten
    private GameObject canvas, chatPanel, messageScrollView, messageContent, inputFieldObj, sendButtonObj;
    private Button worldTab, localTab, privateTab;

    void Start()
    {
        // Canvas erstellen
        canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = canvas.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // ChatPanel
        chatPanel = new GameObject("ChatPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        chatPanel.transform.SetParent(canvas.transform);
        var rt = chatPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(20, 20);
        rt.sizeDelta = new Vector2(400, 300);
        var img = chatPanel.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f);

        // ScrollView
        messageScrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        messageScrollView.transform.SetParent(chatPanel.transform);
        rt = messageScrollView.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.2f);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(5, 40);
        rt.offsetMax = new Vector2(-5, -5);
        var scrollRect = messageScrollView.GetComponent<ScrollRect>();
        var scrollImage = messageScrollView.GetComponent<Image>();
        scrollImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        scrollRect.vertical = true;

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(messageScrollView.transform);
        var rtViewport = viewport.GetComponent<RectTransform>();
        rtViewport.anchorMin = Vector2.zero;
        rtViewport.anchorMax = Vector2.one;
        rtViewport.offsetMin = Vector2.zero;
        rtViewport.offsetMax = Vector2.zero;
        var mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;
        var imgViewport = viewport.GetComponent<Image>();
        imgViewport.color = Color.white;

        // Content
        messageContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        messageContent.transform.SetParent(viewport.transform);
        var rtContent = messageContent.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0, 1);
        rtContent.anchorMax = new Vector2(1, 1);
        rtContent.pivot = new Vector2(0.5f, 1);
        rtContent.anchoredPosition = Vector2.zero;
        rtContent.sizeDelta = new Vector2(0, 0);
        var vlg = messageContent.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 2;
        var csf = messageContent.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Assign content to ScrollRect
        scrollRect.content = rtContent;

        // Input Panel
        var inputPanel = new GameObject("InputPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        inputPanel.transform.SetParent(chatPanel.transform);
        var rtInput = inputPanel.GetComponent<RectTransform>();
        rtInput.anchorMin = new Vector2(0, 0);
        rtInput.anchorMax = new Vector2(1, 0);
        rtInput.pivot = new Vector2(0.5f, 0);
        rtInput.anchoredPosition = new Vector2(0, 5);
        rtInput.sizeDelta = new Vector2(0, 30);
        var hlg = inputPanel.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(5, 5, 2, 2);
        hlg.spacing = 5;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // InputField
        inputFieldObj = new GameObject("InputField", typeof(RectTransform), typeof(TMP_InputField), typeof(Image));
        inputFieldObj.transform.SetParent(inputPanel.transform);
        var rtInputField = inputFieldObj.GetComponent<RectTransform>();
        rtInputField.sizeDelta = new Vector2(300, 25);
        var inputImage = inputFieldObj.GetComponent<Image>();
        inputImage.color = Color.white;
        var tmpInput = inputFieldObj.GetComponent<TMP_InputField>();
        tmpInput.placeholder = CreatePlaceholder(inputFieldObj.transform);
        tmpInput.textComponent = CreateText(inputFieldObj.transform);

        // Send Button
        sendButtonObj = new GameObject("SendButton", typeof(RectTransform), typeof(Button), typeof(Image));
        sendButtonObj.transform.SetParent(inputPanel.transform);
        var rtButton = sendButtonObj.GetComponent<RectTransform>();
        rtButton.sizeDelta = new Vector2(60, 25);
        var btnImage = sendButtonObj.GetComponent<Image>();
        btnImage.color = new Color(0.2f, 0.6f, 1f);
        var btn = sendButtonObj.GetComponent<Button>();
        var btnText = CreateButtonText(sendButtonObj.transform, "Send");

        // Assign button action
        btn.onClick.AddListener(() => { /* Verbindung zum ChatManager hier herstellen */ });
    }

    TextMeshProUGUI CreateText(Transform parent)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize = 14;
        txt.color = Color.black;
        return txt;
    }

    TextMeshProUGUI CreateButtonText(Transform parent, string text)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 14;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        return txt;
    }

    TMP_Text CreatePlaceholder(Transform parent)
    {
        var go = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = "Nachricht eingeben...";
        txt.fontSize = 14;
        txt.color = new Color(0.5f, 0.5f, 0.5f);
        txt.alignment = TextAlignmentOptions.Left;
        return txt;
    }
}
