using UnityEngine;
using UnityEditor;
using Gunbound.Environment;

namespace Gunbound.EditorTools
{
    public static class SetupDeathZoneAndTerrain
    {
        [MenuItem("Gunbound/Setup DeathZone and Terrain")]
        public static void Setup()
        {
            GameObject deathZoneObj = GameObject.Find("DeathZone");
            if (deathZoneObj == null)
            {
                deathZoneObj = new GameObject("DeathZone");
                Undo.RegisterCreatedObjectUndo(deathZoneObj, "Create DeathZone");
                Debug.Log("[Setup] Created new 'DeathZone' GameObject");
            }
            else
            {
                Undo.RecordObject(deathZoneObj, "Update DeathZone");
                Debug.Log("[Setup] Found existing 'DeathZone' GameObject");
            }

            deathZoneObj.transform.position = new Vector3(0f, -8f, 0f);

            BoxCollider2D boxCol = deathZoneObj.GetComponent<BoxCollider2D>();
            if (boxCol == null)
            {
                boxCol = deathZoneObj.AddComponent<BoxCollider2D>();
            }
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(100f, 6f);

            DeathTrigger deathTrigger = deathZoneObj.GetComponent<DeathTrigger>();
            if (deathTrigger == null)
            {
                deathTrigger = deathZoneObj.AddComponent<DeathTrigger>();
            }

            GameObject groundObj = GameObject.Find("Ground") ?? GameObject.Find("Terrain") ?? GameObject.Find("Map");
            if (groundObj != null)
            {
                DestructibleTerrain terrain = groundObj.GetComponent<DestructibleTerrain>();
                if (terrain == null)
                {
                    terrain = groundObj.AddComponent<DestructibleTerrain>();
                    Debug.Log($"[Setup] Added DestructibleTerrain component to '{groundObj.name}'");
                }
            }

            Debug.Log("[Setup] DeathZone and Destructible Terrain setup successfully at Y = -8!");
        }
    }
}
