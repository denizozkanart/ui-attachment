using System;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoGames.UI
{
    /// <summary>
    /// Handles visuals and click callbacks for a category button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CategoryButton : MonoBehaviour
    {
        [Header("Wiring")]
        public AttachmentCategory category;
        public Image icon;
        public Image background;
        public Image border;
        public Text label;

        [Header("Colors")]
        public Color normalBackground = new Color(0.36f, 0.63f, 1f, 0.51f);    // #5DA1FF alpha 130/255
        public Color selectedBackground = new Color(0.36f, 0.63f, 1f, 0.51f);  // same tone, keep alpha
        public Color selectedUnderline = new Color(1f, 0.85f, 0.2f);

        public Action<AttachmentCategory> onClicked;

        private Button _button;
        private Sprite _defaultIcon;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
            AutoWire();
            if (icon != null) _defaultIcon = icon.sprite;
        }

        private void HandleClick()
        {
            onClicked?.Invoke(category);
        }

        public void SetSelected(bool selected)
        {
            if (border != null)
            {
                border.gameObject.SetActive(selected);
                border.color = selected ? selectedUnderline : Color.clear;
            }

            if (background != null)
            {
                background.color = selected ? selectedBackground : normalBackground;
                background.enabled = true;
            }
        }

        public void SetEquippedSprite(Sprite sprite)
        {
            if (icon == null) return;
            icon.sprite = sprite != null ? sprite : _defaultIcon;
        }

        public void SetLabel(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void AutoWire()
        {
            if (icon == null)
            {
                icon = transform.Find("Icon")?.GetComponent<Image>()
                       ?? GetComponentInChildren<Image>(true);
            }

            if (label == null)
            {
                label = transform.Find("Label")?.GetComponent<Text>()
                        ?? GetComponentInChildren<Text>(true);
            }

            if (background == null)
            {
                background = transform.Find("Background")?.GetComponent<Image>()
                             ?? GetComponent<Image>();
            }

            if (border == null)
            {
                border = transform.Find("Border")?.GetComponent<Image>();
            }
        }
    }
}
