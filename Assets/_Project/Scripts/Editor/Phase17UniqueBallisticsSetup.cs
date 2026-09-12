using System.IO;
using UnityEngine;
using UnityEditor;
using Gunbound.Combat;
using Gunbound.Player;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 17 Setup Utility:
    /// Generates specialized cannon projectile prefabs for Armor, Mage, and Boomer mobiles with custom physics behaviors
    /// (Shrapnel Split, Boomerang Wind Curve, Plasma Beam) and links them to MobileData ScriptableObjects (RF-5.2.1, RF-5.2.2).
    /// </summary>
    public static class Phase17UniqueBallisticsSetup
    {
        [MenuItem("Gunbound/Setup Phase 17 (Unique Mobile Ballistics)")]
        public static void SetupPhase17()
        {
            Debug.Log("[Phase17Setup] Starting Unique Mobile Ballistics Setup...");

            string prefabFolder = "Assets/_Project/Prefabs/Projectiles";
            if (!Directory.Exists(prefabFolder))
            {
                Directory.CreateDirectory(prefabFolder);
                AssetDatabase.Refresh();
            }

            // Find base Projectile prefab or create new
            GameObject baseTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFolder + "/Projectile_Base.prefab");
            if (baseTemplate == null)
            {
                baseTemplate = CreateBaseProjectileGameObject();
                PrefabUtility.SaveAsPrefabAsset(baseTemplate, prefabFolder + "/Projectile_Base.prefab");
                Object.DestroyImmediate(baseTemplate);
                AssetDatabase.Refresh();
                baseTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFolder + "/Projectile_Base.prefab");
            }

            // Armor Prefabs
            GameObject armorT1 = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Armor_T1.prefab", baseTemplate, 300f, 3.0f, SpecialProjectileType.Standard, new Color(0.8f, 0.4f, 0.2f));
            GameObject armorT2 = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Armor_T2.prefab", baseTemplate, 450f, 3.5f, SpecialProjectileType.ShrapnelSplit, new Color(0.9f, 0.3f, 0.1f));
            GameObject armorSS = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Armor_SS.prefab", baseTemplate, 700f, 5.5f, SpecialProjectileType.Standard, new Color(1.0f, 0.2f, 0.0f));

            // Mage Prefabs
            GameObject mageT1 = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Mage_T1.prefab", baseTemplate, 250f, 2.5f, SpecialProjectileType.PlasmaBeam, new Color(0.2f, 0.7f, 1.0f));
            GameObject mageT2 = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Mage_T2.prefab", baseTemplate, 400f, 3.8f, SpecialProjectileType.PlasmaBeam, new Color(0.1f, 0.5f, 1.0f));
            GameObject mageSS = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Mage_SS.prefab", baseTemplate, 650f, 5.0f, SpecialProjectileType.PlasmaBeam, new Color(0.4f, 0.2f, 1.0f));

            // Boomer Prefabs
            GameObject boomerT1 = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Boomer_T1.prefab", baseTemplate, 240f, 2.3f, SpecialProjectileType.BoomerangWind, new Color(0.3f, 0.9f, 0.4f));
            GameObject boomerT2 = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Boomer_T2.prefab", baseTemplate, 380f, 3.5f, SpecialProjectileType.BoomerangWind, new Color(0.2f, 0.8f, 0.3f));
            GameObject boomerSS = CreateOrUpdateProjectilePrefab(prefabFolder + "/Projectile_Boomer_SS.prefab", baseTemplate, 620f, 4.8f, SpecialProjectileType.BoomerangWind, new Color(0.9f, 0.9f, 0.2f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Link prefabs to MobileData ScriptableObject assets
            string mobileFolder = "Assets/_Project/Data/Mobiles";
            LinkPrefabsToMobileData(mobileFolder + "/Mobile_Armor.asset", armorT1, armorT2, armorSS);
            LinkPrefabsToMobileData(mobileFolder + "/Mobile_Mage.asset", mageT1, mageT2, mageSS);
            LinkPrefabsToMobileData(mobileFolder + "/Mobile_Boomer.asset", boomerT1, boomerT2, boomerSS);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase17Setup] Phase 17 Unique Mobile Ballistics Setup completed successfully!");
        }

        private static GameObject CreateBaseProjectileGameObject()
        {
            GameObject go = new GameObject("Projectile_Base", typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(Projectile), typeof(TrailRenderer));
            
            var col = go.GetComponent<CircleCollider2D>();
            col.radius = 0.15f;

            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var proj = go.GetComponent<Projectile>();
            
            var trail = go.GetComponent<TrailRenderer>();
            trail.time = 0.6f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.02f;

            return go;
        }

        private static GameObject CreateOrUpdateProjectilePrefab(string prefabPath, GameObject template, float damage, float radius, SpecialProjectileType specialType, Color color)
        {
            GameObject instance = Object.Instantiate(template);
            instance.name = Path.GetFileNameWithoutExtension(prefabPath);

            Projectile proj = instance.GetComponent<Projectile>();
            if (proj != null)
            {
                SerializedObject so = new SerializedObject(proj);
                so.FindProperty("maxDamage").floatValue = damage;
                so.FindProperty("explosionRadius").floatValue = radius;
                so.FindProperty("_specialType").enumValueIndex = (int)specialType;
                so.ApplyModifiedProperties();
            }

            TrailRenderer trail = instance.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color * 0.7f, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                trail.colorGradient = gradient;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static void LinkPrefabsToMobileData(string dataPath, GameObject t1, GameObject t2, GameObject ss)
        {
            MobileData data = AssetDatabase.LoadAssetAtPath<MobileData>(dataPath);
            if (data == null)
            {
                Debug.LogWarning($"[Phase17Setup] MobileData asset not found at {dataPath}");
                return;
            }

            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_shot1Prefab").objectReferenceValue = t1;
            so.FindProperty("_shot2Prefab").objectReferenceValue = t2;
            so.FindProperty("_ssPrefab").objectReferenceValue = ss;
            so.ApplyModifiedProperties();

            Debug.Log($"[Phase17Setup] Linked ballistics prefabs to '{data.name}': T1={t1.name}, T2={t2.name}, SS={ss.name}");
        }
    }
}
