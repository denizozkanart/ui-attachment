using System.Collections.Generic;
using UnityEngine;

namespace VertigoGames.UI
{
    /// <summary>
    /// Represents a mutable set of weapon stats.
    /// </summary>
    [System.Serializable]
    public class WeaponStats
    {
        // Base (naked) weapon stats
        public float power = 56f;
        public float damage = 82f;
        public float fireRate = 800f;
        public float accuracy = 80f;
        public float speed = 96f;
        public float range = 24.2f;
        public float reload = 2.0f;

        public float GetValue(StatType statType)
        {
            return statType switch
            {
                StatType.Power => power,
                StatType.Damage => damage,
                StatType.FireRate => fireRate,
                StatType.Accuracy => accuracy,
                StatType.Speed => speed,
                StatType.Range => range,
                StatType.Reload => reload,
                _ => 0f
            };
        }

        public WeaponStats Clone()
        {
            return new WeaponStats
            {
                power = power,
                damage = damage,
                fireRate = fireRate,
                accuracy = accuracy,
                speed = speed,
                range = range,
                reload = reload
            };
        }

        public static WeaponStats operator +(WeaponStats a, IEnumerable<StatModifier> modifiers)
        {
            var copy = a.Clone();
            if (modifiers == null) return copy;
            foreach (var mod in modifiers)
            {
                if (mod == null) continue;
                copy.Apply(mod);
            }
            return copy;
        }

        public void Apply(StatModifier mod)
        {
            switch (mod.stat)
            {
                case StatType.Power:
                    power += mod.value;
                    break;
                case StatType.Damage:
                    damage += mod.value;
                    break;
                case StatType.FireRate:
                    fireRate += mod.value;
                    break;
                case StatType.Accuracy:
                    accuracy += mod.value;
                    break;
                case StatType.Speed:
                    speed += mod.value;
                    break;
                case StatType.Range:
                    range += mod.value;
                    break;
                case StatType.Reload:
                    reload += mod.value;
                    break;
            }
        }
    }
}
