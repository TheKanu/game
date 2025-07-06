using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Mirror;
using System.Linq;

public class ChatManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private ScrollRect messageScrollView;
    [SerializeField] private Transform messageContent;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    [Header("Channel Tabs")]
    [SerializeField] private Button worldTab;
    [SerializeField] private Button localTab;
    [SerializeField] private Button privateTab;
    [SerializeField] private Button systemTab;

    [Header("Message Prefab")]
    [SerializeField] private GameObject messagePrefab;

    [Header("Chat Settings")]
    [SerializeField] private int maxMessages = 100;
    [SerializeField] private float localChatRange = 50f;
    [SerializeField] private bool showTimestamps = true;
    [SerializeField] private bool enableChatFading = true;
    [SerializeField] private float fadeAfterSeconds = 5f;

    [Header("Anti-Spam")]
    [SerializeField] private float spamCooldown = 1f;
    [SerializeField] private int maxMessagesPerMinute = 30;
    [SerializeField] private string[] bannedWords = { "spam", "hack", "cheat" };

    // Chat state
    private ChatChannel currentChannel = ChatChannel.World;
    private Dictionary<ChatChannel, List<GameObject>> channelMessages = new Dictionary<ChatChannel, List<GameObject>>();
    private Dictionary<ChatChannel, List<ChatMessage>> messageHistory = new Dictionary<ChatChannel, List<ChatMessage>>();
    private bool chatOpen = false;
    private bool waitingForMessageSend = false; // Diese Zeile hinzufügen
    private string lastPrivateTarget = "";


    // Anti-spam
    private float lastMessageTime = 0f;
    private Queue<float> messageTimes = new Queue<float>();

    // Chat fading
    private CanvasGroup chatCanvasGroup;
    private float lastActivityTime;

    public static ChatManager Instance { get; private set; }

    public enum ChatChannel
    {
        World = 0,
        Local = 1,
        Private = 2,
        System = 3,
        Guild = 4
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string sender;
        public string message;
        public ChatChannel channel;
        public float timestamp;
        public Vector3 senderPosition;
        public Color messageColor;

        public ChatMessage(string sender, string message, ChatChannel channel, Vector3 position = default, Color color = default)
        {
            this.sender = sender;
            this.message = message;
            this.channel = channel;
            this.timestamp = Time.time;
            this.senderPosition = position;
            this.messageColor = color == default ? Color.white : color;
        }
    }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // UI-Validierung vor Initialisierung
        if (!ValidateUIReferences())
        {
            Debug.LogError("ChatManager: UI-Setup fehlgeschlagen! Deaktiviere Chat-Funktionalität.");
            enabled = false; // Deaktiviert Update() Aufrufe
            return;
        }

        InitializeChat();
        SetupEventListeners();
        SetupChatFading();
        ToggleChat(false);
    }

    bool ValidateUIReferences()
    {
        bool isValid = true;

        if (chatPanel == null)
        {
            Debug.LogError("ChatManager: chatPanel ist nicht zugewiesen!");
            isValid = false;
        }

        if (inputField == null)
        {
            Debug.LogError("ChatManager: inputField ist nicht zugewiesen!");
            isValid = false;
        }

        if (messageContent == null)
        {
            Debug.LogError("ChatManager: messageContent ist nicht zugewiesen!");
            isValid = false;
        }

        if (messageScrollView == null)
        {
            Debug.LogError("ChatManager: messageScrollView ist nicht zugewiesen!");
            isValid = false;
        }

        if (messagePrefab == null)
        {
            Debug.LogError("ChatManager: messagePrefab ist nicht zugewiesen!");
            isValid = false;
        }

        return isValid;
    }


    void Update()
    {
        HandleInput();
        HandleChatFading();
    }

    void InitializeChat()
    {
        // Initialize message storage for each channel
        foreach (ChatChannel channel in System.Enum.GetValues(typeof(ChatChannel)))
        {
            channelMessages[channel] = new List<GameObject>();
            messageHistory[channel] = new List<ChatMessage>();
        }

        // Welcome message
        AddSystemMessage("Willkommen im Chat! Verwende /help für Befehle.");

        // Set initial channel
        SwitchChannel(ChatChannel.World);
    }

    void SetupEventListeners()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(SendMessage);

        if (inputField != null)
            inputField.onEndEdit.AddListener(OnInputEndEdit);

        if (worldTab != null)
            worldTab.onClick.AddListener(() => SwitchChannel(ChatChannel.World));

        if (localTab != null)
            localTab.onClick.AddListener(() => SwitchChannel(ChatChannel.Local));

        if (privateTab != null)
            privateTab.onClick.AddListener(() => SwitchChannel(ChatChannel.Private));

        if (systemTab != null)
            systemTab.onClick.AddListener(() => SwitchChannel(ChatChannel.System));
    }

    void OnInputEndEdit(string text)
    {
        // Diese Methode wird aufgerufen, wenn der Benutzer die Eingabe beendet
        // Wir behandeln Enter-Eingaben jetzt in HandleInput(), daher bleibt diese leer
        // oder kann für andere Zwecke verwendet werden
    }


    void SetupChatFading()
    {
        chatCanvasGroup = chatPanel.GetComponent<CanvasGroup>();
        if (chatCanvasGroup == null)
        {
            chatCanvasGroup = chatPanel.AddComponent<CanvasGroup>();
        }
        lastActivityTime = Time.time;
    }

    void HandleInput()
    {
        // Null-Check für kritische UI-Elemente
        if (inputField == null || chatPanel == null)
        {
            Debug.LogWarning("ChatManager: UI-Referenzen sind null! Überprüfe Inspector-Zuweisungen.");
            return;
        }

        // Handle Enter key press
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!chatOpen)
            {
                // First Enter press - open chat
                OpenChat();
            }
            else if (waitingForMessageSend)
            {
                // Second Enter press - send message if there's text, otherwise close chat
                if (!string.IsNullOrEmpty(inputField.text.Trim()))
                {
                    SendMessage();
                    waitingForMessageSend = false; // Reset flag after sending
                }
                else
                {
                    CloseChat();
                }
            }
        }

        // Close chat with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && chatOpen)
        {
            CloseChat();
        }

        // Quick channel switching
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchChannel(ChatChannel.World);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchChannel(ChatChannel.Local);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchChannel(ChatChannel.Private);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchChannel(ChatChannel.System);
    }

    void HandleChatFading()
    {
        if (!enableChatFading || chatOpen) return;

        if (Time.time - lastActivityTime > fadeAfterSeconds)
        {
            float alpha = Mathf.Lerp(chatCanvasGroup.alpha, 0.3f, Time.deltaTime * 2f);
            chatCanvasGroup.alpha = alpha;
        }
    }

    public void OpenChat()
    {
        if (inputField == null || chatPanel == null || chatCanvasGroup == null)
        {
            Debug.LogError("ChatManager: Kann Chat nicht öffnen - UI-Referenzen fehlen!");
            return;
        }

        chatOpen = true;
        waitingForMessageSend = true; // Set flag when opening chat (if using advanced solution)
        ToggleChat(true);
        inputField.Select();
        inputField.ActivateInputField();

        // Reset fading
        chatCanvasGroup.alpha = 1f;
        lastActivityTime = Time.time;
    }



    public void CloseChat()
    {
        chatOpen = false;
        waitingForMessageSend = false; // Reset flag when closing chat (if using advanced solution)
        ToggleChat(false);

        if (inputField != null)
        {
            inputField.text = "";
            inputField.DeactivateInputField();
        }
    }



    void ToggleChat(bool show)
    {
        if (chatPanel != null)
            chatPanel.SetActive(show);

        // Cursor management
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }



    public void SendMessage()
    {
        if (inputField == null)
        {
            Debug.LogError("ChatManager: InputField ist null - kann keine Nachricht senden!");
            return;
        }

        string message = inputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // Anti-spam check
        if (!CanSendMessage())
        {
            AddSystemMessage("Du sendest Nachrichten zu schnell! Warte einen Moment.");
            return;
        }

        // Filter banned words
        if (ContainsBannedWords(message))
        {
            AddSystemMessage("Deine Nachricht enthält verbotene Wörter.");
            return;
        }

        // Handle commands
        if (message.StartsWith("/"))
        {
            ProcessCommand(message);
        }
        else
        {
            // Send regular message
            string playerName = GetPlayerName();
            Vector3 playerPos = GetPlayerPosition();

            // Network call to send message
            CmdSendChatMessage(playerName, message, (int)currentChannel, playerPos);
        }

        // Clear input and track spam
        inputField.text = "";
        inputField.ActivateInputField();
        lastMessageTime = Time.time;
        messageTimes.Enqueue(Time.time);
    }

    [Command(requiresAuthority = false)]
    void CmdSendChatMessage(string sender, string message, int channelInt, Vector3 senderPos)
    {
        ChatChannel channel = (ChatChannel)channelInt;

        // Server-side validation
        if (string.IsNullOrEmpty(message) || message.Length > 200)
            return;

        // Broadcast to appropriate clients
        switch (channel)
        {
            case ChatChannel.World:
                RpcReceiveWorldMessage(sender, message);
                break;
            case ChatChannel.Local:
                RpcReceiveLocalMessage(sender, message, senderPos);
                break;
            case ChatChannel.Private:
                // Handle private messages separately
                break;
        }
    }

    [ClientRpc]
    void RpcReceiveWorldMessage(string sender, string message)
    {
        AddMessage(sender, message, ChatChannel.World);
    }

    [ClientRpc]
    void RpcReceiveLocalMessage(string sender, string message, Vector3 senderPos)
    {
        // Check if player is in range for local chat
        float distance = Vector3.Distance(GetPlayerPosition(), senderPos);
        if (distance <= localChatRange)
        {
            AddMessage(sender, message, ChatChannel.Local);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdSendPrivateMessage(string sender, string recipient, string message)
    {
        // Find recipient and send private message
        RpcReceivePrivateMessage(sender, recipient, message);
    }

    [ClientRpc]
    void RpcReceivePrivateMessage(string sender, string recipient, string message)
    {
        string playerName = GetPlayerName();
        if (recipient == playerName)
        {
            AddMessage($"{sender} -> You", message, ChatChannel.Private);
            lastPrivateTarget = sender;
        }
        else if (sender == playerName)
        {
            AddMessage($"You -> {recipient}", message, ChatChannel.Private);
        }
    }

    void ProcessCommand(string command)
    {
        string[] parts = command.Split(' ');
        string cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "/help":
                ShowHelp();
                break;

            case "/w":
            case "/whisper":
            case "/tell":
                if (parts.Length >= 3)
                {
                    string target = parts[1];
                    string message = string.Join(" ", parts.Skip(2));
                    SendPrivateMessage(target, message);
                }
                else
                {
                    AddSystemMessage("Verwendung: /w <Spielername> <Nachricht>");
                }
                break;

            case "/r":
            case "/reply":
                if (parts.Length >= 2 && !string.IsNullOrEmpty(lastPrivateTarget))
                {
                    string message = string.Join(" ", parts.Skip(1));
                    SendPrivateMessage(lastPrivateTarget, message);
                }
                else
                {
                    AddSystemMessage("Niemand zum Antworten vorhanden.");
                }
                break;

            case "/clear":
                ClearCurrentChannel();
                break;

            case "/time":
                AddSystemMessage($"Aktuelle Zeit: {System.DateTime.Now:HH:mm:ss}");
                break;

            case "/players":
                ShowOnlinePlayers();
                break;

            case "/ping":
                AddSystemMessage($"Ping: {Mathf.RoundToInt((float)(NetworkTime.rtt * 1000))}ms");
                break;

            default:
                AddSystemMessage($"Unbekannter Befehl: {cmd}. Verwende /help für Hilfe.");
                break;
        }
    }

    void ShowHelp()
    {
        AddSystemMessage("=== Chat-Befehle ===");
        AddSystemMessage("/w <Name> <Nachricht> - Private Nachricht senden");
        AddSystemMessage("/r <Nachricht> - Auf private Nachricht antworten");
        AddSystemMessage("/clear - Aktuellen Chat leeren");
        AddSystemMessage("/time - Aktuelle Zeit anzeigen");
        AddSystemMessage("/players - Online-Spieler anzeigen");
        AddSystemMessage("/ping - Ping anzeigen");
        AddSystemMessage("1-4 - Schneller Kanalwechsel");
        AddSystemMessage("Enter - Chat öffnen/schließen");
        AddSystemMessage("Escape - Chat schließen");
    }

    void SendPrivateMessage(string target, string message)
    {
        string playerName = GetPlayerName();
        CmdSendPrivateMessage(playerName, target, message);
        lastPrivateTarget = target;
    }

    void ShowOnlinePlayers()
    {
        // This would need to be implemented with a proper player list
        AddSystemMessage("Online-Spieler: " + NetworkManager.singleton.numPlayers);
    }

    bool CanSendMessage()
    {
        // Check spam cooldown
        if (Time.time - lastMessageTime < spamCooldown)
            return false;

        // Check messages per minute
        while (messageTimes.Count > 0 && Time.time - messageTimes.Peek() > 60f)
        {
            messageTimes.Dequeue();
        }

        return messageTimes.Count < maxMessagesPerMinute;
    }

    bool ContainsBannedWords(string message)
    {
        string lowerMessage = message.ToLower();
        return bannedWords.Any(word => lowerMessage.Contains(word.ToLower()));
    }

    public void AddMessage(string sender, string message, ChatChannel channel)
    {
        ChatMessage chatMessage = new ChatMessage(sender, message, channel, GetPlayerPosition(), GetChannelColor(channel));

        // Add to history
        messageHistory[channel].Add(chatMessage);

        // Create UI element
        GameObject messageObj = CreateMessageUI(chatMessage);
        channelMessages[channel].Add(messageObj);

        // Show/hide based on current channel
        messageObj.SetActive(channel == currentChannel);

        // Limit message count
        LimitMessages(channel);

        // Auto-scroll and update activity
        if (channel == currentChannel)
        {
            ScrollToBottom();
            lastActivityTime = Time.time;
            chatCanvasGroup.alpha = 1f;
        }
    }

    GameObject CreateMessageUI(ChatMessage message)
    {
        if (messagePrefab == null || messageContent == null) return null;

        GameObject messageObj = Instantiate(messagePrefab, messageContent);
        TextMeshProUGUI textComponent = messageObj.GetComponent<TextMeshProUGUI>();

        if (textComponent != null)
        {
            string formattedMessage = FormatMessage(message);
            textComponent.text = formattedMessage;
            textComponent.color = message.messageColor;
        }

        return messageObj;
    }

    string FormatMessage(ChatMessage message)
    {
        string timestamp = showTimestamps ? $"[{System.DateTime.Now:HH:mm}] " : "";

        switch (message.channel)
        {
            case ChatChannel.World:
                return $"{timestamp}[World] {message.sender}: {message.message}";
            case ChatChannel.Local:
                return $"{timestamp}[Local] {message.sender}: {message.message}";
            case ChatChannel.Private:
                return $"{timestamp}[Private] {message.message}";
            case ChatChannel.System:
                return $"{timestamp}[System] {message.message}";
            case ChatChannel.Guild:
                return $"{timestamp}[Guild] {message.sender}: {message.message}";
            default:
                return $"{timestamp}{message.sender}: {message.message}";
        }
    }

    Color GetChannelColor(ChatChannel channel)
    {
        switch (channel)
        {
            case ChatChannel.World:
                return Color.white;
            case ChatChannel.Local:
                return Color.yellow;
            case ChatChannel.Private:
                return Color.magenta;
            case ChatChannel.System:
                return Color.cyan;
            case ChatChannel.Guild:
                return Color.green;
            default:
                return Color.white;
        }
    }

    void LimitMessages(ChatChannel channel)
    {
        var messages = channelMessages[channel];
        var history = messageHistory[channel];

        while (messages.Count > maxMessages)
        {
            GameObject oldMessage = messages[0];
            messages.RemoveAt(0);
            history.RemoveAt(0);

            if (oldMessage != null)
                Destroy(oldMessage);
        }
    }

    public void SwitchChannel(ChatChannel channel)
    {
        // Hide current channel messages
        foreach (var message in channelMessages[currentChannel])
        {
            if (message != null)
                message.SetActive(false);
        }

        // Show new channel messages
        currentChannel = channel;
        foreach (var message in channelMessages[currentChannel])
        {
            if (message != null)
                message.SetActive(true);
        }

        // Update UI
        UpdateChannelTabs();
        ScrollToBottom();

        // Update activity
        lastActivityTime = Time.time;
        chatCanvasGroup.alpha = 1f;
    }

    void UpdateChannelTabs()
    {
        // Reset all tab colors
        SetTabColor(worldTab, currentChannel == ChatChannel.World);
        SetTabColor(localTab, currentChannel == ChatChannel.Local);
        SetTabColor(privateTab, currentChannel == ChatChannel.Private);
        SetTabColor(systemTab, currentChannel == ChatChannel.System);
    }

    void SetTabColor(Button tab, bool isActive)
    {
        if (tab != null)
        {
            Image tabImage = tab.GetComponent<Image>();
            if (tabImage != null)
            {
                tabImage.color = isActive ? Color.white : Color.gray;
            }
        }
    }

    void ScrollToBottom()
    {
        if (messageScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            messageScrollView.verticalNormalizedPosition = 0f;
        }
    }

    void ClearCurrentChannel()
    {
        foreach (var message in channelMessages[currentChannel])
        {
            if (message != null)
                Destroy(message);
        }
        channelMessages[currentChannel].Clear();
        messageHistory[currentChannel].Clear();

        AddSystemMessage($"{currentChannel} Chat wurde geleert.");
    }

    public void AddSystemMessage(string message)
    {
        AddMessage("System", message, ChatChannel.System);
    }

    // Helper methods - customize based on your game
    string GetPlayerName()
    {
        if (NetworkClient.localPlayer != null)
        {
            // Try to get player name from NetworkIdentity or PlayerController
            return NetworkClient.localPlayer.name;
        }
        return $"Player_{System.Environment.MachineName}";
    }

    Vector3 GetPlayerPosition()
    {
        if (NetworkClient.localPlayer != null)
        {
            return NetworkClient.localPlayer.transform.position;
        }
        return Vector3.zero;
    }

    // Public methods for external access
    public void SetChatVisibility(bool visible)
    {
        if (chatPanel != null)
            chatPanel.SetActive(visible);
    }

    public void AddCustomMessage(string sender, string message, ChatChannel channel, Color color)
    {
        ChatMessage chatMessage = new ChatMessage(sender, message, channel, Vector3.zero, color);
        AddMessage(sender, message, channel);
    }

    public List<ChatMessage> GetChannelHistory(ChatChannel channel)
    {
        return messageHistory.ContainsKey(channel) ? messageHistory[channel] : new List<ChatMessage>();
    }

    public void LoadChatHistory(ChatChannel channel, List<ChatMessage> history)
    {
        if (messageHistory.ContainsKey(channel))
        {
            messageHistory[channel].Clear();
            messageHistory[channel].AddRange(history);

            // Recreate UI for current channel
            if (channel == currentChannel)
            {
                RefreshChannelUI(channel);
            }
        }
    }

    void RefreshChannelUI(ChatChannel channel)
    {
        // Clear existing UI
        foreach (var message in channelMessages[channel])
        {
            if (message != null)
                Destroy(message);
        }
        channelMessages[channel].Clear();

        // Recreate UI from history
        foreach (var message in messageHistory[channel])
        {
            GameObject messageObj = CreateMessageUI(message);
            channelMessages[channel].Add(messageObj);
            messageObj.SetActive(true);
        }

        ScrollToBottom();
    }
}
