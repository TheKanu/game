using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class ChatUI : MonoBehaviour
{
    [Header("Auto-Setup")]
    public bool autoCreateUI = true;
    public bool useTextMeshPro = true;

    void Start()
    {
        if (autoCreateUI)
        {
            CreateChatUI();
        }
    }

    void CreateChatUI()
    {
        // Fixed FindObjectOfType calls
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("ChatCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject chatPanel = CreateChatPanel(canvas.transform);
        GameObject messagePrefab = CreateMessagePrefab();

        ChatManager chatManager = FindFirstObjectByType<ChatManager>();
        if (chatManager == null)
        {
            GameObject chatSystemObj = new GameObject("ChatSystem");
            chatManager = chatSystemObj.AddComponent<ChatManager>();
            // Only add NetworkIdentity if you're using Mirror networking
            // chatSystemObj.AddComponent<NetworkIdentity>();
        }

        AssignUIReferences(chatManager, chatPanel, messagePrefab);
    }


    GameObject CreateChatPanel(Transform parent)
    {
        GameObject chatPanel = new GameObject("ChatPanel");
        chatPanel.transform.SetParent(parent, false);

        // RectTransform setup
        RectTransform rect = chatPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(220, 150);
        rect.sizeDelta = new Vector2(400, 300);

        // Background image
        Image bg = chatPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        // Canvas group for fading
        chatPanel.AddComponent<CanvasGroup>();

        // Create child elements
        CreateChannelTabs(chatPanel.transform);
        CreateMessageArea(chatPanel.transform);
        CreateInputArea(chatPanel.transform);

        return chatPanel;
    }

    void CreateChannelTabs(Transform parent)
    {
        GameObject tabContainer = new GameObject("ChannelTabs");
        tabContainer.transform.SetParent(parent, false);

        RectTransform rect = tabContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(0, -15);
        rect.sizeDelta = new Vector2(0, 30);

        HorizontalLayoutGroup layout = tabContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(5, 5, 2, 2);
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        // Create tabs
        CreateTab(tabContainer.transform, "World", "WorldTab");
        CreateTab(tabContainer.transform, "Local", "LocalTab");
        CreateTab(tabContainer.transform, "Private", "PrivateTab");
        CreateTab(tabContainer.transform, "System", "SystemTab");
    }

    GameObject CreateTab(Transform parent, string text, string name)
    {
        GameObject tab = new GameObject(name);
        tab.transform.SetParent(parent, false);

        Button button = tab.AddComponent<Button>();
        Image image = tab.AddComponent<Image>();
        image.color = Color.gray;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(tab.transform, false);

        if (useTextMeshPro)
        {
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return tab;
    }

    void CreateMessageArea(Transform parent)
    {
        GameObject scrollView = new GameObject("MessageScrollView");
        scrollView.transform.SetParent(parent, false);

        RectTransform rect = scrollView.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(5, 40);
        rect.offsetMax = new Vector2(-5, -45);

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

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
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vertLayout = content.AddComponent<VerticalLayoutGroup>();
        vertLayout.spacing = 2;
        vertLayout.padding = new RectOffset(5, 5, 5, 5);
        vertLayout.childControlWidth = true;
        vertLayout.childControlHeight = true;
        vertLayout.childForceExpandWidth = true;

        ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Setup ScrollRect
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.verticalScrollbar = null;
    }

    void CreateInputArea(Transform parent)
    {
        GameObject inputPanel = new GameObject("InputPanel");
        inputPanel.transform.SetParent(parent, false);

        RectTransform rect = inputPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(0, 15);
        rect.sizeDelta = new Vector2(0, 30);

        HorizontalLayoutGroup layout = inputPanel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(5, 5, 2, 2);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        // Input Field
        GameObject inputField = new GameObject("InputField");
        inputField.transform.SetParent(inputPanel.transform, false);

        Image inputBg = inputField.AddComponent<Image>();
        inputBg.color = Color.white;

        if (useTextMeshPro)
        {
            TMP_InputField tmpInput = inputField.AddComponent<TMP_InputField>();
            tmpInput.characterLimit = 200;
            tmpInput.placeholder = CreatePlaceholderText(inputField.transform, "Nachricht eingeben...");
            tmpInput.textComponent = CreateInputText(inputField.transform);
        }
        else
        {
            InputField input = inputField.AddComponent<InputField>();
            input.characterLimit = 200;
            input.placeholder = CreatePlaceholderTextLegacy(inputField.transform, "Nachricht eingeben...");
            input.textComponent = CreateInputTextLegacy(inputField.transform);
        }

        LayoutElement inputLayout = inputField.AddComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1;

        // Send Button
        GameObject sendButton = new GameObject("SendButton");
        sendButton.transform.SetParent(inputPanel.transform, false);

        Button button = sendButton.AddComponent<Button>();
        Image buttonImage = sendButton.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        // Button text
        GameObject buttonText = new GameObject("Text");
        buttonText.transform.SetParent(sendButton.transform, false);

        if (useTextMeshPro)
        {
            TextMeshProUGUI textComp = buttonText.AddComponent<TextMeshProUGUI>();
            textComp.text = "Send";
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            Text textComp = buttonText.AddComponent<Text>();
            textComp.text = "Send";
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        RectTransform buttonTextRect = buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        LayoutElement buttonLayout = sendButton.AddComponent<LayoutElement>();
        buttonLayout.minWidth = 60;
    }

    TextMeshProUGUI CreateInputText(Transform parent)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 12;
        text.color = Color.black;

        // Fixed: Use textWrappingMode instead of enableWordWrapping
        text.textWrappingMode = TextWrappingModes.Normal;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(5f, 0f);  // Add 'f' suffix
        rect.offsetMax = new Vector2(-5f, 0f); // Add 'f' suffix

        return text;
    }


    TextMeshProUGUI CreatePlaceholderText(Transform parent, string placeholderText)
    {
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent, false);

        TextMeshProUGUI text = placeholderObj.AddComponent<TextMeshProUGUI>();
        text.text = placeholderText;
        text.fontSize = 12;
        text.color = Color.gray;
        text.fontStyle = FontStyles.Italic;

        RectTransform rect = placeholderObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(5, 0);
        rect.offsetMax = new Vector2(-5, 0);

        return text;
    }

    Text CreateInputTextLegacy(Transform parent)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        Text text = textObj.AddComponent<Text>();
        text.fontSize = 12;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(5, 0);
        rect.offsetMax = new Vector2(-5, 0);

        return text;
    }

    Text CreatePlaceholderTextLegacy(Transform parent, string placeholderText)
    {
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent, false);

        Text text = placeholderObj.AddComponent<Text>();
        text.text = placeholderText;
        text.fontSize = 12;
        text.color = Color.gray;
        text.fontStyle = FontStyle.Italic;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform rect = placeholderObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(5, 0);
        rect.offsetMax = new Vector2(-5, 0);

        return text;
    }

    GameObject CreateMessagePrefab()
    {
        GameObject prefab = new GameObject("MessagePrefab");

        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(0, 20);

        if (useTextMeshPro)
        {
            TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
        }
        else
        {
            Text text = prefab.AddComponent<Text>();
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        ContentSizeFitter sizeFitter = prefab.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement layoutElement = prefab.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1;

        return prefab;
    }

    void AssignUIReferences(ChatManager chatManager, GameObject chatPanel, GameObject messagePrefab)
    {
        // Use reflection to assign private fields
        var type = typeof(ChatManager);
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Assign chat panel
        var chatPanelField = type.GetField("chatPanel", bindingFlags);
        if (chatPanelField != null)
            chatPanelField.SetValue(chatManager, chatPanel);

        // Assign message prefab
        var messagePrefabField = type.GetField("messagePrefab", bindingFlags);
        if (messagePrefabField != null)
            messagePrefabField.SetValue(chatManager, messagePrefab);

        // Assign other UI elements
        AssignUIElement(chatManager, "messageScrollView", chatPanel.transform.Find("MessageScrollView")?.GetComponent<ScrollRect>());
        AssignUIElement(chatManager, "messageContent", chatPanel.transform.Find("MessageScrollView/Viewport/MessageContent"));
        AssignUIElement(chatManager, "inputField", chatPanel.transform.Find("InputPanel/InputField")?.GetComponent<TMP_InputField>());
        AssignUIElement(chatManager, "sendButton", chatPanel.transform.Find("InputPanel/SendButton")?.GetComponent<Button>());
        AssignUIElement(chatManager, "worldTab", chatPanel.transform.Find("ChannelTabs/WorldTab")?.GetComponent<Button>());
        AssignUIElement(chatManager, "localTab", chatPanel.transform.Find("ChannelTabs/LocalTab")?.GetComponent<Button>());
        AssignUIElement(chatManager, "privateTab", chatPanel.transform.Find("ChannelTabs/PrivateTab")?.GetComponent<Button>());
        AssignUIElement(chatManager, "systemTab", chatPanel.transform.Find("ChannelTabs/SystemTab")?.GetComponent<Button>());
    }

    void AssignUIElement(ChatManager chatManager, string fieldName, Object value)
    {
        var type = typeof(ChatManager);
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var field = type.GetField(fieldName, bindingFlags);
        if (field != null && value != null)
            field.SetValue(chatManager, value);
    }
}
