#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class QuickSceneSetup : EditorWindow
{
    [MenuItem("WoW Tools/Quick Scene Setup (One Click)")]
    public static void SetupCompleteScene()
    {
        Debug.Log("=== Starting WoW Movement Scene Setup ===");

        // 1. Create Player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            Debug.Log("✓ Created Player GameObject");
        }

        // Setup player components
        player.transform.position = new Vector3(0, 1, 0);
        player.tag = "Player";

        // Add Character Controller
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0, 1, 0);
            Debug.Log("✓ Added Character Controller");
        }

        // Add PlayerController script
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = player.AddComponent<PlayerController>();
            Debug.Log("✓ Added PlayerController script");
        }

        // Create visual
        Transform playerModel = player.transform.Find("PlayerModel");
        if (playerModel == null)
        {
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "PlayerModel";
            model.transform.SetParent(player.transform);
            model.transform.localPosition = new Vector3(0, 1, 0);
            DestroyImmediate(model.GetComponent<CapsuleCollider>());
            Debug.Log("✓ Created Player Model");
        }

        // 2. Setup Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
        }

        mainCam.transform.position = new Vector3(0, 2.5f, -5f);
        mainCam.transform.rotation = Quaternion.Euler(15f, 0, 0);

        CameraController camController = mainCam.GetComponent<CameraController>();
        if (camController == null)
        {
            camController = mainCam.gameObject.AddComponent<CameraController>();
            Debug.Log("✓ Added CameraController");
        }

        // Set camera target
        camController.SetTarget(player.transform);
        Debug.Log("✓ Set Camera Target to Player");

        // 3. Create Ground
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);

            // Add material
            Renderer renderer = ground.GetComponent<Renderer>();
            renderer.material.color = new Color(0.3f, 0.5f, 0.3f); // Grass green
            Debug.Log("✓ Created Ground");
        }

        // 4. Create a test cube
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "TestObstacle";
        testCube.transform.position = new Vector3(5, 1, 0);
        testCube.transform.localScale = new Vector3(2, 2, 2);

        // 5. Setup Lighting
        Light directionalLight = FindObjectOfType<Light>();
        if (directionalLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            directionalLight = lightObj.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            Debug.Log("✓ Created Lighting");
        }

        // Select player
        Selection.activeGameObject = player;

        Debug.Log("=== Scene Setup Complete! ===");
        Debug.Log("INSTRUCTIONS:");
        Debug.Log("1. Press Play");
        Debug.Log("2. Use WASD to move");
        Debug.Log("3. Hold Right Mouse to look around");
        Debug.Log("4. Press Space to jump");
        Debug.Log("5. Scroll Mouse Wheel to zoom");

        // Show reminder about layers
        if (!LayerMask.NameToLayer("Player").Equals(6))
        {
            Debug.LogWarning("! Remember to create 'Player' layer in Project Settings > Tags & Layers > Layer 6");
        }
    }

    [MenuItem("WoW Tools/Debug Info")]
    public static void ShowDebugInfo()
    {
        Debug.Log("=== WoW Movement Debug Info ===");

        GameObject player = GameObject.FindWithTag("Player");
        if (player)
        {
            Debug.Log("✓ Player found");
            Debug.Log($"  - Has CharacterController: {player.GetComponent<CharacterController>() != null}");
            Debug.Log($"  - Has PlayerController: {player.GetComponent<PlayerController>() != null}");
            Debug.Log($"  - Position: {player.transform.position}");
        }
        else
        {
            Debug.LogError("✗ No Player found!");
        }

        Camera cam = Camera.main;
        if (cam)
        {
            Debug.Log("✓ Camera found");
            CameraController cc = cam.GetComponent<CameraController>();
            Debug.Log($"  - Has CameraController: {cc != null}");
            if (cc != null)
            {
                Debug.Log($"  - Target assigned: {cc.GetComponent<CameraController>() != null}");
            }
        }
        else
        {
            Debug.LogError("✗ No Main Camera found!");
        }

        Debug.Log($"✓ Input Axes:");
        Debug.Log($"  - Horizontal exists: {Input.GetAxis("Horizontal") != null}");
        Debug.Log($"  - Vertical exists: {Input.GetAxis("Vertical") != null}");
    }
}
#endif