using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VertigoGames.UI
{
    /// <summary>
    /// Weapon preview controller: spawns the weapon, handles drag rotation, and focuses camera on per-category anchors.
    /// Supports explicit node names from the model and fallback anchors created by the setup tool.
    /// </summary>
    [ExecuteAlways]
    public class WeaponDisplayController : MonoBehaviour
    {
        [Header("Weapon")]
        public GameObject weaponPrefab;
        public Transform weaponAnchor;
        public bool autoRotate = true;
        public float rotateSpeed = 10f;
        [Header("Custom Transform Override")]
        public bool useCustomTransform = false;
        public Vector3 customPosition = Vector3.zero;
        public Vector3 customEuler = Vector3.zero;
        public Vector3 customScale = Vector3.one;
        [Header("Pivot")]
        public bool centerToBounds = false;
        [Header("User Rotation")]
        public bool enableDragRotation = true;
        public float dragSensitivity = 0.2f;
        public bool blockDragOverUI = true;
        [Header("Preview Camera")]
        public Camera previewCamera;
        public float defaultFocusDistance = 2.5f;
        public float minFocusDistance = 1f;
        public float focusPadding = 1.8f;
        public Vector3 focusDirection = new Vector3(0f, 0f, -1f);
        public Vector3 cameraOffset = Vector3.zero;
        public bool lockCameraTransform = false;
        public Vector3 manualCameraPosition = Vector3.zero;
        public Vector3 manualCameraEuler = Vector3.zero;
        public bool panOnly = false;
        public bool autoAlignAnchorsToModel = false;
        public bool spawnInEditMode = true;
        public bool keepCameraStaticInEditMode = true;
        [Header("View Settings Asset (optional)")]
        public AttachmentViewSettings viewSettings;
        [Serializable]
        public struct CategoryOverride
        {
            public AttachmentCategory category;
            public Vector3 localOffset;
            public float distance;
        }
        public List<CategoryOverride> categoryOverrides = new();
        [Header("Model Nodes")]
        public string sightNodeName = "sk_primary_dash_att_00_sight_0_LOD0";
        public string magNodeName = "sk_primary_dash_att_03_mag_0_LOD0";
        public string barrelNodeName = "";
        public string stockNodeName = "";
        public string tacticalNodeName = "";

        [Header("Anchors")]
        public Transform sightAnchor;
        public Transform magAnchor;
        public Transform barrelAnchor;
        public Transform stockAnchor;
        public Transform tacticalAnchor;

        private readonly Dictionary<AttachmentCategory, Transform> _anchors = new();
        private readonly Dictionary<AttachmentCategory, float> _focusDistances = new();
        private readonly Dictionary<AttachmentCategory, GameObject> _equippedObjects = new();
        private readonly Dictionary<AttachmentCategory, GameObject> _previewObjects = new();
        private readonly Dictionary<AttachmentCategory, string[]> _preferredNames = new();
        private readonly Dictionary<AttachmentCategory, (string pattern, string nodePrefix)[]> _explicitNodeMap = new();
        private GameObject _weaponInstance;
        private float _yaw;
        private float _pitch;
        private bool _dragging;
        private Vector3 _lastMouse;
        private bool _initialized;
        private Vector3 _focusCenterWorld;
        private bool _hasFocusCenter;
        private readonly List<RaycastResult> _uiRaycastResults = new();

        private void Awake()
        {
            EnsureInitialized();
            ApplyManualCameraPose();
            SpawnWeapon();
            ApplyDefaultModelState();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EnsureInitialized();
                ApplyManualCameraPose();
                SpawnWeapon();
                ApplyDefaultModelState();
            }
        }

        private void OnDisable()
        {
            _dragging = false;
        }

        private void Update()
        {
            if (autoRotate && weaponAnchor != null && Application.isPlaying)
            {
                weaponAnchor.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
            }

            HandleDragRotation();
        }

        private void LateUpdate()
        {
            // In edit mode force a render so the preview RT shows the weapon
            if (!Application.isPlaying && previewCamera != null)
            {
                previewCamera.Render();
            }

            // Safety: if weapon destroyed in edit mode, respawn to keep inventory view visible
            if (!Application.isPlaying && spawnInEditMode && (_weaponInstance == null) && weaponPrefab != null && weaponAnchor != null)
            {
                SpawnWeapon();
                ApplyManualCameraPose();
                ApplyDefaultModelState();
                if (previewCamera != null) previewCamera.Render();
            }
        }

        private void ApplyDefaultModelState()
        {
            if (_weaponInstance == null) return;
            ClearFocusCenter();
            SetCategoryActiveNodes(AttachmentCategory.Sight, "sk_primary_dash_att_00_sight_0");
            SetCategoryActiveNodes(AttachmentCategory.Mag, "sk_primary_dash_att_03_mag_0");
            SetCategoryActiveNodes(AttachmentCategory.Barrel, "sk_primary_dash_att_08_barrel_0");
            SetCategoryActiveNodes(AttachmentCategory.Stock, "sk_primary_dash_att_14_stock_0");
            SetCategoryActiveNodes(AttachmentCategory.Tactical, "sk_primary_dash_att_11_tactical_0");
        }

        private void SetCategoryActiveNodes(AttachmentCategory category, string targetPrefix)
        {
            if (_weaponInstance == null || string.IsNullOrEmpty(targetPrefix)) return;
            var cat = category.ToString().ToLowerInvariant();
            foreach (Transform child in _weaponInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child == _weaponInstance.transform) continue;
                var lname = child.name.ToLowerInvariant();
                if (!lname.Contains("att_") || !lname.Contains(cat)) continue;

                var baseName = lname;
                var lodIdx = baseName.IndexOf("_lod", StringComparison.OrdinalIgnoreCase);
                if (lodIdx >= 0) baseName = baseName.Substring(0, lodIdx);

                bool match = baseName.Contains(targetPrefix.ToLowerInvariant());
                child.gameObject.SetActive(match);
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            CacheAnchorsFromFields();
            SeedPreferredNames();
            CacheFocusDistances();
            ApplySettingsAsset();
            _initialized = true;
        }

        private void ApplyManualCameraPose()
        {
            if (previewCamera == null) return;
            previewCamera.transform.SetPositionAndRotation(manualCameraPosition, Quaternion.Euler(manualCameraEuler));
        }

        public void SetCameraPose(Vector3 position, Vector3 euler)
        {
            manualCameraPosition = position;
            manualCameraEuler = euler;
            ApplyManualCameraPose();
        }

        public void SpawnWeapon()
        {
            if (weaponPrefab == null || weaponAnchor == null) return;
            if (!Application.isPlaying && !spawnInEditMode) return;
            ClearExistingWeaponInstance();
            _weaponInstance = Instantiate(weaponPrefab, weaponAnchor);

            if (useCustomTransform)
            {
                _weaponInstance.transform.localPosition = customPosition;
                _weaponInstance.transform.localRotation = Quaternion.Euler(customEuler);
                _weaponInstance.transform.localScale = customScale;
                _yaw = customEuler.y;
                _pitch = customEuler.x;
            }
            else
            {
                _weaponInstance.transform.localPosition = Vector3.zero;
                _weaponInstance.transform.localRotation = Quaternion.identity;
                _weaponInstance.transform.localScale = Vector3.one;
                _yaw = 0f;
                _pitch = 0f;
            }

            if (centerToBounds)
            {
                var renderers = _weaponInstance.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }
                    _weaponInstance.transform.localPosition -= bounds.center;
                }
            }

            if (autoAlignAnchorsToModel)
            {
                AlignAnchorsToModelChildren();
            }
        }

        private void ClearExistingWeaponInstance()
        {
            if (_weaponInstance != null)
            {
                SafeDestroy(_weaponInstance);
                _weaponInstance = null;
            }

            if (weaponAnchor != null)
            {
                foreach (Transform child in weaponAnchor)
                {
                    if (IsAnchorTransform(child)) continue;
                    SafeDestroy(child.gameObject);
                }
            }
        }

        private bool IsAnchorTransform(Transform t)
        {
            return t == sightAnchor || t == magAnchor || t == barrelAnchor || t == stockAnchor || t == tacticalAnchor;
        }

        private void HandleDragRotation()
        {
            if (!enableDragRotation || _weaponInstance == null) return;

            bool IsPointerOverUI()
            {
                if (!blockDragOverUI) return false;
                if (EventSystem.current == null) return false;
                var ped = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                _uiRaycastResults.Clear();
                EventSystem.current.RaycastAll(ped, _uiRaycastResults);
                foreach (var hit in _uiRaycastResults)
                {
                    if (hit.gameObject != null)
                    {
                        // UI hiyerarşisinde herhangi bir Button var mı diye yukarı yürü
                        var t = hit.gameObject.transform;
                        while (t != null)
                        {
                            if (t.GetComponent<Button>() != null)
                                return true;
                            t = t.parent;
                        }
                    }
                }
                return false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI())
                {
                    _dragging = false;
                    return;
                }
                _dragging = true;
                _lastMouse = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
            }

            if (_dragging)
            {
                var current = Input.mousePosition;
                var delta = current - _lastMouse;
                _lastMouse = current;

                float yawDelta = delta.x * dragSensitivity;

                if (_hasFocusCenter && weaponAnchor != null)
                {
                    weaponAnchor.RotateAround(_focusCenterWorld, Vector3.up, yawDelta);
                }
                else
                {
                    _yaw += yawDelta;
                    bool allowPitch = false;
                    if (allowPitch)
                    {
                        _pitch -= delta.y * dragSensitivity;
                        _pitch = Mathf.Clamp(_pitch, -80f, 80f);
                    }
                    var rot = Quaternion.Euler(_pitch, _yaw, 0f);
                    _weaponInstance.transform.localRotation = rot;
                }
            }
        }

        public void PreviewAttachment(AttachmentData data)
        {
            if (data == null) return;
            SpawnAttachment(data, equipped: false);
            ApplyModelVariant(data);
        }

        public void EquipAttachment(AttachmentData data)
        {
            if (data == null) return;
            ClearPreview(data.category);
            if (_equippedObjects.TryGetValue(data.category, out var existing))
            {
                SafeDestroy(existing);
            }

            var go = SpawnAttachment(data, equipped: true);
            _equippedObjects[data.category] = go;
            ApplyModelVariant(data);
        }

        public void ClearPreview(AttachmentCategory category)
        {
            if (_previewObjects.TryGetValue(category, out var existing))
            {
                SafeDestroy(existing);
                _previewObjects.Remove(category);
            }
        }

        public void ShowEquippedOnly(AttachmentCategory category)
        {
            ClearPreview(category);
        }

        private GameObject SpawnAttachment(AttachmentData data, bool equipped)
        {
            var anchor = GetAnchor(data.category);
            if (anchor == null) return null;

            if (!equipped && _previewObjects.TryGetValue(data.category, out var prev))
            {
                SafeDestroy(prev);
                _previewObjects.Remove(data.category);
            }

            var go = new GameObject($"{data.category}_{data.displayName}_{(equipped ? "Equipped" : "Preview")}");
            go.transform.SetParent(anchor, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            if (equipped)
            {
                _equippedObjects[data.category] = go;
            }
            else
            {
                _previewObjects[data.category] = go;
            }

            return go;
        }

        private void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void CacheFocusDistances()
        {
            _focusDistances.Clear();
            _focusDistances[AttachmentCategory.Sight] = 2.0f;
            _focusDistances[AttachmentCategory.Mag] = 2.0f;
            _focusDistances[AttachmentCategory.Barrel] = 2.2f;
            _focusDistances[AttachmentCategory.Stock] = 2.4f;
            _focusDistances[AttachmentCategory.Tactical] = 2.2f;
        }

        private void ApplySettingsAsset()
        {
            if (viewSettings == null) return;

            manualCameraPosition = viewSettings.cameraPosition;
            manualCameraEuler = viewSettings.cameraEuler;
            focusDirection = viewSettings.focusDirection;
            cameraOffset = viewSettings.cameraOffset;
            focusPadding = viewSettings.focusPadding;

            if (viewSettings.categories != null)
            {
                categoryOverrides.Clear();
                foreach (var c in viewSettings.categories)
                {
                    categoryOverrides.Add(new CategoryOverride
                    {
                        category = c.category,
                        localOffset = c.localOffset,
                        distance = c.distance
                    });
                    _focusDistances[c.category] = c.distance;
                }
            }
        }

        private void CacheAnchorsFromFields()
        {
            _anchors.Clear();
            _anchors[AttachmentCategory.Sight] = sightAnchor;
            _anchors[AttachmentCategory.Mag] = magAnchor;
            _anchors[AttachmentCategory.Barrel] = barrelAnchor;
            _anchors[AttachmentCategory.Stock] = stockAnchor;
            _anchors[AttachmentCategory.Tactical] = tacticalAnchor;
        }

        private Transform GetAnchor(AttachmentCategory category, bool allowModelNodes = false)
        {
            if (_anchors.TryGetValue(category, out var anchor) && anchor != null)
            {
                return anchor;
            }

            if (allowModelNodes && _weaponInstance != null)
            {
                var exact = FindChildByName(_weaponInstance.transform, GetNodeName(category));
                if (exact != null) return exact;

                var best = FindBestChildForCategory(_weaponInstance.transform, category);
                if (best != null) return best;
            }

            return null;
        }

        private string GetNodeName(AttachmentCategory category)
        {
            return category switch
            {
                AttachmentCategory.Sight => string.IsNullOrWhiteSpace(sightNodeName) ? null : sightNodeName,
                AttachmentCategory.Mag => string.IsNullOrWhiteSpace(magNodeName) ? null : magNodeName,
                AttachmentCategory.Barrel => string.IsNullOrWhiteSpace(barrelNodeName) ? null : barrelNodeName,
                AttachmentCategory.Stock => string.IsNullOrWhiteSpace(stockNodeName) ? null : stockNodeName,
                AttachmentCategory.Tactical => string.IsNullOrWhiteSpace(tacticalNodeName) ? null : tacticalNodeName,
                _ => null
            };
        }

        public void FocusCategory(AttachmentCategory category)
        {
            if (previewCamera == null) return;
            if (!Application.isPlaying && keepCameraStaticInEditMode) return;
            if (lockCameraTransform)
            {
                ApplyManualCameraPose();
                return;
            }

            var target = GetTargetPosition(category);

            if (panOnly)
            {
                previewCamera.transform.position = manualCameraPosition;
                previewCamera.transform.rotation = Quaternion.Euler(manualCameraEuler);
                return;
            }

            var radius = GetTargetRadius();
            var dist = _focusDistances.TryGetValue(category, out var d) ? d : defaultFocusDistance;
            if (dist <= 0f)
            {
                dist = Mathf.Max(radius * focusPadding, minFocusDistance);
            }
            else
            {
                dist = Mathf.Max(dist, minFocusDistance);
            }

            var dirLocal = focusDirection.sqrMagnitude > 0.0001f ? focusDirection.normalized : Vector3.back;
            var dirWorld = weaponAnchor != null ? weaponAnchor.TransformDirection(dirLocal) : dirLocal;
            var offsetWorld = weaponAnchor != null ? weaponAnchor.TransformDirection(cameraOffset) : cameraOffset;

            previewCamera.transform.position = target - dirWorld * dist + offsetWorld;
            previewCamera.transform.LookAt(target);
        }

        private void ReparentAnchorsToWeapon()
        {
            // no-op: keep anchors on the holder
        }

        private void AlignAnchorsToModelChildren()
        {
            if (_weaponInstance == null) return;

            foreach (AttachmentCategory cat in Enum.GetValues(typeof(AttachmentCategory)))
            {
                if (!_anchors.TryGetValue(cat, out var anchor) || anchor == null) continue;
                var target = GetAnchorTargetTransform(cat);
                if (target != null)
                {
                    anchor.position = target.position;
                    anchor.rotation = target.rotation;
                }
            }
        }

        private Transform GetAnchorTargetTransform(AttachmentCategory category)
        {
            if (_weaponInstance == null) return null;
            var explicitNode = FindFirstExplicitNode(category);
            if (explicitNode != null) return explicitNode;
            var exact = FindChildByName(_weaponInstance.transform, GetNodeName(category));
            if (exact != null) return exact;
            return FindBestChildForCategory(_weaponInstance.transform, category);
        }

        private Transform FindFirstExplicitNode(AttachmentCategory category)
        {
            if (_weaponInstance == null) return null;
            if (_explicitNodeMap.TryGetValue(category, out var map))
            {
                foreach (var entry in map)
                {
                    var node = FindChildByName(_weaponInstance.transform, entry.nodePrefix);
                    if (node != null) return node;
                }
            }
            return null;
        }

        private Transform FindBestChildForCategory(Transform root, AttachmentCategory category)
        {
            string key = category.ToString().ToLowerInvariant();
            Transform best = null;
            int bestScore = int.MinValue;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root) continue;

                if (_preferredNames.TryGetValue(category, out var names))
                {
                    foreach (var n in names)
                    {
                        if (string.IsNullOrWhiteSpace(n)) continue;
                        if (string.Equals(child.name, n, StringComparison.OrdinalIgnoreCase) ||
                            child.name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return child;
                        }
                    }
                }

                string name = child.name.ToLowerInvariant();
                int score = 0;

                if (name.Contains(key)) score += 5;
                if (name.Contains("_att")) score += 2;
                if (name.Contains("_lod")) score -= 1;
                if (name.Contains("_ads")) score += 1;

                if (category == AttachmentCategory.Sight && (name.Contains("optic") || name.Contains("scope"))) score += 2;
                if (category == AttachmentCategory.Mag && (name.Contains("magazine") || name.Contains("clip"))) score += 2;
                if (category == AttachmentCategory.Barrel && name.Contains("muzzle")) score += 2;
                if (category == AttachmentCategory.Tactical && (name.Contains("laser") || name.Contains("grip") || name.Contains("rail"))) score += 2;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = child;
                }
            }

            return best;
        }

        private Vector3 GetTargetPosition(AttachmentCategory category)
        {
            var anchor = GetAnchor(category);
            if (anchor != null)
            {
                var offset = GetOffsetForCategory(category);
                return anchor.TransformPoint(offset);
            }

            var bounds = GetRenderBounds();
            if (bounds.HasValue) return bounds.Value.center;

            return weaponAnchor != null ? weaponAnchor.position : transform.position;
        }

        private float GetTargetRadius()
        {
            var bounds = GetRenderBounds();
            if (bounds.HasValue)
            {
                var ext = bounds.Value.extents;
                return Mathf.Max(ext.x, ext.y, ext.z);
            }
            return defaultFocusDistance;
        }

        public void FitToActiveRenderers(float paddingMultiplier = 1.3f, bool lookAt = true)
        {
            if (previewCamera == null) return;
            var bounds = GetRenderBounds();
            if (!bounds.HasValue) return;

            var b = bounds.Value;
            var center = b.center;
            var dist = ComputeCameraDistance(b, paddingMultiplier);
            _focusCenterWorld = center;
            _hasFocusCenter = true;

            // use current camera forward to avoid flipping behind the weapon
            var dirWorld = previewCamera.transform.forward;
            var offsetWorld = weaponAnchor != null ? weaponAnchor.TransformDirection(cameraOffset) : cameraOffset;

            previewCamera.transform.position = center - dirWorld * dist + offsetWorld;
            if (lookAt)
            {
                previewCamera.transform.LookAt(center);
            }
            else
            {
                previewCamera.transform.rotation = Quaternion.Euler(manualCameraEuler);
            }
        }

        public void FitToNode(string nodeName, float paddingMultiplier = 1.3f, bool lookAt = true)
        {
            if (previewCamera == null || string.IsNullOrEmpty(nodeName) || _weaponInstance == null) return;
            var target = FindChildByName(_weaponInstance.transform, nodeName);
            if (target == null) return;
            var bounds = GetRendererBounds(target);
            if (!bounds.HasValue)
            {
                FitToActiveRenderers(paddingMultiplier, lookAt);
                return;
            }

            var b = bounds.Value;
            var center = b.center;
            var dist = ComputeCameraDistance(b, paddingMultiplier);
            _focusCenterWorld = center;
            _hasFocusCenter = true;

            // use current camera forward to avoid flipping behind the weapon
            var dirWorld = previewCamera.transform.forward;
            var offsetWorld = weaponAnchor != null ? weaponAnchor.TransformDirection(cameraOffset) : cameraOffset;

            previewCamera.transform.position = center - dirWorld * dist + offsetWorld;
            if (lookAt)
            {
                previewCamera.transform.LookAt(center);
            }
            else
            {
                previewCamera.transform.rotation = Quaternion.Euler(manualCameraEuler);
            }
        }

        private float ComputeCameraDistance(Bounds b, float paddingMultiplier)
        {
            var ext = b.extents * 2f; // full size
            float vfov = previewCamera.fieldOfView * Mathf.Deg2Rad;
            float hfov = 2f * Mathf.Atan(Mathf.Tan(vfov * 0.5f) * previewCamera.aspect);

            float height = ext.y * paddingMultiplier;
            float width = ext.x * paddingMultiplier;

            float distV = (height * 0.5f) / Mathf.Tan(vfov * 0.5f);
            float distH = (width * 0.5f) / Mathf.Tan(hfov * 0.5f);

            float dist = Mathf.Max(distV, distH, minFocusDistance);
            return dist;
        }

        private Bounds? GetRendererBounds(Transform root)
        {
            if (root == null) return null;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return null;
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        private Bounds? GetRenderBounds()
        {
            if (_weaponInstance == null) return null;
            var renderers = _weaponInstance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return null;
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        public void ClearFocusCenter()
        {
            _hasFocusCenter = false;
        }

        public void SetFocusCenterToModelBounds(float paddingMultiplier = 1f)
        {
            var bounds = GetRenderBounds();
            if (bounds.HasValue)
            {
                _focusCenterWorld = bounds.Value.center;
                _hasFocusCenter = true;
                // Optional: update camera distance if desired (currently we keep camera position)
            }
            else
            {
                ClearFocusCenter();
            }
        }

        public float GetFocusDistance(AttachmentCategory category)
        {
            return _focusDistances.TryGetValue(category, out var d) ? d : defaultFocusDistance;
        }

        public void SetFocusDistance(AttachmentCategory category, float distance)
        {
            _focusDistances[category] = distance;
        }

        public void SetOffset(AttachmentCategory category, Vector3 offset)
        {
            bool found = false;
            for (int i = 0; i < categoryOverrides.Count; i++)
            {
                if (categoryOverrides[i].category == category)
                {
                    var co = categoryOverrides[i];
                    co.localOffset = offset;
                    categoryOverrides[i] = co;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                categoryOverrides.Add(new CategoryOverride { category = category, localOffset = offset, distance = GetFocusDistance(category) });
            }
        }

        public Vector3 GetOffsetForCategory(AttachmentCategory category)
        {
            foreach (var co in categoryOverrides)
            {
                if (co.category == category) return co.localOffset;
            }
            return Vector3.zero;
        }

        private void SeedPreferredNames()
        {
            _preferredNames.Clear();
            _preferredNames[AttachmentCategory.Sight] = new[] { sightNodeName };
            _preferredNames[AttachmentCategory.Mag] = new[] { magNodeName };
            if (!string.IsNullOrEmpty(barrelNodeName)) _preferredNames[AttachmentCategory.Barrel] = new[] { barrelNodeName };
            if (!string.IsNullOrEmpty(stockNodeName)) _preferredNames[AttachmentCategory.Stock] = new[] { stockNodeName };
            if (!string.IsNullOrEmpty(tacticalNodeName)) _preferredNames[AttachmentCategory.Tactical] = new[] { tacticalNodeName };

            // explicit model map: pattern from iconName -> node prefix (without LOD)
            _explicitNodeMap.Clear();
            _explicitNodeMap[AttachmentCategory.Sight] = new (string, string)[]
            {
                ("att_00_sight_0", "sk_primary_dash_att_00_sight_0"),
                ("att_01_sight_1", "sk_primary_dash_att_01_sight_1"),
                ("att_02_sight_2", "sk_primary_dash_att_02_sight_2"),
            };
            _explicitNodeMap[AttachmentCategory.Mag] = new (string, string)[]
            {
                ("att_03_mag_0", "sk_primary_dash_att_03_mag_0"),
                ("att_04_mag_1", "sk_primary_dash_att_04_mag_1"),
                ("att_05_mag_2", "sk_primary_dash_att_05_mag_2"),
                ("att_06_mag_3", "sk_primary_dash_att_06_mag_3"),
                ("att_07_mag_4", "sk_primary_dash_att_07_mag_4"),
            };
            _explicitNodeMap[AttachmentCategory.Barrel] = new (string, string)[]
            {
                ("att_08_barrel_0", "sk_primary_dash_att_08_barrel_0"),
                ("att_09_barrel_1", "sk_primary_dash_att_09_barrel_1"),
                ("att_10_barrel_2", "sk_primary_dash_att_10_barrel_2"),
            };
            _explicitNodeMap[AttachmentCategory.Stock] = new (string, string)[]
            {
                ("att_14_stock_0", "sk_primary_dash_att_14_stock_0"),
                ("att_15_stock_1", "sk_primary_dash_att_15_stock_1"),
            };
            _explicitNodeMap[AttachmentCategory.Tactical] = new (string, string)[]
            {
                ("att_11_tactical_0", "sk_primary_dash_att_11_tactical_0"),
                ("att_12_tactical_1", "sk_primary_dash_att_12_tactical_1"),
                ("att_13_tactical_2", "sk_primary_dash_att_13_tactical_2"),
            };
        }

        private Transform FindChildByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            return null;
        }

        private void ApplyModelVariant(AttachmentData data)
        {
            if (_weaponInstance == null || data == null) return;
            var nodePrefix = ResolveExplicitNode(data);
            var token = ExtractNodeToken(data);
            var cat = data.category.ToString().ToLowerInvariant();
            var tokenLower = string.IsNullOrEmpty(token) ? null : token.ToLowerInvariant();

            foreach (Transform child in _weaponInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child == _weaponInstance.transform) continue;
                var lname = child.name.ToLowerInvariant();

                bool isCategoryPart = lname.Contains("att_") && lname.Contains(cat);
                if (!isCategoryPart) continue;

                var baseName = lname;
                var lodIdx = baseName.IndexOf("_lod", StringComparison.OrdinalIgnoreCase);
                if (lodIdx >= 0) baseName = baseName.Substring(0, lodIdx);

                bool match = false;
                if (!string.IsNullOrEmpty(nodePrefix) && baseName.Contains(nodePrefix.ToLowerInvariant()))
                {
                    match = true;
                }
                else if (tokenLower != null && baseName.Contains(tokenLower))
                {
                    match = true;
                }

                child.gameObject.SetActive(match);
            }
        }

        private string ResolveExplicitNode(AttachmentData data)
        {
            if (data == null) return null;
            if (_explicitNodeMap.TryGetValue(data.category, out var map))
            {
                foreach (var (pattern, node) in map)
                {
                    if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(node)) continue;
                    if (!string.IsNullOrEmpty(data.iconName) &&
                        data.iconName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return node;
                    }
                    if (!string.IsNullOrEmpty(data.id) &&
                        data.id.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return node;
                    }
                }
            }
            return null;
        }

        private string ExtractNodeToken(AttachmentData data)
        {
            if (data == null || string.IsNullOrEmpty(data.iconName)) return null;
            var name = data.iconName;
            var idx = name.IndexOf("att_", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var sub = name.Substring(idx);
            // strip extension if any
            var dot = sub.IndexOf('.');
            if (dot >= 0) sub = sub.Substring(0, dot);
            return sub;
        }
    }
}
