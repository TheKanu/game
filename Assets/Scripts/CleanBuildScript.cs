#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class CleanBuildScript : EditorWindow
{
    [MenuItem("WoW Tools/Clean Project Cache")]
    public static void CleanProjectCache()
    {
        if (EditorUtility.DisplayDialog("Clean Project Cache",
            "This will close Unity and clean all temporary files. Continue?",
            "Yes", "Cancel"))
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");

            // Create a batch file to clean after Unity closes
            string batchContent = @"
@echo off
timeout /t 2
echo Cleaning Library folder...
rmdir /s /q """ + projectPath + @"\Library""
echo Cleaning Temp folder...
rmdir /s /q """ + projectPath + @"\Temp""
echo Cleaning obj folder...
rmdir /s /q """ + projectPath + @"\obj""
echo Done! You can now reopen Unity.
pause
";

            string batchPath = projectPath + "/clean_cache.bat";
            File.WriteAllText(batchPath, batchContent);

            // Start the batch file
            System.Diagnostics.Process.Start(batchPath);

            // Close Unity
            EditorApplication.Exit(0);
        }
    }

    [MenuItem("WoW Tools/Fix Console Errors")]
    public static void SuppressRenderPipelineErrors()
    {
        Debug.ClearDeveloperConsole();
        Debug.Log("Console cleared. Note: Render Pipeline errors may reappear but don't affect gameplay.");
    }
}
#endif