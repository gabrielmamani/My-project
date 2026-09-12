using System;
using UnityEngine;

namespace Gunbound.Player
{
    /// <summary>
    /// Enumeration of available weapon shot types.
    /// </summary>
    public enum WeaponType
    {
        Shot1,
        Shot2,
        SS
    }

    /// <summary>
    /// Configuration data container for weapon shot properties.
    /// </summary>
    [Serializable]
    public class WeaponData
    {
        public WeaponType weaponType = WeaponType.Shot1;
        public string weaponName = "Tiro 1";
        public GameObject projectilePrefab;
        public float damage = 250f;
        public float explosionRadius = 2.5f;
        public int baseDelay = 250;
    }
}
