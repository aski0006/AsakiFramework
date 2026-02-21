using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Context
{
    [CustomEditor(typeof(AsakiSceneContext))]
    public class AsakiSceneContextEditor : UnityEditor.Editor
    {
        #region Constants & Colors

        private static readonly Color BrandColor = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color HeaderBgColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        private static readonly Color CardBgColor = new Color(0.22f, 0.22f, 0.22f, 0.9f);
        private static readonly Color CardBorderColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        private static readonly Color SectionHeaderColor = new Color(0.4f, 0.7f, 0.9f);
        private static readonly Color PrefabHeaderColor = new Color(0.3f, 0.65f, 0.35f);
        private static readonly Color RuntimeColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color WarningColor = new Color(0.9f, 0.5f, 0.2f);
        private static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.4f);

        private const float HeaderHeight = 42f;
        private const float CardPadding = 8f;
        private const float CardSpacing = 10f;
        private const float CardBorderRadius = 6f;
        private const int ItemsPerPage = 10;

        #endregion

        #region Serialized Properties

        private SerializedProperty _pureCSharpServicesProp;
        private SerializedProperty _servicePrefabsProp;
        private SerializedProperty _instanceParentProp;

        #endregion

        #region State

        private bool _foldoutPrefabs = true;
        private bool _foldoutPureServices = true;
        private bool _foldoutRuntime = true;
        private bool _foldoutServiceList = true;

        private Texture2D _cardBgTexture;
        private Texture2D _headerBgTexture;
        private GUIStyle _headerLabelStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _foldoutStyle;
        private GUIStyle _miniLabelStyle;
        private GUIStyle _searchFieldStyle;
        private GUIStyle _paginationButtonStyle;

        private List<int> _prefabServiceCounts = new List<int>();

        private string _pureServicesSearchFilter = "";
        private int _pureServicesCurrentPage = 0;
        private List<int> _filteredPureServiceIndices = new List<int>();

        private string _runtimeServicesSearchFilter = "";
        private int _runtimeServicesCurrentPage = 0;
        private List<KeyValuePair<Type, IAsakiService>> _filteredRuntimeServices =
            new List<KeyValuePair<Type, IAsakiService>>();

        private Vector2 _runtimeScrollPosition;

        #endregion

        #region Lifecycle

        private bool _stylesCreated = false;

        private void OnEnable()
        {
            _pureCSharpServicesProp = serializedObject.FindProperty("_pureCSharpServices");
            _servicePrefabsProp = serializedObject.FindProperty("_servicePrefabs");
            _instanceParentProp = serializedObject.FindProperty("_instanceParent");

            _stylesCreated = false;
            _pureServicesSearchFilter = "";
            _pureServicesCurrentPage = 0;
            _runtimeServicesSearchFilter = "";
            _runtimeServicesCurrentPage = 0;
        }

        private void EnsureStylesCreated()
        {
            if (_stylesCreated)
                return;

            _headerBgTexture = MakeTexture(HeaderBgColor);
            _cardBgTexture = MakeTexture(CardBgColor);

            _headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 0, 0),
                normal = { textColor = BrandColor },
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 4, 4),
            };

            _cardStyle = new GUIStyle
            {
                padding = new RectOffset(
                    (int)CardPadding,
                    (int)CardPadding,
                    (int)CardPadding,
                    (int)CardPadding
                ),
                margin = new RectOffset(4, 4, 2, 2),
            };

            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
            };

            _miniLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };

            _searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                margin = new RectOffset(0, 4, 2, 2),
            };

            _paginationButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                fixedWidth = 24,
                fixedHeight = 18,
            };

            _stylesCreated = true;
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        #endregion

        #region Main GUI

        public override void OnInspectorGUI()
        {
            EnsureStylesCreated();

            serializedObject.Update();

            DrawBrandHeader();

            EditorGUILayout.Space(CardSpacing);

            DrawCard(DrawPrefabServicesSection);

            EditorGUILayout.Space(CardSpacing);

            DrawCard(DrawPureCSharpServicesSection);

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(CardSpacing);
                DrawCard(DrawRuntimeSection);
            }

            EditorGUILayout.Space(4);
        }

        #endregion

        #region Card Drawing

        private void DrawCard(Action drawContent)
        {
            Rect cardRect = EditorGUILayout.BeginVertical();

            GUI.DrawTexture(cardRect, _cardBgTexture, ScaleMode.StretchToFill);

            DrawRoundedBorder(cardRect, CardBorderColor, CardBorderRadius);

            GUILayout.Space(CardPadding);

            drawContent();

            GUILayout.Space(CardPadding);

            EditorGUILayout.EndVertical();
        }

        private void DrawRoundedBorder(Rect rect, Color color, float radius)
        {
            Color savedColor = GUI.color;
            GUI.color = color;

            float lineWidth = 1f;

            Rect topLine = new Rect(rect.x, rect.y, rect.width, lineWidth);
            Rect bottomLine = new Rect(rect.x, rect.yMax - lineWidth, rect.width, lineWidth);
            Rect leftLine = new Rect(rect.x, rect.y + radius, lineWidth, rect.height - radius * 2);
            Rect rightLine = new Rect(
                rect.xMax - lineWidth,
                rect.y + radius,
                lineWidth,
                rect.height - radius * 2
            );

            EditorGUI.DrawRect(topLine, color);
            EditorGUI.DrawRect(bottomLine, color);
            EditorGUI.DrawRect(leftLine, color);
            EditorGUI.DrawRect(rightLine, color);

            GUI.color = savedColor;
        }

        #endregion

        #region Brand Header

        private void DrawBrandHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(
                0,
                HeaderHeight,
                GUILayout.ExpandWidth(true)
            );

            GUI.DrawTexture(headerRect, _headerBgTexture, ScaleMode.StretchToFill);

            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.yMax - 3, headerRect.width, 3),
                BrandColor
            );

            GUI.Label(
                new Rect(headerRect.x + 12, headerRect.y + 6, headerRect.width - 24, 22),
                "ASAKI SCENE CONTEXT",
                _headerLabelStyle
            );

            string scopeText = "Scene Scope | v3.0 Prefab Mode";
            GUI.Label(
                new Rect(headerRect.x + 12, headerRect.y + 26, headerRect.width - 24, 16),
                scopeText,
                _miniLabelStyle
            );
        }

        #endregion

        #region Prefab Services Section

        private void DrawPrefabServicesSection()
        {
            DrawSectionHeader(
                "Scene Service Prefabs",
                PrefabHeaderColor,
                ref _foldoutPrefabs,
                GetPrefabStatusText()
            );

            if (!_foldoutPrefabs)
                return;

            EditorGUILayout.Space(6);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_servicePrefabsProp, new GUIContent("Prefabs"), true);

            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(_instanceParentProp, new GUIContent("Parent Transform"));

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(6);

            DrawPrefabPreview();
        }

        private string GetPrefabStatusText()
        {
            int prefabCount = _servicePrefabsProp.arraySize;
            int serviceCount = CountAllServicesInPrefabs();

            if (prefabCount == 0)
                return "No prefabs configured";
            if (serviceCount == 0)
                return $"{prefabCount} prefab(s), no services found";

            return $"{prefabCount} prefab(s) | {serviceCount} service(s)";
        }

        private int CountAllServicesInPrefabs()
        {
            int count = 0;
            _prefabServiceCounts.Clear();

            for (int i = 0; i < _servicePrefabsProp.arraySize; i++)
            {
                SerializedProperty element = _servicePrefabsProp.GetArrayElementAtIndex(i);
                GameObject prefab = element.objectReferenceValue as GameObject;

                int services =
                    prefab != null
                        ? prefab.GetComponentsInChildren<IAsakiSceneService>(true).Length
                        : 0;

                _prefabServiceCounts.Add(services);
                count += services;
            }

            return count;
        }

        private void DrawPrefabPreview()
        {
            if (_servicePrefabsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Drag prefabs containing IAsakiSceneService components here.\n"
                        + "Prefabs will be instantiated at runtime and services auto-registered.",
                    MessageType.Info
                );
                return;
            }

            CheckAndWarnDuplicatePrefabs();

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel, GUILayout.Height(18));

            EditorGUILayout.Space(4);

            for (int i = 0; i < _servicePrefabsProp.arraySize; i++)
            {
                SerializedProperty element = _servicePrefabsProp.GetArrayElementAtIndex(i);
                GameObject prefab = element.objectReferenceValue as GameObject;

                if (prefab == null)
                {
                    DrawWarningRow(i, "Missing Prefab", "Prefab reference is null");
                    continue;
                }

                int serviceCount = i < _prefabServiceCounts.Count ? _prefabServiceCounts[i] : 0;
                bool isDuplicate = IsPrefabDuplicate(prefab, i);

                DrawPrefabRow(prefab, serviceCount, i, isDuplicate);
            }
        }

        private void CheckAndWarnDuplicatePrefabs()
        {
            HashSet<string> seenPrefabPaths = new HashSet<string>();
            List<string> duplicateNames = new List<string>();

            for (int i = 0; i < _servicePrefabsProp.arraySize; i++)
            {
                SerializedProperty element = _servicePrefabsProp.GetArrayElementAtIndex(i);
                GameObject prefab = element.objectReferenceValue as GameObject;

                if (prefab == null)
                    continue;

                string prefabPath = AssetDatabase.GetAssetPath(prefab);

                if (seenPrefabPaths.Contains(prefabPath))
                {
                    if (!duplicateNames.Contains(prefab.name))
                    {
                        duplicateNames.Add(prefab.name);
                    }
                }
                else
                {
                    seenPrefabPaths.Add(prefabPath);
                }
            }

            if (duplicateNames.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"⚠ Duplicate prefab(s) detected: {string.Join(", ", duplicateNames)}\n"
                        + "Each prefab should only be added once. Please remove duplicates.",
                    MessageType.Warning
                );
                EditorGUILayout.Space(4);
            }
        }

        private bool IsPrefabDuplicate(GameObject prefab, int currentIndex)
        {
            if (prefab == null)
                return false;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);

            for (int i = 0; i < _servicePrefabsProp.arraySize; i++)
            {
                if (i >= currentIndex)
                    break;

                SerializedProperty element = _servicePrefabsProp.GetArrayElementAtIndex(i);
                GameObject otherPrefab = element.objectReferenceValue as GameObject;

                if (otherPrefab == null)
                    continue;

                string otherPath = AssetDatabase.GetAssetPath(otherPrefab);

                if (otherPath == prefabPath)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawPrefabRow(
            GameObject prefab,
            int serviceCount,
            int index,
            bool isDuplicate = false
        )
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Color statusColor;
            string statusIcon;

            if (isDuplicate)
            {
                statusColor = WarningColor;
                statusIcon = "⚠";
            }
            else
            {
                statusColor = serviceCount > 0 ? SuccessColor : WarningColor;
                statusIcon = serviceCount > 0 ? "✓" : "⚠";
            }

            GUI.color = statusColor;
            GUILayout.Label(statusIcon, GUILayout.Width(16));
            GUI.color = Color.white;

            string tooltip = isDuplicate
                ? $"⚠ DUPLICATE: This prefab is already in the list!\nPath: {AssetDatabase.GetAssetPath(prefab)}\nServices: {serviceCount}"
                : $"Path: {AssetDatabase.GetAssetPath(prefab)}\nServices: {serviceCount}";

            GUIContent prefabContent = new GUIContent(prefab.name, tooltip);

            GUI.enabled = false;
            EditorGUILayout.ObjectField(
                prefabContent,
                prefab,
                typeof(GameObject),
                false,
                GUILayout.ExpandWidth(true)
            );
            GUI.enabled = true;

            if (isDuplicate)
            {
                GUI.color = WarningColor;
                GUILayout.Label("DUPLICATE", _miniLabelStyle, GUILayout.Width(80));
                GUI.color = Color.white;
            }
            else
            {
                string countText = serviceCount == 1 ? "1 svc" : $"{serviceCount} svcs";
                GUILayout.Label(countText, _miniLabelStyle, GUILayout.Width(60));
            }

            EditorGUILayout.EndHorizontal();

            if (serviceCount > 0)
            {
                IAsakiSceneService[] services = prefab.GetComponentsInChildren<IAsakiSceneService>(
                    true
                );
                EditorGUI.indentLevel++;
                foreach (IAsakiSceneService service in services)
                {
                    if (service is MonoBehaviour behaviour)
                    {
                        Type type = behaviour.GetType();
                        string interfaceInfo = GetServiceInterfaceInfo(type);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(4);
                        GUI.color = new Color(0.5f, 0.7f, 0.5f);
                        GUILayout.Label("├", GUILayout.Width(14));
                        GUI.color = Color.white;

                        GUIContent serviceContent = new GUIContent(
                            type.Name,
                            $"Full Type: {type.FullName}"
                        );
                        EditorGUILayout.LabelField(serviceContent, _miniLabelStyle);

                        if (!string.IsNullOrEmpty(interfaceInfo))
                        {
                            GUI.color = new Color(0.5f, 0.5f, 0.6f);
                            GUILayout.Label("→", GUILayout.Width(14));
                            GUIContent interfaceContent = new GUIContent(
                                interfaceInfo,
                                $"Implements: {interfaceInfo}"
                            );
                            GUILayout.Label(interfaceContent, _miniLabelStyle);
                            GUI.color = Color.white;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawWarningRow(int index, string title, string message)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUI.color = WarningColor;
            GUILayout.Label("✗", GUILayout.Width(16));
            GUI.color = Color.white;

            GUILayout.Label($"{title}: {message}", _miniLabelStyle);

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Pure C# Services Section

        private void DrawPureCSharpServicesSection()
        {
            DrawSectionHeader(
                "Pure C# Services",
                SectionHeaderColor,
                ref _foldoutPureServices,
                GetPureServicesStatusText()
            );

            if (!_foldoutPureServices)
                return;

            EditorGUILayout.Space(6);

            DrawSearchAndPagination(
                ref _pureServicesSearchFilter,
                ref _pureServicesCurrentPage,
                _pureCSharpServicesProp.arraySize,
                UpdateFilteredPureServices
            );

            EditorGUILayout.Space(4);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_pureCSharpServicesProp, true);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);

            DrawAddServiceButton();

            if (_pureCSharpServicesProp.arraySize == 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Serializable C# classes implementing IAsakiSceneService.\n"
                        + "MonoBehaviour types should be in prefab services.\n\n"
                        + "Click 'Add Service' button above to add a new service.",
                    MessageType.Info
                );
            }
            else
            {
                ValidatePureCSharpServices();
            }
        }

        private void DrawAddServiceButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIContent addButtonContent = new GUIContent(
                "+ Add Service",
                "Click to add a new IAsakiSceneService implementation"
            );

            Color savedColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.7f, 0.9f);

            if (GUILayout.Button(addButtonContent, GUILayout.Height(24), GUILayout.Width(120)))
            {
                ShowAddServiceMenu();
            }

            GUI.backgroundColor = savedColor;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddServiceMenu()
        {
            GenericMenu menu = new GenericMenu();

            TypeCache.TypeCollection serviceTypes =
                TypeCache.GetTypesDerivedFrom<IAsakiSceneService>();

            HashSet<Type> existingTypes = GetExistingPureServiceTypes();

            foreach (Type type in serviceTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    continue;

                if (!type.IsSerializable && !type.IsValueType)
                    continue;

                string category = GetServiceCategory(type);
                string menuPath = string.IsNullOrEmpty(category)
                    ? type.Name
                    : $"{category}/{type.Name}";

                if (existingTypes.Contains(type))
                {
                    menu.AddDisabledItem(new GUIContent($"{menuPath} (Already Added)"));
                }
                else
                {
                    menu.AddItem(new GUIContent(menuPath), false, () => AddServiceOfType(type));
                }
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No valid service types found"));
            }

            menu.ShowAsContext();
        }

        private HashSet<Type> GetExistingPureServiceTypes()
        {
            HashSet<Type> types = new HashSet<Type>();

            for (int i = 0; i < _pureCSharpServicesProp.arraySize; i++)
            {
                SerializedProperty element = _pureCSharpServicesProp.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue != null)
                {
                    types.Add(element.managedReferenceValue.GetType());
                }
            }

            return types;
        }

        private string GetServiceCategory(Type type)
        {
            string ns = type.Namespace;
            if (string.IsNullOrEmpty(ns))
                return "";

            if (ns.StartsWith("Asaki.Game."))
                return "Game";
            if (ns.StartsWith("Asaki.Core."))
                return "Core";

            return "Other";
        }

        private void AddServiceOfType(Type type)
        {
            if (IsPureServiceTypeExists(type))
            {
                Debug.LogWarning(
                    $"[AsakiSceneContext] Service '{type.Name}' already exists. Duplicate not added."
                );
                EditorUtility.DisplayDialog(
                    "Duplicate Service",
                    $"Service '{type.Name}' is already in the list.\n\nEach service type can only be added once.",
                    "OK"
                );
                return;
            }

            _pureCSharpServicesProp.arraySize++;

            SerializedProperty newElement = _pureCSharpServicesProp.GetArrayElementAtIndex(
                _pureCSharpServicesProp.arraySize - 1
            );

            object instance = Activator.CreateInstance(type);
            newElement.managedReferenceValue = instance;

            serializedObject.ApplyModifiedProperties();

            Debug.Log($"[AsakiSceneContext] Added service: {type.Name}");
        }

        private bool IsPureServiceTypeExists(Type type)
        {
            for (int i = 0; i < _pureCSharpServicesProp.arraySize; i++)
            {
                SerializedProperty element = _pureCSharpServicesProp.GetArrayElementAtIndex(i);
                if (
                    element.managedReferenceValue != null
                    && element.managedReferenceValue.GetType() == type
                )
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateFilteredPureServices()
        {
            _filteredPureServiceIndices.Clear();

            for (int i = 0; i < _pureCSharpServicesProp.arraySize; i++)
            {
                SerializedProperty element = _pureCSharpServicesProp.GetArrayElementAtIndex(i);

                if (element.managedReferenceValue == null)
                    continue;

                Type elementType = element.managedReferenceValue.GetType();

                if (
                    string.IsNullOrEmpty(_pureServicesSearchFilter)
                    || elementType.Name.IndexOf(
                        _pureServicesSearchFilter,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                    || (
                        elementType.FullName != null
                        && elementType.FullName.IndexOf(
                            _pureServicesSearchFilter,
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                )
                {
                    _filteredPureServiceIndices.Add(i);
                }
            }

            int totalPages = Mathf.CeilToInt(
                (float)_filteredPureServiceIndices.Count / ItemsPerPage
            );
            if (_pureServicesCurrentPage >= totalPages && totalPages > 0)
            {
                _pureServicesCurrentPage = totalPages - 1;
            }
        }

        private string GetPureServicesStatusText()
        {
            int count = _pureCSharpServicesProp.arraySize;
            return count == 0 ? "No services" : $"{count} service(s)";
        }

        private void ValidatePureCSharpServices()
        {
            for (int i = 0; i < _pureCSharpServicesProp.arraySize; i++)
            {
                SerializedProperty element = _pureCSharpServicesProp.GetArrayElementAtIndex(i);

                if (element.managedReferenceValue == null)
                    continue;

                Type elementType = element.managedReferenceValue.GetType();

                if (typeof(MonoBehaviour).IsAssignableFrom(elementType))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        $"❌ {elementType.Name} is a MonoBehaviour!\n"
                            + "Move it to prefab services instead.",
                        MessageType.Error
                    );

                    if (GUILayout.Button("Remove This Entry", GUILayout.Height(20)))
                    {
                        _pureCSharpServicesProp.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    }
                }
            }
        }

        #endregion

        #region Runtime Section

        private void DrawRuntimeSection()
        {
            AsakiSceneContext context = (AsakiSceneContext)target;

            DrawSectionHeader(
                "Runtime Debugger",
                RuntimeColor,
                ref _foldoutRuntime,
                context.IsBuilt ? "Built ✓" : "Pending Build"
            );

            if (!_foldoutRuntime)
                return;

            EditorGUILayout.Space(6);

            Dictionary<Type, IAsakiService> services = context.GetRuntimeServices();
            List<GameObject> prefabs = context.GetInstantiatedPrefabs();

            DrawRuntimeStats(services.Count, prefabs.Count, context.IsBuilt);

            EditorGUILayout.Space(6);

            DrawRuntimeServiceList(services);

            if (Event.current.type == EventType.Layout)
            {
                Repaint();
            }
        }

        private void DrawRuntimeStats(int serviceCount, int prefabCount, bool isBuilt)
        {
            EditorGUILayout.BeginHorizontal();

            DrawStatBox("Services", serviceCount.ToString(), SuccessColor);
            DrawStatBox("Prefabs", prefabCount.ToString(), PrefabHeaderColor);
            DrawStatBox(
                "Status",
                isBuilt ? "Built" : "Pending",
                isBuilt ? SuccessColor : WarningColor
            );

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatBox(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(44));

            GUI.color = valueColor;
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Height(20));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(label, _miniLabelStyle, GUILayout.Height(14));

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeServiceList(Dictionary<Type, IAsakiService> services)
        {
            _foldoutServiceList = EditorGUILayout.Foldout(
                _foldoutServiceList,
                "Service List",
                true
            );

            if (!_foldoutServiceList)
                return;

            EditorGUILayout.Space(4);

            DrawSearchAndPagination(
                ref _runtimeServicesSearchFilter,
                ref _runtimeServicesCurrentPage,
                services.Count,
                () => UpdateFilteredRuntimeServices(services)
            );

            EditorGUILayout.Space(4);

            if (services.Count == 0)
            {
                EditorGUILayout.LabelField("  No services registered", _miniLabelStyle);
                return;
            }

            _runtimeScrollPosition = EditorGUILayout.BeginScrollView(
                _runtimeScrollPosition,
                GUILayout.Height(200)
            );

            if (
                _filteredRuntimeServices.Count == 0
                && !string.IsNullOrEmpty(_runtimeServicesSearchFilter)
            )
            {
                EditorGUILayout.HelpBox("No matching services found.", MessageType.Info);
            }
            else
            {
                int startIndex = _runtimeServicesCurrentPage * ItemsPerPage;
                int endIndex = Mathf.Min(startIndex + ItemsPerPage, _filteredRuntimeServices.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    var kvp = _filteredRuntimeServices[i];
                    DrawRuntimeServiceItem(kvp.Key, kvp.Value);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdateFilteredRuntimeServices(Dictionary<Type, IAsakiService> services)
        {
            _filteredRuntimeServices.Clear();

            foreach (var kvp in services)
            {
                if (
                    string.IsNullOrEmpty(_runtimeServicesSearchFilter)
                    || kvp.Key.Name.IndexOf(
                        _runtimeServicesSearchFilter,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                    || (
                        kvp.Key.FullName != null
                        && kvp.Key.FullName.IndexOf(
                            _runtimeServicesSearchFilter,
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                )
                {
                    _filteredRuntimeServices.Add(kvp);
                }
            }

            int totalPages = Mathf.CeilToInt((float)_filteredRuntimeServices.Count / ItemsPerPage);
            if (_runtimeServicesCurrentPage >= totalPages && totalPages > 0)
            {
                _runtimeServicesCurrentPage = totalPages - 1;
            }
        }

        private void DrawRuntimeServiceItem(Type type, IAsakiService service)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            string icon = service is MonoBehaviour ? "🎮" : "🔹";
            GUIContent content = new GUIContent(
                $"{icon} {type.Name}",
                $"Full Type: {type.FullName}"
            );
            EditorGUILayout.LabelField(content, EditorStyles.miniLabel);

            if (service is MonoBehaviour behaviour)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField(
                    behaviour,
                    typeof(MonoBehaviour),
                    true,
                    GUILayout.Width(120)
                );
                GUI.enabled = true;
            }
            else
            {
                GUI.color = new Color(0.5f, 0.5f, 0.6f);
                GUIContent implContent = new GUIContent(
                    $"({service.GetType().Name})",
                    $"Implementation: {service.GetType().FullName}"
                );
                EditorGUILayout.LabelField(implContent, _miniLabelStyle, GUILayout.Width(120));
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Search & Pagination

        private void DrawSearchAndPagination(
            ref string searchFilter,
            ref int currentPage,
            int totalCount,
            Action updateFilterAction
        )
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Search:", GUILayout.Width(50));

            string newFilter = GUILayout.TextField(
                searchFilter,
                _searchFieldStyle,
                GUILayout.ExpandWidth(true)
            );
            if (newFilter != searchFilter)
            {
                searchFilter = newFilter;
                currentPage = 0;
                updateFilterAction?.Invoke();
            }

            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                searchFilter = "";
                currentPage = 0;
                updateFilterAction?.Invoke();
            }

            EditorGUILayout.EndHorizontal();

            int filteredCount = GetFilteredCount(searchFilter, totalCount);
            if (filteredCount > ItemsPerPage)
            {
                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                int totalPages = Mathf.CeilToInt((float)filteredCount / ItemsPerPage);

                if (GUILayout.Button("◀", _paginationButtonStyle))
                {
                    currentPage = Mathf.Max(0, currentPage - 1);
                }

                GUILayout.Space(4);

                GUI.enabled = false;
                GUILayout.Label(
                    $" {currentPage + 1}/{totalPages} ",
                    EditorStyles.miniLabel,
                    GUILayout.Width(50)
                );
                GUI.enabled = true;

                GUILayout.Space(4);

                if (GUILayout.Button("▶", _paginationButtonStyle))
                {
                    currentPage = Mathf.Min(totalPages - 1, currentPage + 1);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private int GetFilteredCount(string searchFilter, int totalCount)
        {
            if (string.IsNullOrEmpty(searchFilter))
                return totalCount;

            return totalCount;
        }

        #endregion

        #region Utility Methods

        private void DrawSectionHeader(
            string title,
            Color color,
            ref bool foldout,
            string statusText
        )
        {
            EditorGUILayout.BeginHorizontal();

            Color foldoutColor = _foldoutStyle.normal.textColor;
            _foldoutStyle.normal.textColor = color;

            GUIContent titleContent = new GUIContent(title, $"Section: {title}");
            foldout = EditorGUILayout.Foldout(foldout, titleContent, true, _foldoutStyle);

            _foldoutStyle.normal.textColor = foldoutColor;

            GUILayout.FlexibleSpace();

            GUI.color = new Color(color.r, color.g, color.b, 0.8f);
            GUIContent statusContent = new GUIContent(statusText, statusText);
            GUILayout.Label(statusContent, _miniLabelStyle);
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            Rect lineRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(
                new Rect(lineRect.x, lineRect.yMax + 2, lineRect.width, 1),
                new Color(color.r, color.g, color.b, 0.4f)
            );
        }

        private string GetServiceInterfaceInfo(Type type)
        {
            Type[] interfaces = type.GetInterfaces()
                .Where(t =>
                    typeof(IAsakiService).IsAssignableFrom(t)
                    && t != typeof(IAsakiService)
                    && t != typeof(IAsakiSceneService)
                    && t != typeof(IAsakiGlobalService)
                )
                .ToArray();

            return interfaces.Length > 0 ? string.Join(", ", interfaces.Select(t => t.Name)) : "";
        }

        #endregion
    }
}
