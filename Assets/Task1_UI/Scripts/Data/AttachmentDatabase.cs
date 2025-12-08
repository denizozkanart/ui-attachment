using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VertigoGames.UI
{
    /// <summary>
    /// Simple in-memory database that seeds placeholder attachments.
    /// </summary>
    public class AttachmentDatabase : MonoBehaviour
    {
        [Tooltip("Optional explicit attachment list. Leave empty to auto-generate placeholder data.")]
        public List<AttachmentData> attachments = new();

        [Tooltip("Fallback icon used when a specific icon cannot be found.")]
        public Sprite fallbackIcon;

        private readonly Dictionary<AttachmentCategory, List<AttachmentData>> _byCategory = new();
        private readonly Dictionary<string, Sprite> _iconCache = new();

        public void Initialize()
        {
            // Always reseed to keep data/config in sync with code changes
            SeedPlaceholderData();
            BuildCategoryLookup();
            LoadIcons();
        }

        public IReadOnlyList<AttachmentData> GetByCategory(AttachmentCategory category)
        {
            if (_byCategory.TryGetValue(category, out var list))
            {
                return list;
            }
            return new List<AttachmentData>();
        }

        public AttachmentData GetDefault(AttachmentCategory category)
        {
            var list = GetByCategory(category);
            return list.Count > 0 ? list[0] : null;
        }

        public Sprite GetIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return fallbackIcon;
            if (_iconCache.TryGetValue(iconName, out var icon) && icon != null)
            {
                return icon;
            }
#if UNITY_EDITOR
            var editorIcon = LoadIconEditor(iconName);
            if (editorIcon != null)
            {
                _iconCache[iconName] = editorIcon;
                return editorIcon;
            }
#endif
            return fallbackIcon;
        }

        private void BuildCategoryLookup()
        {
            _byCategory.Clear();
            foreach (AttachmentCategory cat in System.Enum.GetValues(typeof(AttachmentCategory)))
            {
                _byCategory[cat] = new List<AttachmentData>();
            }

            foreach (var att in attachments)
            {
                if (att == null) continue;
                if (!_byCategory.TryGetValue(att.category, out var list))
                {
                    list = new List<AttachmentData>();
                    _byCategory[att.category] = list;
                }
                list.Add(att);
            }
        }

        private void SeedPlaceholderData()
        {
            attachments = new List<AttachmentData>
            {
                // Sight
                CreateAttachment("sight_default", "Default Sight", AttachmentCategory.Sight, "ui_icon_att_primary_dash_att_00_sight_0", Color.white,
                    (+0f, StatType.Accuracy), (+0f, StatType.Power)),
                // Thermal Imaging: explicit boosts to show positive deltas on Power/Damage/Range
                CreateAttachment("sight_thermal", "Thermal Imaging", AttachmentCategory.Sight, "ui_icon_att_primary_dash_att_01_sight_1", new Color(0.2f, 0.8f, 0.2f),
                    (+7.39f, StatType.Power), (+8.2f, StatType.Damage), (+2.2f, StatType.Range)),
                CreateAttachment("sight_night", "Nightstalker", AttachmentCategory.Sight, "ui_icon_att_primary_dash_att_02_sight_2", new Color(0.1f, 0.6f, 0.9f),
                    (+18f, StatType.Accuracy), (+6f, StatType.Range), (-6f, StatType.Speed)),

                // Mag
                CreateAttachment("mag_default", "DEFAULT MAG", AttachmentCategory.Mag, "ui_icon_att_primary_dash_att_03_mag_0", Color.white,
                    (+0f, StatType.FireRate)),
                CreateAttachment("mag_flashload", "FLASHLOAD", AttachmentCategory.Mag, "ui_icon_att_primary_dash_att_04_mag_1", new Color(0.8f, 0.8f, 0.2f),
                    (+20f, StatType.FireRate), (-4f, StatType.Power)),
                CreateAttachment("mag_bulletstorm", "BULLETSTORM", AttachmentCategory.Mag, "ui_icon_att_primary_dash_att_05_mag_2", new Color(0.6f, 0.85f, 0.3f),
                    (+35f, StatType.FireRate), (-6f, StatType.Accuracy)),
                CreateAttachment("mag_quickload", "QUICKLOAD", AttachmentCategory.Mag, "ui_icon_att_primary_dash_att_06_mag_3", new Color(0.25f, 0.7f, 0.9f),
                    (-8f, StatType.Reload), (+5f, StatType.Speed)),
                CreateAttachment("mag_reloader", "RELOADER", AttachmentCategory.Mag, "ui_icon_att_primary_dash_att_07_mag_4", new Color(0.9f, 0.5f, 0.2f),
                    (-10f, StatType.Reload), (+10f, StatType.Power)),

                // Barrel
                CreateAttachment("barrel_default", "Default Barrel", AttachmentCategory.Barrel, "ui_icon_att_primary_dash_att_08_barrel_0", Color.white,
                    (+0f, StatType.Damage)),
                CreateAttachment("barrel_cool", "Cool Breeze", AttachmentCategory.Barrel, "ui_icon_att_primary_dash_att_09_barrel_1", new Color(0.3f, 0.7f, 0.2f),
                    (+10f, StatType.Range), (+8f, StatType.Accuracy)),
                CreateAttachment("barrel_peace", "Peace Keeper", AttachmentCategory.Barrel, "ui_icon_att_primary_dash_att_10_barrel_2", new Color(0.2f, 0.5f, 0.8f),
                    (+28f, StatType.Damage), (-4f, StatType.Speed)),

                // Stock
                CreateAttachment("stock_default", "Default Stock", AttachmentCategory.Stock, "ui_icon_att_primary_dash_att_14_stock_0", Color.white,
                    (+0f, StatType.Accuracy)),
                CreateAttachment("stock_light", "Feather Stock", AttachmentCategory.Stock, "ui_icon_att_primary_dash_att_15_stock_1", new Color(0.6f, 0.8f, 0.2f),
                    (+8f, StatType.Speed), (-4f, StatType.Accuracy)),

                // Tactical
                CreateAttachment("tac_default", "Default Tactical", AttachmentCategory.Tactical, "ui_icon_att_primary_dash_att_11_tactical_0", Color.white,
                    (+0f, StatType.Power)),
                CreateAttachment("tac_grip", "Stabilizer Grip", AttachmentCategory.Tactical, "ui_icon_att_primary_dash_att_12_tactical_1", new Color(0.3f, 0.6f, 0.2f),
                    (+12f, StatType.Accuracy), (+8f, StatType.Range)),
                CreateAttachment("tac_poison", "Poison Module", AttachmentCategory.Tactical, "ui_icon_att_primary_dash_att_13_tactical_2", new Color(0.8f, 0.3f, 0.7f),
                    (+55f, StatType.Power), (+110.4f, StatType.Damage), (-6f, StatType.Speed))
            };
        }

        private AttachmentData CreateAttachment(string id, string name, AttachmentCategory category, string iconName, Color color, params (float value, StatType stat)[] mods)
        {
            var data = new AttachmentData
            {
                id = id,
                displayName = name,
                category = category,
                iconName = iconName,
                previewColor = color,
                statModifiers = new List<StatModifier>()
            };
            foreach (var mod in mods)
            {
                data.statModifiers.Add(new StatModifier { stat = mod.stat, value = mod.value });
            }
            return data;
        }

        private void LoadIcons()
        {
            _iconCache.Clear();
#if UNITY_EDITOR
            foreach (var att in attachments)
            {
                if (att == null || string.IsNullOrEmpty(att.iconName)) continue;
                var sprite = LoadIconEditor(att.iconName);
                if (sprite != null)
                {
                    _iconCache[att.iconName] = sprite;
                }
            }
#endif
        }

#if UNITY_EDITOR
        private Sprite LoadIconEditor(string iconName)
        {
            var path = $"Assets/Task1_UI/weapon/icons/{iconName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite;
        }
#endif
    }
}
