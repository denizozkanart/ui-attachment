using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertigoGames.UI
{
    [CreateAssetMenu(fileName = "AttachmentViewSettings", menuName = "Vertigo/Attachment View Settings")]
    public class AttachmentViewSettings : ScriptableObject
    {
        [Serializable]
        public struct CategoryView
        {
            public AttachmentCategory category;
            public Vector3 localOffset;
            public float distance;
        }

        [Header("Camera")]
        public Vector3 cameraPosition = new Vector3(-2.247f, 0f, 0.05f);
        public Vector3 cameraEuler = new Vector3(0f, 90f, 0f);
        public Vector3 focusDirection = new Vector3(-1f, 0f, 0f);
        public Vector3 cameraOffset = Vector3.zero;
        public float focusPadding = 1.2f;

        [Header("Per Category")]
        public List<CategoryView> categories = new List<CategoryView>
        {
            new CategoryView{ category = AttachmentCategory.Sight, localOffset = Vector3.zero, distance = 2.0f },
            new CategoryView{ category = AttachmentCategory.Mag, localOffset = Vector3.zero, distance = 2.0f },
            new CategoryView{ category = AttachmentCategory.Barrel, localOffset = Vector3.zero, distance = 2.2f },
            new CategoryView{ category = AttachmentCategory.Stock, localOffset = Vector3.zero, distance = 2.4f },
            new CategoryView{ category = AttachmentCategory.Tactical, localOffset = Vector3.zero, distance = 2.2f },
        };
    }
}
