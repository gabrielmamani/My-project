#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.EditorTools;

namespace Gunbound.EditorTools
{
    [InitializeOnLoad]
    public static class AutoSaveOnLoad
    {
        static AutoSaveOnLoad()
        {
            EditorApplication.update += RunOnce;
        }

        private static void RunOnce()
        {
            EditorApplication.update -= RunOnce;

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded && activeScene.name == "SampleScene")
            {
                GameObject p2 = GameObject.Find("Player_2");
                if (p2 == null)
                {
                    Debug.Log("[AutoSaveOnLoad] Player_2 missing in scene. Building Player_2 and saving scene...");
                    Player2Setup.SetupPlayer2AndDuel();
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[AutoSaveOnLoad] SampleScene.unity successfully updated and saved!");
                }
            }
        }
    }
}
#endif
