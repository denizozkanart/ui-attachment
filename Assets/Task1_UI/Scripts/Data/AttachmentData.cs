using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertigoGames.UI
{
    [Serializable]
    public class StatModifier
    {
        public StatType stat;
        public float value;
    }

    /// <summary>
    /// Simple DTO representing an attachment entry.
    /// </summary>
    [Serializable]
    public class AttachmentData
    {
        public string id;
        public string displayName;
        public AttachmentCategory category;
        public string iconName;
        public Color previewColor = Color.white;
        public List<StatModifier> statModifiers = new();

        public float GetDelta(StatType statType)
        {
            float delta = 0f;
            if (statModifiers == null) return delta;
            foreach (var mod in statModifiers)
            {
                if (mod != null && mod.stat == statType)
                {
                    delta += mod.value;
                }
            }
            return delta;
        }
    }
}
