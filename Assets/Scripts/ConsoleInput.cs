using UnityEngine;

public class WoWCameraSettings : MonoBehaviour
{
    private CameraController cameraController;

    [Header("Console Command Simulation")]
    [SerializeField] private bool showConsole = false;
    private string consoleInput = "";

    void Start()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
    }

    void Update()
    {
        // Toggle console with tilde key
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            showConsole = !showConsole;
            consoleInput = "";
        }

        // Process console command
        if (showConsole && Input.GetKeyDown(KeyCode.Return))
        {
            ProcessCommand(consoleInput);
            consoleInput = "";
            showConsole = false;
        }
    }

    void ProcessCommand(string input)
    {
        string[] parts = input.Split(' ');
        if (parts.Length < 2) return;

        string command = parts[0].ToLower();
        string value = parts[1];

        switch (command)
        {
            case "/console":
                if (parts.Length >= 3)
                {
                    cameraController.ConsoleCommand(parts[1], parts[2]);
                    Debug.Log($"Console: {parts[1]} set to {parts[2]}");
                }
                break;

            case "/fov":
                if (float.TryParse(value, out float fov))
                {
                    cameraController.SetFOV(fov);
                    Debug.Log($"FOV set to {fov}");
                }
                break;
        }
    }

    void OnGUI()
    {
        if (showConsole)
        {
            // Draw console background
            GUI.Box(new Rect(10, Screen.height - 40, 400, 30), "");

            // Console input
            GUI.SetNextControlName("ConsoleInput");
            consoleInput = GUI.TextField(new Rect(15, Screen.height - 35, 390, 20), consoleInput);
            GUI.FocusControl("ConsoleInput");
        }

        // Help text
        if (GUI.Button(new Rect(10, 10, 150, 25), "Camera Help (H)") || Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("=== WoW Camera Controls ===");
            Debug.Log("Right Mouse: Rotate camera");
            Debug.Log("Mouse Wheel: Zoom in/out");
            Debug.Log("Arrow Keys: Rotate camera");
            Debug.Log("Page Up/Down: Pitch camera");
            Debug.Log("Numpad +/-: Zoom");
            Debug.Log("` (Tilde): Open console");
            Debug.Log("");
            Debug.Log("Console Commands:");
            Debug.Log("/console cameraFOV [60-100]");
            Debug.Log("/console cameraSmoothStyle [1-10]");
        }
    }
}