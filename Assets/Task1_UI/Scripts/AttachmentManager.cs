using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoGames.UI
{
    /// <summary>
    /// Central controller wiring categories, attachments, stats, and equip logic.
    /// </summary>
    public class AttachmentManager : MonoBehaviour
    {
        [Header("Data")]
        public AttachmentDatabase database;
        public WeaponStats baseStats = new WeaponStats();

        [Header("UI")]
        public Transform categoryButtonsContainer;
        public CategoryButton[] categoryButtons;
        public Transform attachmentItemsContainer;
        public AttachmentItemButton attachmentItemPrefab;
        public StatsPanel statsPanel;
        public Button equipButton;
        public Text equipButtonLabel;
        public Text equippedStateLabel;
        [Header("Bottom Bar")]
        public Button tryButton;
        public Button bottomEquipButton;
        public Button instantTierButton;
        public Button increaseTierButton;
        public RectTransform attachmentStrip;
        public RectTransform bottomBar;
        [Header("Top Bar")]
        public Button topBackButton;
        public Text topLabel;
        [Header("Categories Header")]
        public GameObject attachmentsHeader;
        [Header("Fit Settings")]
        [Tooltip("Padding multiplier used when fitting the full loadout in the inventory view.")]
        public float inventoryFitPadding = 2.30f;
        public float autoFitPadding = 2.6f;
        [Header("Camera/Holder Poses")]
        public Transform weaponHolder;
        public Vector3 inventoryCameraPos = new Vector3(-1.88f, 0f, 0.05f);
        public Vector3 inventoryCameraEuler = new Vector3(0f, 90f, 0f);
        public Vector3 attachmentCameraPos = new Vector3(-0.931f, 0.174f, 0.058f);
        public Vector3 attachmentCameraEuler = new Vector3(-0.077f, 94.201f, 0.235f);
        public float inventoryHolderYaw = -40f;
        public float attachmentHolderYaw = -30f;

        [Header("Display")]
        public WeaponDisplayController weaponDisplay;

        private readonly Dictionary<AttachmentCategory, AttachmentData> _equipped = new();
        private AttachmentCategory _currentCategory = AttachmentCategory.Sight;
        private AttachmentData _selectedAttachment;
        private readonly List<AttachmentItemButton> _spawnedItems = new();
        private readonly Color[] _framePalette =
        {
            new Color32(0x9A, 0xC8, 0xCA, 0xFF),
            new Color32(0x26, 0xE0, 0x80, 0xFF),
            new Color32(0x1C, 0xC4, 0xF8, 0xFF)
        };
        private bool _hasSelectedCategory;
        private bool _inInventory = true;
        private const bool DebugStats = false;
        private readonly Dictionary<StatType, float> _scratchDelta = new();
        private readonly Dictionary<StatType, float> _scratchRecomputed = new();

        private void Start()
        {
            // Base stats defaults
            baseStats.power = 56f;
            baseStats.damage = 82f;
            baseStats.fireRate = 800f;
            baseStats.accuracy = 80f;
            baseStats.speed = 96f;
            baseStats.range = 24.2f;
            baseStats.reload = 2.0f;

            if (database == null)
            {
                database = GetComponent<AttachmentDatabase>() ?? FindObjectOfType<AttachmentDatabase>();
                if (database == null)
                {
                    Debug.LogError("AttachmentManager: Database reference missing.");
                    return;
                }
            }

            database.Initialize();
            SeedDefaults();
            WireCategoryButtons();
            PushEquippedToWeapon();
            AutoWireStatsPanel();

            // Inventory-style default: hide strip until a category is picked
            if (attachmentItemsContainer != null)
                attachmentItemsContainer.gameObject.SetActive(false);
            if (attachmentStrip != null)
                attachmentStrip.gameObject.SetActive(false);
            if (bottomBar != null)
                bottomBar.gameObject.SetActive(true);

            WireBottomBar();

            // Show base stats in inventory view (no preview deltas)
            var equippedStats = BuildEquippedOnly();
            statsPanel?.Show(equippedStats.finalStats, null, true);
            SetTopLabel("INVENTORY");

            ApplyInventoryPose();
            FitInventoryView(delayed: true);

            if (topBackButton != null)
                topBackButton.onClick.AddListener(OnTopBackClicked);
            if (equipButton != null)
                equipButton.onClick.AddListener(OnEquipClicked);
        }

        private void SeedDefaults()
        {
            foreach (AttachmentCategory cat in System.Enum.GetValues(typeof(AttachmentCategory)))
            {
                var def = database.GetDefault(cat);
                if (def != null)
                {
                    _equipped[cat] = def;
                }
            }
        }

        private void PushEquippedToWeapon()
        {
            if (weaponDisplay == null) return;
            foreach (var kvp in _equipped)
            {
                weaponDisplay.EquipAttachment(kvp.Value);
            }
        }

        private void AutoWireStatsPanel()
        {
            if (statsPanel == null)
            {
                statsPanel = FindObjectOfType<StatsPanel>(true);
                if (statsPanel != null && DebugStats)
                {
                    Debug.Log("[AttachmentManager] Auto-wired StatsPanel from scene.");
                }
            }

            if (statsPanel != null)
            {
                if (statsPanel.contentRoot == null)
                {
                    var content = statsPanel.transform.Find("Viewport/Content");
                    if (content != null) statsPanel.contentRoot = content;
                }
                if (statsPanel.scrollRect == null)
                {
                    statsPanel.scrollRect = statsPanel.GetComponent<ScrollRect>();
                }
            }
        }

        private void WireCategoryButtons()
        {
            if (categoryButtons == null || categoryButtons.Length == 0)
            {
                categoryButtons = categoryButtonsContainer.GetComponentsInChildren<CategoryButton>(true);
            }

            foreach (var button in categoryButtons)
            {
                button.onClicked = OnCategoryClicked;
                button.SetLabel(button.category.ToString().ToUpperInvariant());
                button.SetSelected(false);

                var equipped = GetEquipped(button.category) ?? database.GetDefault(button.category);
                button.SetEquippedSprite(database.GetIcon(equipped?.iconName));
            }
        }

        private void OnCategoryClicked(AttachmentCategory category)
        {
            if (!_hasSelectedCategory)
            {
                _hasSelectedCategory = true;
                HideBottomBar();
                HideHeaderAndExpandButtons();
                SetTopLabel("ATTACHMENTS");
                _inInventory = false;
            }

            ApplyAttachmentPose();
            SelectCategory(category);
            AutoSelectItem();
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(true);
            }
        }

        private void SelectCategory(AttachmentCategory category)
        {
            weaponDisplay?.ShowEquippedOnly(_currentCategory);

            _currentCategory = category;
            UpdateCategorySelectionVisuals();
            BuildAttachmentList();
            AutoSelectItem();
            if (attachmentItemsContainer != null && !attachmentItemsContainer.gameObject.activeSelf)
            {
                attachmentItemsContainer.gameObject.SetActive(true);
            }
            if (attachmentStrip != null)
            {
                attachmentStrip.gameObject.SetActive(true);
            }
            weaponDisplay?.FocusCategory(category);
        }

        private void WireBottomBar()
        {
            if (tryButton != null)
            {
                tryButton.onClick.AddListener(ShowAttachmentStrip);
            }
            if (bottomEquipButton != null)
            {
                bottomEquipButton.onClick.AddListener(OnEquipClicked);
                bottomEquipButton.onClick.AddListener(ShowAttachmentStrip);
            }
            if (instantTierButton != null)
            {
                instantTierButton.onClick.AddListener(ShowAttachmentStrip);
            }
            if (increaseTierButton != null)
            {
                increaseTierButton.onClick.AddListener(ShowAttachmentStrip);
            }
        }

        public void ShowAttachmentStrip()
        {
            if (attachmentItemsContainer != null)
            {
                attachmentItemsContainer.gameObject.SetActive(true);
            }
            if (attachmentStrip != null)
            {
                attachmentStrip.gameObject.SetActive(true);
            }
            HideBottomBar();
        }

        private void HideBottomBar()
        {
            if (bottomBar != null)
            {
                bottomBar.gameObject.SetActive(false);
            }
        }

        private void SetTopLabel(string text)
        {
            if (topLabel != null)
            {
                topLabel.text = text;
            }
        }

        private void HideHeaderAndExpandButtons()
        {
            if (attachmentsHeader != null && attachmentsHeader.activeSelf)
            {
                attachmentsHeader.SetActive(false);
            }
        }

        private void OnTopBackClicked()
        {
            if (_inInventory)
            {
                return;
            }

            _inInventory = true;
            _hasSelectedCategory = false;

            if (attachmentItemsContainer != null)
            {
                attachmentItemsContainer.gameObject.SetActive(false);
            }
            if (attachmentStrip != null)
            {
                attachmentStrip.gameObject.SetActive(false);
            }
            if (bottomBar != null)
            {
                bottomBar.gameObject.SetActive(true);
            }

            ApplyInventoryPose();
            weaponDisplay?.ClearFocusCenter();
            FitInventoryView(delayed: true);

            foreach (var btn in categoryButtons)
            {
                btn.SetSelected(false);
            }

            ShowHeaderAndResetButtons();
            SetTopLabel("INVENTORY");

            var equippedStats = BuildEquippedOnly();
            statsPanel?.Show(equippedStats.finalStats, null, true);
        }

        private void ShowHeaderAndResetButtons()
        {
            if (attachmentsHeader != null)
            {
                attachmentsHeader.SetActive(true);
            }
        }

        private void UpdateCategorySelectionVisuals()
        {
            foreach (var button in categoryButtons)
            {
                button.SetSelected(button.category == _currentCategory);
            }
        }

        private void ApplyInventoryPose()
        {
            weaponDisplay?.SetCameraPose(inventoryCameraPos, inventoryCameraEuler);
            if (weaponHolder != null)
            {
                weaponHolder.localRotation = Quaternion.Euler(0f, inventoryHolderYaw, 0f);
            }
        }

        private void ApplyAttachmentPose()
        {
            weaponDisplay?.SetCameraPose(attachmentCameraPos, attachmentCameraEuler);
            if (weaponHolder != null)
            {
                weaponHolder.localRotation = Quaternion.Euler(0f, attachmentHolderYaw, 0f);
            }
        }

        private void BuildAttachmentList()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _spawnedItems.Clear();

            var list = database.GetByCategory(_currentCategory);
            for (int i = 0; i < list.Count; i++)
            {
                var data = list[i];
                var instance = Instantiate(attachmentItemPrefab, attachmentItemsContainer);
                instance.gameObject.SetActive(true);
                var sprite = database.GetIcon(data.iconName);
                instance.Bind(data, sprite);
                instance.onClicked = OnAttachmentItemClicked;

                if (instance.frame != null && _framePalette.Length > 0)
                {
                    instance.frame.color = _framePalette[i % _framePalette.Length];
                }

                _spawnedItems.Add(instance);

                var equippedData = GetEquipped(_currentCategory);
                instance.SetEquipped(equippedData != null && equippedData.id == data.id);
            }
        }

        private void AutoSelectItem()
        {
            var equipped = GetEquipped(_currentCategory);
            var target = equipped ?? database.GetDefault(_currentCategory);
            SelectAttachment(target, preview: false);
        }

        private void OnAttachmentItemClicked(AttachmentItemButton item)
        {
            if (item == null || item.Data == null) return;
            SelectAttachment(item.Data, preview: true);
            var equipped = GetEquipped(_currentCategory);
            bool isEquipped = equipped != null && item.Data.id == equipped.id;
            if (equipButton != null && equipButtonLabel != null)
            {
                equipButton.interactable = !isEquipped;
                equipButtonLabel.text = isEquipped ? "EQUIPPED" : "EQUIP";
            }
        }

        private void SelectAttachment(AttachmentData data, bool preview)
        {
            _selectedAttachment = data;

            foreach (var item in _spawnedItems)
            {
                var selected = item != null && item.Data != null && data != null && item.Data.id == data.id;
                item.SetSelected(selected);
                var equipped = GetEquipped(_currentCategory);
                item.SetEquipped(equipped != null && item.Data != null && equipped.id == item.Data.id);
            }

            if (preview && data != null)
            {
                weaponDisplay?.PreviewAttachment(data);
            }
            else
            {
                weaponDisplay?.ShowEquippedOnly(_currentCategory);
            }

            UpdateEquipButton();
            UpdateStatsPanel();
            weaponDisplay?.FocusCategory(_currentCategory);
            FitSelectionCamera(_currentCategory, data);
        }

        private void OnEquipClicked()
        {
            if (_selectedAttachment == null) return;
            _equipped[_selectedAttachment.category] = _selectedAttachment;
            weaponDisplay?.EquipAttachment(_selectedAttachment);
            UpdateCategoryIcons(_selectedAttachment.category);
            weaponDisplay?.ShowEquippedOnly(_selectedAttachment.category);
            UpdateEquipButton();
            UpdateStatsPanel();
        }

        private void UpdateEquipButton()
        {
            if (equipButton == null || equipButtonLabel == null) return;
            var equipped = GetEquipped(_currentCategory);
            bool isEquipped = equipped != null && _selectedAttachment != null && equipped.id == _selectedAttachment.id;

            equipButton.interactable = !isEquipped;
            equipButtonLabel.text = isEquipped ? "EQUIPPED" : "EQUIP";

            if (equippedStateLabel != null)
            {
                equippedStateLabel.text = string.Empty;
                equippedStateLabel.enabled = false;
            }
        }

        private void UpdateCategoryIcons(AttachmentCategory category)
        {
            var equipped = GetEquipped(category);
            var sprite = database.GetIcon(equipped?.iconName);
            foreach (var btn in categoryButtons)
            {
                if (btn.category == category)
                {
                    btn.SetEquippedSprite(sprite);
                }
            }
        }

        private void UpdateStatsPanel()
        {
            if (statsPanel == null)
            {
                Debug.LogError("[AttachmentManager] StatsPanel reference missing.");
                return;
            }

            var statsWithSelection = BuildStatsWithSelection();
            var equippedStats = BuildEquippedOnly();

            if (DebugStats && Application.isPlaying)
            {
                var selectedId = _selectedAttachment != null ? _selectedAttachment.id : "none";
                Debug.Log($"[Stats] selected={selectedId} | final: P{statsWithSelection.finalStats.power} D{statsWithSelection.finalStats.damage} FR{statsWithSelection.finalStats.fireRate} ACC{statsWithSelection.finalStats.accuracy} SPD{statsWithSelection.finalStats.speed} RNG{statsWithSelection.finalStats.range} RLD{statsWithSelection.finalStats.reload}");
                foreach (var kvp in statsWithSelection.deltas)
                {
                    Debug.Log($"[Stats] delta {kvp.Key}: {kvp.Value}");
                }
                Debug.Log($"[Stats] panel target: {statsPanel.gameObject.name} (content={statsPanel.contentRoot})");
            }

            if (_inInventory)
            {
                // Inventory view: show totals of all equipped attachments (no preview)
                statsPanel.Show(equippedStats.finalStats, equippedStats.deltas);
            }
            else
            {
                // Attachments view: base + preview deltas
                statsPanel.Show(baseStats, statsWithSelection.deltas);
            }
        }

        private void FitSelectionCamera(AttachmentCategory category, AttachmentData data)
        {
            if (weaponDisplay == null) return;

            var nodeName = GetNodeNameForData(category, data);
            if (!string.IsNullOrEmpty(nodeName))
            {
                weaponDisplay.FitToNode(nodeName, autoFitPadding, lookAt: false);
            }
        }

        private string GetNodeNameForData(AttachmentCategory category, AttachmentData data)
        {
            if (data == null) return null;

            string icon = data.iconName ?? string.Empty;
            string id = data.id ?? string.Empty;

            if (category == AttachmentCategory.Sight)
            {
                if (icon.Contains("att_00_sight_0") || id.Contains("sight_default"))
                    return "sk_primary_dash_att_00_sight_0_LOD0";
                if (icon.Contains("att_01_sight_1") || id.Contains("sight_thermal") || id.Contains("thermal"))
                    return "sk_primary_dash_att_01_sight_1_LOD0";
                if (icon.Contains("att_02_sight_2") || id.Contains("sight_night") || id.Contains("night"))
                    return "sk_primary_dash_att_02_sight_2_LOD0";
            }

            if (category == AttachmentCategory.Mag)
            {
                if (icon.Contains("att_03_mag_0") || id.Contains("mag_default"))
                    return "sk_primary_dash_att_03_mag_0_LOD0";
                if (icon.Contains("att_04_mag_1") || id.Contains("mag_fast") || id.Contains("quick"))
                    return "sk_primary_dash_att_04_mag_1_LOD0";
                if (icon.Contains("att_05_mag_2") || id.Contains("mag_bulletstorm") || id.Contains("bulletstorm"))
                    return "sk_primary_dash_att_05_mag_2_LOD0";
                if (icon.Contains("att_06_mag_3") || id.Contains("mag_quickload") || id.Contains("quickload"))
                    return "sk_primary_dash_att_06_mag_3_LOD0";
                if (icon.Contains("att_07_mag_4") || id.Contains("mag_heavy") || id.Contains("heavy"))
                    return "sk_primary_dash_att_07_mag_4_LOD0";
            }

            if (category == AttachmentCategory.Barrel)
            {
                if (icon.Contains("att_08_barrel_0") || id.Contains("barrel_default"))
                    return "sk_primary_dash_att_08_barrel_0_LOD0";
                if (icon.Contains("att_09_barrel_1") || id.Contains("barrel_cool") || id.Contains("cool"))
                    return "sk_primary_dash_att_09_barrel_1_LOD0";
                if (icon.Contains("att_10_barrel_2") || id.Contains("barrel_peace") || id.Contains("peace"))
                    return "sk_primary_dash_att_10_barrel_2_LOD0";
            }

            if (category == AttachmentCategory.Stock)
            {
                if (icon.Contains("att_14_stock_0") || id.Contains("stock_default"))
                    return "sk_primary_dash_att_14_stock_0_LOD0";
                if (icon.Contains("att_15_stock_1") || id.Contains("stock_light") || id.Contains("stock_tactical"))
                    return "sk_primary_dash_att_15_stock_1_LOD0";
            }

            if (category == AttachmentCategory.Tactical)
            {
                if (icon.Contains("att_11_tactical_0") || id.Contains("tac_default"))
                    return "sk_primary_dash_att_11_tactical_0_LOD0";
                if (icon.Contains("att_12_tactical_1") || id.Contains("tac_grip") || id.Contains("grip"))
                    return "sk_primary_dash_att_12_tactical_1_LOD0";
                if (icon.Contains("att_13_tactical_2") || id.Contains("tac_poison") || id.Contains("poison"))
                    return "sk_primary_dash_att_13_tactical_2_LOD0";
            }

            return null;
        }

        private (WeaponStats finalStats, Dictionary<StatType, float> deltas) BuildStatsWithSelection()
        {
            var finalStats = baseStats.Clone();
            _scratchDelta.Clear();

            foreach (var kvp in _equipped)
            {
                ApplyModifiers(finalStats, kvp.Value?.statModifiers);
                AddModifiers(_scratchDelta, kvp.Value?.statModifiers);
            }

            if (_selectedAttachment != null)
            {
                if (_equipped.TryGetValue(_selectedAttachment.category, out var eq) && eq != null)
                {
                    ApplyModifiers(finalStats, eq.statModifiers, remove: true);
                    RemoveCategoryModifiers(_scratchDelta, _selectedAttachment.category);
                }
                ApplyModifiers(finalStats, _selectedAttachment.statModifiers);
                AddModifiers(_scratchDelta, _selectedAttachment.statModifiers);
            }

            _scratchRecomputed.Clear();
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                var baseVal = baseStats.GetValue(stat);
                var finalVal = finalStats.GetValue(stat);
                _scratchRecomputed[stat] = finalVal - baseVal;
            }

            return (finalStats, _scratchRecomputed);
        }

        private (WeaponStats finalStats, Dictionary<StatType, float> deltas) BuildEquippedOnly()
        {
            var finalStats = baseStats.Clone();
            _scratchDelta.Clear();

            foreach (var kvp in _equipped)
            {
                ApplyModifiers(finalStats, kvp.Value?.statModifiers);
                AddModifiers(_scratchDelta, kvp.Value?.statModifiers);
            }

            _scratchRecomputed.Clear();
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                var baseVal = baseStats.GetValue(stat);
                var finalVal = finalStats.GetValue(stat);
                _scratchRecomputed[stat] = finalVal - baseVal;
            }

            return (finalStats, _scratchRecomputed);
        }

        private void FitInventoryView(bool delayed = false)
        {
            if (weaponDisplay == null) return;
            if (delayed)
            {
                StartCoroutine(FitNextFrame());
            }
            else
            {
                FitNow();
            }
        }

        private IEnumerator FitNextFrame()
        {
            yield return null; // wait one frame so model/render is ready
            FitNow();
        }

        private void FitNow()
        {
            if (weaponDisplay == null) return;
            weaponDisplay.FitToActiveRenderers(inventoryFitPadding, lookAt: true);
            weaponDisplay.SetFocusCenterToModelBounds();
        }

        private void AddModifiers(Dictionary<StatType, float> dict, IEnumerable<StatModifier> mods)
        {
            if (mods == null) return;
            foreach (var mod in mods)
            {
                if (!dict.ContainsKey(mod.stat))
                {
                    dict[mod.stat] = 0f;
                }
                dict[mod.stat] += mod.value;
            }
        }

        private void RemoveCategoryModifiers(Dictionary<StatType, float> dict, AttachmentCategory category)
        {
            if (_equipped.TryGetValue(category, out var equipped))
            {
                if (equipped != null && equipped.statModifiers != null)
                {
                    foreach (var mod in equipped.statModifiers)
                    {
                        if (dict.ContainsKey(mod.stat))
                        {
                            dict[mod.stat] -= mod.value;
                        }
                    }
                }
            }
        }

        private void ApplyModifiers(WeaponStats stats, IEnumerable<StatModifier> mods, bool remove = false)
        {
            if (stats == null || mods == null) return;
            foreach (var mod in mods)
            {
                if (mod == null) continue;
                float val = remove ? -mod.value : mod.value;
                stats.Apply(new StatModifier { stat = mod.stat, value = val });
            }
        }

        private AttachmentData GetEquipped(AttachmentCategory category)
        {
            return _equipped.TryGetValue(category, out var data) ? data : null;
        }

        // Kept for editor tool compatibility; no-op since layout capture was removed.
        public void CaptureLayoutFromScene()
        {
        }
    }
}
