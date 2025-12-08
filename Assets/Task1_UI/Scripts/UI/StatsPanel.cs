using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoGames.UI
{
    /// <summary>
    /// Controls the right-side stats list. Reuses existing StatItem children; renk/layout sahneye birakilir.
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [Header("Wiring")]
        public Transform contentRoot;
        public ScrollRect scrollRect;

        [Header("Icons")]
        public Sprite powerIcon;
        public Sprite damageIcon;
        public Sprite fireRateIcon;
        public Sprite accuracyIcon;
        public Sprite speedIcon;
        public Sprite rangeIcon;
        public Sprite reloadIcon;

        private readonly List<StatItem> _items = new();
        private readonly Dictionary<StatItem, Color> _defaultBg = new();
        private readonly Dictionary<StatItem, Color> _defaultIconShell = new();
        private readonly Dictionary<StatItem, Color> _defaultDelta = new();
        private readonly List<Row> _rowsBuffer = new(8);
        private readonly List<StatItem> _itemOrderBuffer = new(8);

        private struct Row
        {
            public StatType stat;
            public float finalVal;
            public float delta;
            public int orderIndex;
        }

        private readonly StatType[] _order =
        {
            StatType.Power,
            StatType.Damage,
            StatType.FireRate,
            StatType.Accuracy,
            StatType.Speed,
            StatType.Range,
            StatType.Reload
        };

        public void Show(WeaponStats baseStats, Dictionary<StatType, float> deltas, bool resetScroll = false)
        {
            if (contentRoot == null)
            {
                Debug.LogError("[StatsPanel] Missing wiring: contentRoot is null.");
                return;
            }
            float previousPos = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1f;
            CacheItems();
            if (_items.Count == 0)
            {
                Debug.LogError("[StatsPanel] No StatItem children under contentRoot.");
                return;
            }

            _rowsBuffer.Clear();
            for (int i = 0; i < _order.Length; i++)
            {
                var stat = _order[i];
                float delta = deltas != null && deltas.TryGetValue(stat, out var d) ? d : 0f;
                float finalVal = baseStats.GetValue(stat);
                _rowsBuffer.Add(new Row { stat = stat, finalVal = finalVal, delta = delta, orderIndex = i });
            }

            _rowsBuffer.Sort((a, b) =>
            {
                int bucketA = a.delta > 0 ? 0 : (Mathf.Approximately(a.delta, 0f) ? 1 : 2);
                int bucketB = b.delta > 0 ? 0 : (Mathf.Approximately(b.delta, 0f) ? 1 : 2);
                if (bucketA != bucketB) return bucketA.CompareTo(bucketB);
                return a.orderIndex.CompareTo(b.orderIndex);
            });

            // Sahnedeki mevcut sira neyse ona göre iterate et (LayoutGroup ile uyumlu)
            _itemOrderBuffer.Clear();
            _itemOrderBuffer.AddRange(_items);
            _itemOrderBuffer.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

            int count = _itemOrderBuffer.Count;
            for (int i = 0; i < count; i++)
            {
                bool active = i < _rowsBuffer.Count;
                _itemOrderBuffer[i].gameObject.SetActive(active);
                if (!active) continue;

                var row = _rowsBuffer[i];
                var iconSprite = GetIcon(row.stat);
                _itemOrderBuffer[i].Bind(GetLabel(row.stat), row.finalVal, row.delta, iconSprite);

                // Renkleri resetle ve pozitifse yeşil uygula
                RestoreDefaultColors(_itemOrderBuffer[i]);
                if (row.delta > 0f)
                {
                    ApplyPositiveColors(_itemOrderBuffer[i]);
                    SetDeltaTextColor(_itemOrderBuffer[i], new Color32(0x82, 0xF5, 0x96, 0xFF)); // #82F596
                }
                else if (row.delta < 0f)
                {
                    ApplyNegativeColors(_itemOrderBuffer[i]);
                    SetDeltaTextColor(_itemOrderBuffer[i], new Color32(0xE8, 0x7A, 0x7C, 0xFF)); // #E87A7C
                }

                // Siralamayi gerçekte görünür yapmak için transform sirala
                _itemOrderBuffer[i].transform.SetSiblingIndex(i);
            }

            if (resetScroll)
            {
                ResetScroll();
            }
            else if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(previousPos);
            }

            ForceLayoutUpdate();
        }

        private void CacheItems()
        {
            if (_items.Count > 0) return;
            if (contentRoot == null) return;

            // Template varsa devre disi birak (StatItemTemplate isimli)
            var template = contentRoot.Find("StatItemTemplate");
            if (template != null) template.gameObject.SetActive(false);

            var found = contentRoot.GetComponentsInChildren<StatItem>(true)
                .Where(it => it != null && !string.Equals(it.name, "StatItemTemplate"))
                .OrderBy(it => it.transform.GetSiblingIndex())
                .ToList();
            _items.AddRange(found);

            foreach (var it in _items)
            {
                if (it != null)
                {
                    if (!_defaultBg.ContainsKey(it))
                        _defaultBg[it] = it.background != null ? it.background.color : Color.white;

                    var shell = GetIconShell(it);
                    if (shell != null && !_defaultIconShell.ContainsKey(it))
                        _defaultIconShell[it] = shell.color;

                    var delta = it.delta != null ? it.delta
                        : it.transform.Find("DeltaItem/Delta")?.GetComponent<Text>()
                        ?? it.transform.Find("Deltaitem/Delta")?.GetComponent<Text>()
                        ?? it.transform.Find("Delta/Delta")?.GetComponent<Text>();
                    if (delta != null && !_defaultDelta.ContainsKey(it))
                        _defaultDelta[it] = delta.color;
                }
            }
        }

        private void ForceLayoutUpdate()
        {
            if (contentRoot is RectTransform rt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Canvas.ForceUpdateCanvases();
        }

        private void ResetScroll()
        {
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private string GetLabel(StatType stat)
        {
            return stat switch
            {
                StatType.FireRate => "FIRE RATE",
                StatType.Reload => "RELOAD",
                _ => stat.ToString().ToUpperInvariant()
            };
        }

        private Sprite GetIcon(StatType stat)
        {
            return stat switch
            {
                StatType.Power => powerIcon,
                StatType.Damage => damageIcon,
                StatType.FireRate => fireRateIcon,
                StatType.Accuracy => accuracyIcon,
                StatType.Speed => speedIcon,
                StatType.Range => rangeIcon,
                StatType.Reload => reloadIcon,
                _ => null
            };
        }

        private Image GetIconShell(StatItem item)
        {
            return item.transform.Find("IconItem")?.GetComponent<Image>()
                   ?? item.transform.Find("Iconitem")?.GetComponent<Image>();
        }

        private void RestoreDefaultColors(StatItem item)
        {
            if (item == null) return;
            if (item.background != null && _defaultBg.TryGetValue(item, out var bgCol))
                item.background.color = bgCol;

            var shell = GetIconShell(item);
            if (shell != null && _defaultIconShell.TryGetValue(item, out var shellCol))
                shell.color = shellCol;
        }

        private void ApplyPositiveColors(StatItem item)
        {
            if (item == null) return;
            Color pos = new Color32(0x26, 0x95, 0x78, 0xFF); // #269578

            if (item.background != null) item.background.color = pos;
            var shell = GetIconShell(item);
            if (shell != null) shell.color = pos;

            // Arka planın alfa'sını 50/255'e çekmek istenirse (talep: alpha 50)
            if (item.background != null)
            {
                var c = item.background.color;
                item.background.color = new Color(c.r, c.g, c.b, 50f / 255f);
            }
        }

        private void ApplyNegativeColors(StatItem item)
        {
            if (item == null) return;
            Color neg = new Color32(0xA9, 0x32, 0x32, 0xFF); // #A93232

            if (item.background != null) item.background.color = neg;
            var shell = GetIconShell(item);
            if (shell != null) shell.color = neg;

            // Alfa 50/255
            if (item.background != null)
            {
                var c = item.background.color;
                item.background.color = new Color(c.r, c.g, c.b, 50f / 255f);
            }
        }

        private void SetDeltaTextColor(StatItem item, Color color)
        {
            if (item == null) return;
            var deltaText = item.delta != null ? item.delta
                : item.transform.Find("DeltaItem/Delta")?.GetComponent<Text>()
                ?? item.transform.Find("Deltaitem/Delta")?.GetComponent<Text>()
                ?? item.transform.Find("Delta/Delta")?.GetComponent<Text>();
            if (deltaText != null) deltaText.color = color;
        }
    }
}
