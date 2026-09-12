#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Utility to automatically register active scene to EditorBuildSettings.scenes
    /// and configure NetworkManager SceneManagement settings to prevent Netcode popups (RF-07.1.1).
    /// </summary>
    [InitializeOnLoad]
    public static class AddSceneToBuildSettings
    {
        static AddSceneToBuildSettings()
        {
            EditorApplication.delayCall += () =>
            {
                RegisterActiveSceneToBuildSettings();
            };
        }

        [MenuItem("Gunbound/Add Active Scene To Build Settings")]
        public static void RegisterActiveSceneToBuildSettings()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path))
            {
                var scenes = EditorBuildSettings.scenes;
                bool exists = false;
                foreach (var s in scenes)
                {
                    if (s.path == activeScene.path)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
                    System.Array.Copy(scenes, newScenes, scenes.Length);
                    newScenes[scenes.Length] = new EditorBuildSettingsScene(activeScene.path, true);
                    EditorBuildSettings.scenes = newScenes;
                    Debug.Log($"[AddSceneToBuildSettings] Successfully added '{activeScene.path}' to EditorBuildSettings.scenes!");
                }
            }

            // Also configure NetworkManager if present
            var netManager = Object.FindFirstObjectByType<NetworkManager>();
            if (netManager != null && netManager.NetworkConfig != null)
            {
                netManager.NetworkConfig.EnableSceneManagement = false;
                EditorUtility.SetDirty(netManager);
                Debug.Log("[AddSceneToBuildSettings] NetworkConfig.EnableSceneManagement set to false to prevent scene sync prompts.");
            }
        }
    }
}
#endif
