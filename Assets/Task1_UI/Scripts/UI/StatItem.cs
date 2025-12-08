using UnityEngine;
using UnityEngine.UI;

namespace VertigoGames.UI
{
    /// <summary>
    /// Single stat row UI element. Tasarim sahnede belirlensin diye renk/align'e dokunmaz.
    /// </summary>
    public class StatItem : MonoBehaviour
    {
        public Text label;
        public Text value;
        public Text delta;
        public Image icon;
        public Image background;

        private void Awake()
        {
            // Yeni hiyerarsi icin otomatik referans doldurma
            // Icon her zaman child/Icon olsun (serialized referans parent'e tutturulmus olsa bile override et)
            icon = transform.Find("IconItem/Icon")?.GetComponent<Image>()
                   ?? transform.Find("Iconitem/Icon")?.GetComponent<Image>()
                   ?? transform.Find("Icon/Icon")?.GetComponent<Image>();

            if (label == null)
            {
                label = transform.Find("LabelItem/Label")?.GetComponent<Text>()
                        ?? transform.Find("Labelitems/Label")?.GetComponent<Text>()
                        ?? transform.Find("Label/Label")?.GetComponent<Text>();
            }

            if (value == null)
            {
                value = transform.Find("LabelItem/Value")?.GetComponent<Text>()
                        ?? transform.Find("Labelitems/Value")?.GetComponent<Text>()
                        ?? transform.Find("Label/Value")?.GetComponent<Text>();
            }

            if (delta == null)
            {
                delta = transform.Find("DeltaItem/Delta")?.GetComponent<Text>()
                        ?? transform.Find("Deltaitem/Delta")?.GetComponent<Text>()
                        ?? transform.Find("Delta/Delta")?.GetComponent<Text>()
                        ?? transform.Find("Delta")?.GetComponent<Text>();
            }
            if (background == null) background = GetComponent<Image>();
        }

        public void Bind(string labelText, float baseValue, float deltaValue, Sprite iconSprite)
        {
            var targetLabel = label != null ? label
                : transform.Find("LabelItem/Label")?.GetComponent<Text>()
                ?? transform.Find("Labelitems/Label")?.GetComponent<Text>()
                ?? transform.Find("Label/Label")?.GetComponent<Text>();
            if (targetLabel != null)
            {
                targetLabel.text = labelText;
                label = targetLabel;
            }
            if (value != null) value.text = baseValue.ToString("0.##");

            if (delta != null)
            {
                if (Mathf.Approximately(deltaValue, 0f))
                {
                    delta.text = string.Empty;
                    delta.enabled = false;
                }
                else
                {
                    var sign = deltaValue > 0 ? "+" : string.Empty;
                    delta.text = $"{sign}{deltaValue:0.##}";
                    delta.enabled = true;
                }
            }

            // Sadece Icon child'ına bas, IconItem'a dokunma
            var targetIcon = icon != null ? icon
                : transform.Find("IconItem/Icon")?.GetComponent<Image>()
                ?? transform.Find("Iconitem/Icon")?.GetComponent<Image>()
                ?? transform.Find("Icon/Icon")?.GetComponent<Image>();
            if (targetIcon != null)
            {
                targetIcon.sprite = iconSprite; // null da olsa override et
                icon = targetIcon;
            }

            // IconItem kabuğunun sprite'ini temizle (Source Image = None)
            var iconItemImage = transform.Find("IconItem")?.GetComponent<Image>()
                               ?? transform.Find("Iconitem")?.GetComponent<Image>();
            if (iconItemImage != null) iconItemImage.sprite = null;
            // Background/renk ve layout sahne tarafinda ayarli kalsin; burada degistirmiyoruz.
        }
    }
}
