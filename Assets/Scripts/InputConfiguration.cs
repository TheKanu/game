using UnityEngine;

[CreateAssetMenu(fileName = "InputConfiguration", menuName = "WoW/Input Configuration")]
public class InputConfiguration : ScriptableObject
{
    [Header("Movement Keys")]
    public KeyCode moveForward = KeyCode.W;
    public KeyCode moveBackward = KeyCode.S;
    public KeyCode strafeLeft = KeyCode.A;
    public KeyCode strafeRight = KeyCode.D;
    public KeyCode jump = KeyCode.Space;
    public KeyCode autorun = KeyCode.Numlock;

    [Header("Camera Keys")]
    public KeyCode cameraRotateModifier = KeyCode.Mouse1; // Right mouse
    public KeyCode cameraOnlyModifier = KeyCode.Mouse0;   // Left mouse

    [Header("Mouse Settings")]
    [Range(0.1f, 10f)]
    public float mouseSensitivityX = 2f;
    [Range(0.1f, 10f)]
    public float mouseSensitivityY = 2f;
    public bool invertMouseY = true;

    [Header("Movement Settings")]
    [Range(1f, 20f)]
    public float moveSpeed = 7f;
    [Range(1f, 20f)]
    public float backwardSpeedMultiplier = 0.65f;
    [Range(1f, 20f)]
    public float strafeSpeedMultiplier = 0.93f;

    [Header("Camera Settings")]
    [Range(1f, 30f)]
    public float cameraDistance = 5f;
    [Range(0f, 5f)]
    public float cameraHeight = 2f;
    [Range(-2f, 2f)]
    public float cameraShoulderOffset = 0.5f;

    // Singleton pattern for easy access
    private static InputConfiguration instance;
    public static InputConfiguration Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<InputConfiguration>("InputConfiguration");
                if (instance == null)
                {
                    Debug.LogWarning("InputConfiguration not found in Resources folder. Creating default.");
                    instance = CreateInstance<InputConfiguration>();
                }
            }
            return instance;
        }
    }
}