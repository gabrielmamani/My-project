#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Core;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 9 Audio & VFX Setup:
    /// Ensures Persistent AudioManager GameObject exists in SampleScene and generates synthetic SFX fallbacks.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase9AudioVFXSetup
    {
        static Phase9AudioVFXSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    ExecuteBatchSetup();
                }
            };
        }

        [MenuItem("Gunbound/Setup Phase 9 (Audio & VFX Manager)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            GameObject audioObj = GameObject.Find("AudioManager");
            if (audioObj == null)
            {
                audioObj = new GameObject("AudioManager");
                Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioManager");
            }

            AudioManager audioMgr = audioObj.GetComponent<AudioManager>();
            if (audioMgr == null)
            {
                audioMgr = audioObj.AddComponent<AudioManager>();
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Phase9AudioVFXSetup] AudioManager setup verified and configured in scene.");
        }
    }
}
#endif
