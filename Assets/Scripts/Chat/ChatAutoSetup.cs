#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class ChatAutoSetup : MonoBehaviour
{
    [MenuItem("WoW Tools/Setup Chat System")]
    public static void SetupChatSystem()
    {
        Debug.Log("=== Setting up Chat System ===");

        // 1. Canvas finden oder erstellen
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("✓ Created Canvas");
        }

        // 2. Chat Panel erstellen
        GameObject chatPanel = CreateChatPanel(canvas.transform);

        // 3. Message Prefab erstellen
        GameObject messagePrefab = CreateMessagePrefab();

        // 4. Chat Manager einrichten
        GameObject chatSystemObj = GameObject.Find("ChatSystem");
        if (chatSystemObj == null)
        {
            chatSystemObj = new GameObject("ChatSystem");
        }

        SimpleChatManager chatManager = chatSystemObj.GetComponent<SimpleChatManager>();
        if (chatManager == null)
        {
            chatManager = chatSystemObj.AddComponent<SimpleChatManager>();
        }

        // 5. UI Referenzen zuweisen
        AssignReferences(chatManager, chatPanel, messagePrefab);

        // 6. Prefab speichern
        string prefabPath = "Assets/Prefabs/ChatMessagePrefab.prefab";

        // Erstelle Prefabs Ordner falls nicht vorhanden
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(messagePrefab, prefabPath);
        DestroyImmediate(messagePrefab);

        // Lade das gespeicherte Prefab und weise es zu
        GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        // Nochmal zuweisen mit dem gespeicherten Prefab
        SerializedObject serializedManager = new SerializedObject(chatManager);
        serializedManager.FindProperty("messagePrefab").objectReferenceValue = savedPrefab;
        serializedManager.ApplyModifiedProperties();

        Selection.activeGameObject = chatSystemObj;

        Debug.Log("=== Chat System Setup Complete! ===");
        Debug.Log("Drücke Enter um den Chat zu öffnen");
    }

    static GameObject CreateChatPanel(Transform parent)
    {
        GameObject chatPanel = new GameObject("ChatPanel");
        chatPanel.transform.SetParent(parent, false);

        // RectTransform
        RectTransform rect = chatPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(220, 150);
        rect.sizeDelta = new Vector2(400, 300);

        // Background
        Image bg = chatPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        // Canvas Group für Fading
        chatPanel.AddComponent<CanvasGroup>();

        // Message Area
        GameObject scrollView = CreateMessageArea(chatPanel.transform);

        // Input Area
        CreateInputArea(chatPanel.transform);

        return chatPanel;
    }

    static GameObject CreateMessageArea(Transform parent)
    {
        GameObject scrollView = new GameObject("MessageScrollView");
        scrollView.transform.SetParent(parent, false);

        RectTransform rect = scrollView.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(5, 40);
        rect.offsetMax = new Vector2(-5, -5);

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);

        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Mask mask = viewport.AddComponent<Mask>();
        Image maskImage = viewport.AddComponent<Image>();
        maskImage.color = Color.white;
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("MessageContent");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2;
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect Setup
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        return scrollView;
    }

    static void CreateInputArea(Transform parent)
    {
        GameObject inputPanel = new GameObject("InputPanel");
        inputPanel.transform.SetParent(parent, false);

        RectTransform rect = inputPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(0, 20);
        rect.sizeDelta = new Vector2(-10, 30);

        // Input Field
        GameObject inputFieldObj = new GameObject("InputField");
        inputFieldObj.transform.SetParent(inputPanel.transform, false);

        RectTransform inputRect = inputFieldObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0);
        inputRect.anchorMax = new Vector2(1, 1);
        inputRect.offsetMin = new Vector2(0, 0);
        inputRect.offsetMax = new Vector2(-70, 0);

        Image inputBg = inputFieldObj.AddComponent<Image>();
        inputBg.color = Color.white;

        TMP_InputField tmpInput = inputFieldObj.AddComponent<TMP_InputField>();
        tmpInput.characterLimit = 200;

        // Text Component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputFieldObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 0);
        textRect.offsetMax = new Vector2(-5, 0);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        tmpInput.textComponent = text;

        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputFieldObj.transform, false);

        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(5, 0);
        placeholderRect.offsetMax = new Vector2(-5, 0);

        TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Nachricht eingeben...";
        placeholder.fontSize = 14;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;

        tmpInput.placeholder = placeholder;

        // Send Button
        GameObject sendButton = new GameObject("SendButton");
        sendButton.transform.SetParent(inputPanel.transform, false);

        RectTransform buttonRect = sendButton.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 0);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.anchoredPosition = new Vector2(-35, 0);
        buttonRect.sizeDelta = new Vector2(60, 0);

        Button button = sendButton.AddComponent<Button>();
        Image buttonImage = sendButton.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f);

        // Button Text
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(sendButton.transform, false);

        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Senden";
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
    }

    static GameObject CreateMessagePrefab()
    {
        GameObject prefab = new GameObject("ChatMessagePrefab");

        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(0, 20);

        TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        ContentSizeFitter fitter = prefab.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement element = prefab.AddComponent<LayoutElement>();
        element.flexibleWidth = 1;

        return prefab;
    }

    static void AssignReferences(SimpleChatManager manager, GameObject chatPanel, GameObject messagePrefab)
    {
        SerializedObject serializedManager = new SerializedObject(manager);

        serializedManager.FindProperty("chatPanel").objectReferenceValue = chatPanel;
        serializedManager.FindProperty("messageScrollView").objectReferenceValue =
            chatPanel.transform.Find("MessageScrollView")?.GetComponent<ScrollRect>();
        serializedManager.FindProperty("messageContent").objectReferenceValue =
            chatPanel.transform.Find("MessageScrollView/Viewport/MessageContent");
        serializedManager.FindProperty("inputField").objectReferenceValue =
            chatPanel.transform.Find("InputPanel/InputField")?.GetComponent<TMP_InputField>();
        serializedManager.FindProperty("sendButton").objectReferenceValue =
            chatPanel.transform.Find("InputPanel/SendButton")?.GetComponent<Button>();

        serializedManager.ApplyModifiedProperties();
    }
}
#endif