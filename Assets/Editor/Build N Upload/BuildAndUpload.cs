using UnityEditor;
using UnityEngine;
using Renci.SshNet;
using System.IO;
using System.IO.Compression;
using System.Linq;

public class BuildAndUpload : EditorWindow
{
    private string outputPath = "Builds/MyGame";
    private string zipPath = "Builds/MyGame.zip";
    private string serverIP = "your_rpi_ip";
    private string username = "your_username";
    private string password = "your_password";

    [MenuItem("Tools/Build and Upload/RPI")]
    public static void ShowWindow()
    {
        GetWindow(typeof(BuildAndUpload), false, "Build and Upload");
    }

    private void OnGUI()
    {
        GUILayout.Label("Build and Upload Settings", EditorStyles.boldLabel);

        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        zipPath = EditorGUILayout.TextField("Zip Path", zipPath);
        serverIP = EditorGUILayout.TextField("Server IP", serverIP);
        username = EditorGUILayout.TextField("Username", username);
        password = EditorGUILayout.PasswordField("Password", password);

        // Ensure GUILayout.BeginHorizontal() or EditorGUILayout.BeginHorizontal() is matched with GUILayout.EndHorizontal() or EditorGUILayout.EndHorizontal()
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build, Zip, and Upload"))
        {
            BuildGame();
            ZipBuild();
            UploadToServer();
        }
        EditorGUILayout.EndHorizontal(); // Match every BeginHorizontal with an EndHorizontal
    }


    private void BuildGame()
    {
        // Retrieve current build settings directly
        string[] defaultScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        
        string defaultLocationPathName = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.activeBuildTarget);

        // Configure build options to use the default build settings
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = defaultScenes,
            locationPathName = string.IsNullOrEmpty(defaultLocationPathName) ? outputPath : defaultLocationPathName,
            target = EditorUserBuildSettings.activeBuildTarget,
            options = BuildOptions.None
        };

        // Build the project with the current settings
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log("Build completed using default settings!");
    }

    private void ZipBuild()
    {
        // Delete any existing zip file
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        // Create a zip archive of the build directory
        ZipFile.CreateFromDirectory(outputPath, zipPath);
        Debug.Log("Zipping completed!");
    }

    private void UploadToServer()
    {
        try
        {
            using (var client = new SshClient(serverIP, username, password))
            {
                client.Connect();
                using (var scp = new ScpClient(client.ConnectionInfo))
                {
                    scp.Connect();
                    scp.Upload(new FileInfo(zipPath), "/var/www/html/mygame.zip");
                    Debug.Log("Upload completed!");
                    scp.Disconnect();
                }
                client.Disconnect();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to upload: " + e.Message);
        }
    }
}
