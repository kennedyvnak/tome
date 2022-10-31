using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Kennedy.UnityEditorUtility.ScriptableSingletons
{
    public class ScriptablesSingletonWindow : EditorWindow
    {
        private class AssetHandler : AssetPostprocessor
        {
            private static event System.Action<string> e_AssetImported;
            private static event System.Action<string> e_AssetDeleted;
            private static event System.Action<string, string> e_AssetMoved;

            public readonly Dictionary<string, Type> singletonsPath = new Dictionary<string, Type>();

            public event System.Action<System.Type> SingletonImported;
            public event System.Action<System.Type> SingletonDeleted;
            public event System.Action<System.Type, string, string> SingletonMoved;
            public event System.Action AddressablesChanged;

            public AssetHandler()
            {
                string[] guids = AssetDatabase.FindAssets($"t:{typeof(UnityUtility.ScriptableSingletons.ScriptableSingleton<>).Name}");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    singletonsPath.Add(path, AssetDatabase.LoadAssetAtPath(path, typeof(ScriptableObject)).GetType());
                }

                e_AssetImported += AssetImported;
                e_AssetDeleted += AssetDeleted;
                e_AssetMoved += AssetMoved;
                AddressableAssetSettingsDefaultObject.Settings.OnModification += AddressablesModificationHandle;
            }

            ~AssetHandler()
            {
                e_AssetImported -= AssetImported;
                e_AssetDeleted -= AssetDeleted;
                e_AssetMoved -= AssetMoved;
                AddressableAssetSettingsDefaultObject.Settings.OnModification -= AddressablesModificationHandle;
            }

            private void AddressablesModificationHandle(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent modificationEvent, object evtArgs)
            {
                if (modificationEvent == AddressableAssetSettings.ModificationEvent.EntryRemoved
                   || modificationEvent == AddressableAssetSettings.ModificationEvent.EntryAdded
                   || modificationEvent == AddressableAssetSettings.ModificationEvent.EntryCreated
                   || modificationEvent == AddressableAssetSettings.ModificationEvent.EntryModified
                   || modificationEvent == AddressableAssetSettings.ModificationEvent.EntryMoved
                   || modificationEvent == AddressableAssetSettings.ModificationEvent.GroupAdded
                   || modificationEvent == AddressableAssetSettings.ModificationEvent.GroupRemoved
                   || modificationEvent == AddressableAssetSettings.ModificationEvent.GroupRenamed)
                {
                    AddressablesChanged?.Invoke();
                }
            }

            private void AssetImported(string path)
            {
                if (!singletonsPath.ContainsKey(path))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(ScriptableObject));
                    if (!asset)
                        return;
                    Type assetType = asset.GetType();
                    if (IsSubclassOfRawGeneric(assetType, typeof(UnityUtility.ScriptableSingletons.ScriptableSingleton<>)))
                    {
                        singletonsPath.Add(path, assetType);
                        SingletonImported?.Invoke(assetType);
                    }
                }
            }

            private void AssetDeleted(string path)
            {
                if (singletonsPath.TryGetValue(path, out Type deletedType))
                {
                    singletonsPath.Remove(path);
                    SingletonDeleted?.Invoke(deletedType);
                }
            }

            private void AssetMoved(string oldPath, string newPath)
            {
                if (singletonsPath.TryGetValue(oldPath, out Type movedType))
                {
                    singletonsPath.Remove(oldPath);
                    singletonsPath.Add(newPath, movedType);
                    SingletonMoved?.Invoke(movedType, oldPath, newPath);
                }
            }

            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    string importedAsset = importedAssets[i];
                    e_AssetImported?.Invoke(importedAsset);
                }

                for (int i = 0; i < deletedAssets.Length; i++)
                {
                    string deletedAsset = deletedAssets[i];
                    e_AssetDeleted?.Invoke(deletedAsset);
                }

                for (int i = 0; i < movedAssets.Length; i++)
                {
                    e_AssetMoved?.Invoke(movedFromAssetPaths[i], movedAssets[i]);
                }
            }

            private static bool IsSubclassOfRawGeneric(Type toCheck, Type baseType)
            {
                while (toCheck != typeof(object))
                {
                    Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                    if (baseType == cur)
                    {
                        return true;
                    }

                    toCheck = toCheck.BaseType;
                }

                return false;
            }
        }

        private enum InstanceStatus { None, Single, Multiple }

        private const string k_TemplatePath = "Assets/Editor/UI/Templates/ScriptableSingletons.uxml";
        private const string k_ItemObjectTemplatePath = "Assets/Editor/UI/Templates/ScriptableSingletons-ItemObject.uxml";

        private const string k_SingletonsGroupName = "ScriptableSingletons";
        private const string k_SingletonsEntryLabel = "Scriptable Singleton";

        private static List<Type> scriptableSingletons { get; set; }

        private VisualTreeAsset _itemObjectTemplate;

        private VisualElement _contentStatusIcon;
        private ListView _objectsListView;

        private ToolbarSearchField _searchField;

        private Dictionary<VisualElement, EventCallback<ContextualMenuPopulateEvent>> _elementsEvents = new Dictionary<VisualElement, EventCallback<ContextualMenuPopulateEvent>>();

        private List<System.Type> _singletonTypes;

        private Texture2D _warningTexture, _okTexture, _errorTexture;

        private AssetHandler _assetHandler;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void Initialize()
        {
            scriptableSingletons = TypeCache.GetTypesDerivedFrom(typeof(UnityUtility.ScriptableSingletons.ScriptableSingleton<>))
                .Where(x => !x.IsAbstract).ToList();
        }

        [MenuItem("Window/Asset Management/Scriptable Singletons")]
        private static void ShowWindow()
        {
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
            {
                if (UnityEditor.EditorUtility.DisplayDialog("Don't exists addressables settings", "The addressables settings doesn't exists in project, create new?", "Yes", "No"))
                {
                    // Create addressables settings
                    AddressableAssetSettingsDefaultObject.GetSettings(true);
                }
                else
                    return;
            }
            GetWindow<ScriptablesSingletonWindow>().Show();
        }

        private void OnEnable()
        {
            _assetHandler = new AssetHandler();
            _singletonTypes = scriptableSingletons;

            titleContent = new GUIContent("Scriptable Singletons");
        }

        public void CreateGUI()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath).CloneTree(rootVisualElement);
            _itemObjectTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_ItemObjectTemplatePath);

            _objectsListView = rootVisualElement.Q<ListView>("objects-list-view");
            _contentStatusIcon = rootVisualElement.Q<VisualElement>("type-status-icon");
            _searchField = rootVisualElement.Q<ToolbarSearchField>("search-field");

            _errorTexture = EditorGUIUtility.IconContent("d_redLight").image as Texture2D;
            _warningTexture = EditorGUIUtility.IconContent("d_orangeLight").image as Texture2D;
            _okTexture = EditorGUIUtility.IconContent("d_greenLight").image as Texture2D;

            _contentStatusIcon.style.backgroundImage = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image as Texture2D;

            _assetHandler.SingletonDeleted += SingletonDeleted;
            _assetHandler.SingletonImported += SingletonImported;
            _assetHandler.SingletonMoved += SingletonMoved;
            _assetHandler.AddressablesChanged += AddressablesChanged;

            _objectsListView.makeItem = () =>
            {
                TemplateContainer element = _itemObjectTemplate.Instantiate();
                element.AddManipulator(new ContextualMenuManipulator(null));
                return element;
            };

            _objectsListView.onItemsChosen += (items) =>
            {
                foreach (object item in items)
                    if (item is Type itemType)
                        ShowTypeSelection(itemType);
            };

            _objectsListView.bindItem = (e, i) =>
            {
                Type type = _singletonTypes[i];
                InstanceStatus status = GetInstanceStatus(type);

                e.Q<Label>("type-name").text = type.Name;
                e.Q<Label>("asset-path").text = GetInstancePathDisplay(type);

                Texture2D statusIcon = status == InstanceStatus.Single && !InstanceAddressIsCorrect(type) ? _errorTexture : GetStatusTexture(status);
                e.Q<VisualElement>("state-icon").style.backgroundImage = statusIcon;

                if (_elementsEvents.TryGetValue(e, out EventCallback<ContextualMenuPopulateEvent> oldEvt))
                    e.UnregisterCallback<ContextualMenuPopulateEvent>(oldEvt);

                EventCallback<ContextualMenuPopulateEvent> newEvt = (evt) =>
                {
                    evt.menu.AppendAction("Create Instance",
                        (a) => CreateSingletonInstance(type),
                        (a) => status == InstanceStatus.None ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Select All Instances",
                        (a) => SelectAllInstancesOfType(type),
                        (a) => DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction("Fix instance address",
                        (a) => SetInstanceAddress(type),
                        (a) => InstanceAddressIsCorrect(type) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                };

                _elementsEvents[e] = newEvt;
                e.RegisterCallback(newEvt);
            };

            _objectsListView.itemsSource = _singletonTypes;
            _objectsListView.Rebuild();
        }

        private void CreateSingletonInstance(Type type)
        {
            string path = EditorUtility.SaveFilePanel($"Select {type.Name} singleton path", "Assets/Data/Settings", type.Name, "asset");
            path = UnityEditor.FileUtil.GetProjectRelativePath(path);
            if (string.IsNullOrEmpty(path))
                return;
            ScriptableObject instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, path);
        }

        private void SelectAllInstancesOfType(Type type)
        {
            ScriptableObject[] instances = _assetHandler.singletonsPath.Where(pair => pair.Value == type)
                .Select(p => AssetDatabase.LoadAssetAtPath<ScriptableObject>(p.Key)).ToArray();
            Selection.objects = instances;
        }

        private string GetInstancePathDisplay(Type type) => GetInstanceStatus(type) switch
        {
            InstanceStatus.None => "Don't exist an instance in project.",
            InstanceStatus.Multiple => "More than one instances in project;",
            InstanceStatus.Single => GetInstancePath(type),
            _ => throw new System.IndexOutOfRangeException(),
        };

        private void SingletonImported(Type obj) => RefreshTypeElement(obj);

        private void SingletonDeleted(Type obj) => RefreshTypeElement(obj);

        private void SingletonMoved(Type obj, string oldPath, string newPath) => RefreshTypeElement(obj);

        private void AddressablesChanged() => _objectsListView.RefreshItems();

        private void RefreshTypeElement(Type type)
        {
            _objectsListView.RefreshItem(_singletonTypes.IndexOf(type));
        }

        private InstanceStatus GetInstanceStatus(System.Type type)
        {
            int instancesCount = _assetHandler.singletonsPath.Values.Count(t => t == type);

            return instancesCount > 1 ? InstanceStatus.Multiple : instancesCount == 1 ? InstanceStatus.Single : InstanceStatus.None;
        }

        private Texture2D GetStatusTexture(InstanceStatus status) => status switch
        {
            InstanceStatus.Single => _okTexture,
            InstanceStatus.None => _warningTexture,
            InstanceStatus.Multiple => _errorTexture,
            _ => throw new System.IndexOutOfRangeException(),
        };

        private void ShowTypeSelection(Type type)
        {
            if (GetInstanceStatus(type) == InstanceStatus.None)
                return;

            UnityEngine.Object obj = LoadSingleInstanceType(type);
            if (obj)
                EditorGUIUtility.PingObject(obj);
        }

        private UnityEngine.Object LoadSingleInstanceType(Type type)
        {
            return AssetDatabase.LoadAssetAtPath(GetInstancePath(type), typeof(UnityEngine.Object));
        }

        private string GetInstancePath(Type type) => _assetHandler.singletonsPath.First(t => t.Value == type).Key;

        private void SetInstanceAddress(Type type)
        {
            InstanceStatus instanceStatus = GetInstanceStatus(type);

            if (instanceStatus == InstanceStatus.Single)
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                AddressableAssetGroup group = settings.FindGroup(k_SingletonsGroupName);
                if (!group)
                    group = settings.CreateGroup(k_SingletonsGroupName, false, false, false, settings.DefaultGroup.Schemas);

                string instancePath = GetInstancePath(type);
                string instanceGuid = AssetDatabase.AssetPathToGUID(instancePath);

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(instanceGuid, group, readOnly: false, postEvent: false);
                if (!settings.GetLabels().Contains(k_SingletonsEntryLabel))
                    settings.AddLabel(k_SingletonsEntryLabel, false);
                entry.labels.Add(k_SingletonsEntryLabel);
                entry.SetAddress(type.FullName);

                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }
        }

        private bool InstanceAddressIsCorrect(Type type)
        {
            InstanceStatus instanceStatus = GetInstanceStatus(type);

            if (instanceStatus == InstanceStatus.Single)
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

                string instancePath = GetInstancePath(type);
                string instanceGuid = AssetDatabase.AssetPathToGUID(instancePath);

                AddressableAssetEntry entry = settings.FindAssetEntry(instanceGuid);
                return entry != null && entry.address == type.FullName && entry.parentGroup.Name == k_SingletonsGroupName;
            }

            return false;
        }
    }
}