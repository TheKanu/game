using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class WoWMovementSetup : EditorWindow
{
    [MenuItem("WoW Tools/Setup Movement System")]
    public static void ShowWindow()
    {
        GetWindow<WoWMovementSetup>("WoW Movement Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("WoW Movement System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. Create Player with Movement", GUILayout.Height(30)))
        {
            CreatePlayer();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("2. Setup Camera System", GUILayout.Height(30)))
        {
            SetupCamera();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("3. Create Test Environment", GUILayout.Height(30)))
        {
            CreateTestEnvironment();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("4. Configure Project Settings", GUILayout.Height(30)))
        {
            ConfigureProjectSettings();
        }
        
        GUILayout.Space(20);
        GUILayout.Label("Manual Setup Required:", EditorStyles.boldLabel);
        GUILayout.Label("- Configure Input Manager (see documentation)");
        GUILayout.Label("- Set up Layers in Project Settings");
        GUILayout.Label("- Adjust Character Controller values");
    }
    
    void CreatePlayer()
    {
        // Create player GameObject
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, 0);
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default"); // Change to "Player" layer when created
        
        // Add Character Controller
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0.001f;
        controller.center = new Vector3(0, 1, 0);
        
        // Add Player Controller script
        player.AddComponent<PlayerController>();
        
        // Create visual representation
        GameObject playerModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerModel.name = "PlayerModel";
        playerModel.transform.SetParent(player.transform);
        playerModel.transform.localPosition = new Vector3(0, 1, 0);
        playerModel.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        
        // Remove collider from visual
        DestroyImmediate(playerModel.GetComponent<CapsuleCollider>());
        
        // Create direction indicator
        GameObject directionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        directionIndicator.name = "DirectionIndicator";
        directionIndicator.transform.SetParent(playerModel.transform);
        directionIndicator.transform.localPosition = new Vector3(0, 0, 0.5f);
        directionIndicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.5f);
        DestroyImmediate(directionIndicator.GetComponent<BoxCollider>());
        
        // Color the direction indicator
        Renderer indicatorRenderer = directionIndicator.GetComponent<Renderer>();
        indicatorRenderer.material.color = Color.blue;
        
        Selection.activeGameObject = player;
        Debug.Log("Player created successfully! Don't forget to assign the Player layer when you create it.");
    }
    
    void SetupCamera()
    {
        // Find or create camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        
        // Position camera
        mainCamera.transform.position = new Vector3(0, 2.5f, -5f);
        mainCamera.transform.rotation = Quaternion.Euler(15f, 0, 0);
        
        // Camera settings
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 300f;
        
        // Add Camera Controller
        CameraController cameraController = mainCamera.gameObject.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = mainCamera.gameObject.AddComponent<CameraController>();
        }
        
        // Try to find and assign player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Set target in CameraController (you'll need to make the target field public or add a method)
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("Camera setup complete! Manually assign the Player transform to the Camera Controller's Target field.");
            }
        }
        
        Selection.activeGameObject = mainCamera.gameObject;
    }
    
    void CreateTestEnvironment()
    {
        // Create terrain
        GameObject terrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
        terrain.name = "Ground";
        terrain.transform.position = Vector3.zero;
        terrain.transform.localScale = new Vector3(20, 1, 20);
        terrain.layer = LayerMask.NameToLayer("Default"); // Change to "Environment" when created
        
        // Add some obstacles
        for (int i = 0; i < 5; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_{i}";
            obstacle.transform.position = new Vector3(
                Random.Range(-50f, 50f),
                1f,
                Random.Range(-50f, 50f)
            );
            obstacle.transform.localScale = new Vector3(
                Random.Range(2f, 5f),
                Random.Range(2f, 6f),
                Random.Range(2f, 5f)
            );
            obstacle.layer = LayerMask.NameToLayer("Default"); // Change to "Environment" when created
        }
        
        // Create some ramps
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.position = new Vector3(10, 0.5f, 0);
        ramp.transform.rotation = Quaternion.Euler(0, 0, 30);
        ramp.transform.localScale = new Vector3(5, 0.5f, 5);
        
        Debug.Log("Test environment created! Remember to assign Environment layer to all objects.");
    }
    
    void ConfigureProjectSettings()
    {
        // This would configure project settings programmatically
        Debug.Log("Please configure the following manually:");
        Debug.Log("1. Go to Edit > Project Settings > Time");
        Debug.Log("   - Fixed Timestep: 0.02");
        Debug.Log("   - Maximum Allowed Timestep: 0.1");
        Debug.Log("");
        Debug.Log("2. Go to Edit > Project Settings > Tags and Layers");
        Debug.Log("   - Add Layer 'Player' to slot 6");
        Debug.Log("   - Add Layer 'Environment' to slot 7");
        Debug.Log("   - Add Layer 'CameraCollision' to slot 8");
        Debug.Log("");
        Debug.Log("3. Configure Input Manager with WASD, Mouse, Jump, etc.");
    }
}
#endif