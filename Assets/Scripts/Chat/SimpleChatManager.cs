using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class SimpleChatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private ScrollRect messageScrollView;
    [SerializeField] private Transform messageContent;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    [Header("Message Prefab")]
    [SerializeField] private GameObject messagePrefab;

    [Header("Chat Settings")]
    [SerializeField] private int maxMessages = 50;
    [SerializeField] private bool showTimestamps = true;
    [SerializeField] private float fadeAfterSeconds = 5f;
    [SerializeField] private float fadedAlpha = 0.3f;

    // Chat state
    private List<GameObject> messages = new List<GameObject>();
    private bool chatOpen = false;
    private CanvasGroup chatCanvasGroup;
    private float lastActivityTime;

    // Singleton
    public static SimpleChatManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetupChat();

        // Verstecke Chat initial
        if (chatPanel != null)
            chatPanel.SetActive(false);

        // Willkommensnachricht
        AddMessage("System", "Willkommen! Drücke Enter um zu chatten.");
    }

    void Update()
    {
        HandleChatInput();
        HandleChatFading();
    }

    void SetupChat()
    {
        // Canvas Group für Fading
        if (chatPanel != null)
        {
            chatCanvasGroup = chatPanel.GetComponent<CanvasGroup>();
            if (chatCanvasGroup == null)
            {
                chatCanvasGroup = chatPanel.AddComponent<CanvasGroup>();
            }
        }

        // Button Listener
        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(SendChatMessage);
        }

        // Input Field Enter-Event
        if (inputField != null)
        {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    SendChatMessage();
                }
            });
        }

        lastActivityTime = Time.time;
    }

    void HandleChatInput()
    {
        // Enter drücken
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!chatOpen)
            {
                // Chat öffnen
                OpenChat();
            }
            else
            {
                // Nachricht senden wenn Text vorhanden, sonst Chat schließen
                if (!string.IsNullOrEmpty(inputField.text.Trim()))
                {
                    SendChatMessage();
                }
                else
                {
                    CloseChat();
                }
            }
        }

        // Escape zum Schließen
        if (Input.GetKeyDown(KeyCode.Escape) && chatOpen)
        {
            CloseChat();
        }
    }

    void OpenChat()
    {
        chatOpen = true;

        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
            chatCanvasGroup.alpha = 1f;
        }

        if (inputField != null)
        {
            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();
        }

        // Zeige Cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        lastActivityTime = Time.time;
    }

    void CloseChat()
    {
        chatOpen = false;

        if (inputField != null)
        {
            inputField.text = "";
            inputField.DeactivateInputField();
        }

        // Verstecke Cursor wieder für Spiel
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SendChatMessage()
    {
        if (inputField == null) return;

        string message = inputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // Kommando-Verarbeitung
        if (message.StartsWith("/"))
        {
            ProcessCommand(message);
        }
        else
        {
            // Normale Nachricht
            AddMessage("Spieler", message);
        }

        // Input leeren aber Chat offen lassen
        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();

        lastActivityTime = Time.time;
    }

    void ProcessCommand(string command)
    {
        string[] parts = command.Split(' ');
        string cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "/help":
                AddMessage("System", "=== Befehle ===");
                AddMessage("System", "/help - Diese Hilfe");
                AddMessage("System", "/clear - Chat leeren");
                AddMessage("System", "/time - Zeit anzeigen");
                AddMessage("System", "/quit - Spiel beenden");
                break;

            case "/clear":
                ClearChat();
                AddMessage("System", "Chat wurde geleert.");
                break;

            case "/time":
                AddMessage("System", $"Aktuelle Zeit: {System.DateTime.Now:HH:mm:ss}");
                break;

            case "/quit":
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                break;

            default:
                AddMessage("System", $"Unbekannter Befehl: {cmd}");
                break;
        }
    }

    public void AddMessage(string sender, string message)
    {
        if (messagePrefab == null || messageContent == null) return;

        // Erstelle neue Nachricht
        GameObject msgObj = Instantiate(messagePrefab, messageContent);

        // Text setzen
        TextMeshProUGUI textComp = msgObj.GetComponent<TextMeshProUGUI>();
        if (textComp != null)
        {
            string timestamp = showTimestamps ? $"[{System.DateTime.Now:HH:mm}] " : "";

            // Farbe basierend auf Sender
            string coloredSender = sender;
            if (sender == "System")
            {
                coloredSender = $"<color=yellow>{sender}</color>";
            }
            else if (sender == "Spieler")
            {
                coloredSender = $"<color=white>{sender}</color>";
            }

            textComp.text = $"{timestamp}{coloredSender}: {message}";
        }

        messages.Add(msgObj);

        // Limit Nachrichten
        while (messages.Count > maxMessages)
        {
            if (messages[0] != null)
                Destroy(messages[0]);
            messages.RemoveAt(0);
        }

        // Auto-scroll
        if (messageScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            messageScrollView.verticalNormalizedPosition = 0f;
        }

        // Chat sichtbar machen wenn neue Nachricht
        if (chatPanel != null && !chatPanel.activeSelf)
        {
            chatPanel.SetActive(true);
        }

        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 1f;
        }

        lastActivityTime = Time.time;
    }

    void ClearChat()
    {
        foreach (var msg in messages)
        {
            if (msg != null)
                Destroy(msg);
        }
        messages.Clear();
    }

    void HandleChatFading()
    {
        if (chatOpen || chatCanvasGroup == null || chatPanel == null) return;

        // Fade nur wenn Chat nicht offen ist
        if (Time.time - lastActivityTime > fadeAfterSeconds)
        {
            float targetAlpha = fadedAlpha;
            chatCanvasGroup.alpha = Mathf.Lerp(chatCanvasGroup.alpha, targetAlpha, Time.deltaTime * 2f);

            // Komplett ausblenden wenn fast transparent
            if (chatCanvasGroup.alpha < 0.35f)
            {
                chatPanel.SetActive(false);
            }
        }
    }

    // Öffentliche Methoden für andere Systeme
    public void SystemMessage(string message)
    {
        AddMessage("System", message);
    }

    public bool IsChatOpen()
    {
        return chatOpen;
    }
}