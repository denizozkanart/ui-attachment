using System;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoGames.UI
{
    /// <summary>
    /// Represents an attachment card in the bottom strip.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AttachmentItemButton : MonoBehaviour
    {
        [Header("Wiring")]
        public Image icon;
        public Image frame;
        public Image selectionBorder;
        public Image equippedBorder;
        public Text nameLabel;
        public GameObject equippedBadge;

        [Header("Colors")]
        public Color selectedColor = new Color(1f, 0.85f, 0.2f); // yellow for selection
        public Color equippedColor = new Color(0.2f, 0.8f, 0.3f); // green for equipped

        public Action<AttachmentItemButton> onClicked;

        public AttachmentData Data { get; private set; }

        private Button _button;
        private bool _selected;
        private bool _equipped;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
            SetSelected(false);
            SetEquipped(false);
        }

        public void Bind(AttachmentData data, Sprite sprite)
        {
            Data = data;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = sprite != null ? Color.white : Color.gray;
            }

            if (nameLabel != null)
            {
                nameLabel.text = data != null ? data.displayName.ToUpperInvariant() : "UNKNOWN";
            }

            SetSelected(false);
            SetEquipped(false);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (selectionBorder != null)
            {
                selectionBorder.enabled = selected;
                selectionBorder.color = selected ? selectedColor : Color.clear;
            }

            // When selected, hide equipped outline to avoid double border.
            if (equippedBorder != null)
            {
                equippedBorder.enabled = !_selected && _equipped;
                equippedBorder.color = _equipped ? equippedColor : Color.clear;
            }
        }

        public void SetEquipped(bool equipped)
        {
            _equipped = equipped;
            if (equippedBorder != null)
            {
                // Equipped outline removed per latest UX; keep border disabled.
                equippedBorder.enabled = false;
                equippedBorder.color = Color.clear;
            }

            if (equippedBadge != null)
            {
                // Equipped badge removed from design; keep it hidden regardless of state.
                equippedBadge.SetActive(false);
            }
        }

        private void HandleClick()
        {
            onClicked?.Invoke(this);
        }
    }
}
