﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Steamworks;

public class MEModHandler : MonoBehaviour
{
    public Button loadModButton;
    public Transform modListContainer;
    public GameObject modEntryPrefab;

    string modFolderPath;
    readonly List<ModEntry> loadedMods = new List<ModEntry>();

    void Start()
    {
        modFolderPath = Path.Combine(Application.persistentDataPath, "Mods");
        Directory.CreateDirectory(modFolderPath);
        if (loadModButton != null) loadModButton.onClick.AddListener(OpenFileDialogAndLoadMod);
        StartCoroutine(BootLoadMods());
    }

    System.Collections.IEnumerator BootLoadMods()
    {
        yield return null;
        LoadAllModsInFolder();
    }

    void LoadAllModsInFolder()
    {
        for (int i = modListContainer.childCount - 1; i >= 0; i--) Destroy(modListContainer.GetChild(i).gameObject);
        loadedMods.Clear();

        var files = new List<string>();
        try
        {
            foreach (var f in Directory.EnumerateFiles(modFolderPath, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.IsNullOrEmpty(ext)) continue;
                if (ext.Equals(".me", StringComparison.OrdinalIgnoreCase) || ext.Equals(".unity3d", StringComparison.OrdinalIgnoreCase))
                    files.Add(f);
            }
        }
        catch { }

        for (int i = 0; i < files.Count; i++)
        {
            var path = files[i];
            if (path.EndsWith(".me", StringComparison.OrdinalIgnoreCase))
                LoadME(path);
            else if (path.EndsWith(".unity3d", StringComparison.OrdinalIgnoreCase))
                LoadUnity3D(path, true, true);
        }

        var mgr = FindFirstObjectByType<CustomDancePlayer.DancePlayerUIManager>();
        if (mgr != null) mgr.RefreshDropdown();
    }

    void OpenFileDialogAndLoadMod()
    {
        var ext = new[] { new ExtensionFilter("MateEngine Files", "me", "unity3d") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Mod or Dance Asset", ".", ext, false);
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

        var src = paths[0];
        var dest = Path.Combine(modFolderPath, Path.GetFileName(src));
        try { File.Copy(src, dest, true); } catch { }

        if (dest.EndsWith(".me", StringComparison.OrdinalIgnoreCase))
        {
            LoadME(dest);
        }
        else if (dest.EndsWith(".unity3d", StringComparison.OrdinalIgnoreCase))
        {
            LoadUnity3D(dest, true, false);
            FindFirstObjectByType<CustomDancePlayer.DancePlayerUIManager>()?.RefreshDropdown();
        }
    }

    void LoadUnity3D(string path, bool addToUI, bool respectSavedState)
    {
        var name = Path.GetFileNameWithoutExtension(path);

        int exist = loadedMods.FindIndex(m => string.Equals(m.name, name, StringComparison.OrdinalIgnoreCase) && m.type == ModType.Unity3D);
        if (exist >= 0) loadedMods.RemoveAt(exist);

        bool enable = true;
        if (respectSavedState && SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data != null)
        {
            if (SaveLoadHandler.Instance.data.modStates.TryGetValue(name, out var s)) enable = s;
        }

        var resMgr = FindFirstObjectByType<CustomDancePlayer.DanceResourceManager>();
        if (enable && resMgr != null)
        {
            try { resMgr.UnregisterInjected(name); } catch { }
            try
            {
                var bundle = AssetBundle.LoadFromFile(path);
                if (bundle != null) resMgr.RegisterDanceBundle(bundle, name);
            }
            catch { }
        }

        var entry = new ModEntry { name = name, localPath = path, type = ModType.Unity3D, instance = null, enabled = enable, author = "Author: Unknown" };
        loadedMods.Add(entry);
        if (addToUI) AddToModListUI(entry, enable);
    }

    void LoadME(string path)
    {
        string id = Path.GetFileNameWithoutExtension(path);
        string cacheRoot = Path.Combine(Application.temporaryCachePath, "ME_Cache");
        Directory.CreateDirectory(cacheRoot);
        string dst = Path.Combine(cacheRoot, id);

        bool needExtract = true;
        try
        {
            if (Directory.Exists(dst))
            {
                DateTime srcTime = File.GetLastWriteTimeUtc(path);
                DateTime dstTime = Directory.GetLastWriteTimeUtc(dst);
                if (dstTime >= srcTime) needExtract = false;
                else Directory.Delete(dst, true);
            }
            if (needExtract)
            {
                ZipFile.ExtractToDirectory(path, dst);
                Directory.SetLastWriteTimeUtc(dst, File.GetLastWriteTimeUtc(path));
            }
        }
        catch { return; }

        string metaPath = Path.Combine(dst, "dance_meta.json");
        bool isDance = File.Exists(metaPath);

        if (isDance)
        {
            LoadMEDance(path, dst, id);
        }
        else
        {
            LoadMEObject(path, dst, id);
        }
    }

    void LoadMEDance(string mePath, string extractedDir, string id)
    {
        bool enable = GetSavedStateOrDefault(id, true);

        string bundlePath = Directory.GetFiles(extractedDir, "*.bundle", SearchOption.AllDirectories).FirstOrDefault();
        if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
        {
            var alt = Directory.GetFiles(extractedDir, "*", SearchOption.AllDirectories).FirstOrDefault(f => Path.GetExtension(f).Equals(".bundle", StringComparison.OrdinalIgnoreCase));
            bundlePath = alt;
        }
        if (string.IsNullOrEmpty(bundlePath)) return;

        var resMgr = FindFirstObjectByType<CustomDancePlayer.DanceResourceManager>();
        if (enable && resMgr != null)
        {
            try { resMgr.UnregisterInjected(id); } catch { }
            try
            {
                var bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle != null) resMgr.RegisterDanceBundle(bundle, id);
            }
            catch { }
        }

        string author = "Author: Unknown";
        var metaPath = Path.Combine(extractedDir, "dance_meta.json");
        if (File.Exists(metaPath))
        {
            try
            {
                var json = File.ReadAllText(metaPath);
                var meta = JsonUtility.FromJson<DanceMeta>(json);
                string cand = null;
                if (!string.IsNullOrWhiteSpace(meta.songAuthor)) cand = meta.songAuthor;
                else if (!string.IsNullOrWhiteSpace(meta.mmdAuthor)) cand = meta.mmdAuthor;
                if (!string.IsNullOrWhiteSpace(cand)) author = "Author: " + cand;
            }
            catch { }
        }

        var entry = new ModEntry { name = id, instance = null, localPath = mePath, extractedPath = extractedDir, type = ModType.MEDance, enabled = enable, author = author };
        loadedMods.Add(entry);
        AddToModListUI(entry, enable);
    }

    void LoadMEObject(string mePath, string extractedDir, string id)
    {
        string bundlePath = null;
        try
        {
            foreach (var file in Directory.GetFiles(extractedDir, "*.bundle", SearchOption.AllDirectories))
            {
                bundlePath = file;
                break;
            }
        }
        catch { }

        if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath)) return;

        AssetBundle bundle = null;
        try { bundle = AssetBundle.LoadFromFile(bundlePath); } catch { }
        if (bundle == null) return;

        GameObject prefab = null;
        try { prefab = bundle.LoadAsset<GameObject>(id); } catch { }

        if (prefab == null)
        {
            var all = bundle.LoadAllAssets<GameObject>();
            if (all != null && all.Length > 0) prefab = all[0];
        }

        GameObject instance = null;
        if (prefab != null)
        {
            try { instance = Instantiate(prefab); } catch { }
        }

        try { bundle.Unload(false); } catch { }

        if (instance == null) return;

        var refPathJson = Path.Combine(extractedDir, "reference_paths.json");
        var sceneLinksPath = Path.Combine(extractedDir, "scene_links.json");

        var refPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sceneLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (File.Exists(refPathJson))
            {
                var json = File.ReadAllText(refPathJson);
                var obj = JsonUtility.FromJson<RefPathMap>(json);
                for (int i = 0; i < obj.keys.Count; i++) refPaths[obj.keys[i]] = obj.values[i];
            }
        }
        catch { }

        try
        {
            if (File.Exists(sceneLinksPath))
            {
                var json = File.ReadAllText(sceneLinksPath);
                var obj = JsonUtility.FromJson<SceneLinkMap>(json);
                for (int i = 0; i < obj.keys.Count; i++) sceneLinks[obj.keys[i]] = obj.values[i];
            }
        }
        catch { }

        ApplyReferencePaths(instance, refPaths, sceneLinks);

        bool initialState = GetSavedStateOrDefault(id, true);
        instance.SetActive(initialState);

        var entry = new ModEntry { name = id, instance = instance, localPath = mePath, extractedPath = extractedDir, type = ModType.MEObject, enabled = initialState, author = "Author: Unknown" };
        loadedMods.Add(entry);
        AddToModListUI(entry, initialState);
    }

    void ApplyReferencePaths(GameObject root, Dictionary<string, string> refPaths, Dictionary<string, string> sceneLinks)
    {
        var allBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int b = 0; b < allBehaviours.Length; b++)
        {
            var mb = allBehaviours[b];
            if (mb == null) continue;
            var type = mb.GetType();
            var typeName = type.Name;

            foreach (var map in new[] { refPaths, sceneLinks })
            {
                foreach (var kv in map)
                {
                    if (!kv.Key.StartsWith(typeName + ".", StringComparison.Ordinal)) continue;

                    string rawPath = kv.Key.Substring(typeName.Length + 1);
                    GameObject sceneGO = GameObject.Find(kv.Value);
                    if (sceneGO == null) continue;

                    object current = mb;
                    Type currentType = type;

                    var parts = rawPath.Split('.');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string part = parts[i];
                        int listIndex = -1;

                        if (part.Contains("["))
                        {
                            int s = part.IndexOf('[');
                            int e = part.IndexOf(']');
                            listIndex = int.Parse(part.Substring(s + 1, e - s - 1));
                            part = part.Substring(0, s);
                        }

                        FieldInfo field = currentType.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field == null) break;

                        bool isLast = (i == parts.Length - 1);

                        if (isLast)
                        {
                            if (field.FieldType == typeof(GameObject))
                                field.SetValue(current, sceneGO);
                            else if (typeof(Component).IsAssignableFrom(field.FieldType))
                            {
                                var comp = sceneGO.GetComponent(field.FieldType);
                                if (comp != null) field.SetValue(current, comp);
                            }
                        }
                        else
                        {
                            object next = field.GetValue(current);
                            if (next == null) break;

                            if (listIndex >= 0 && next is System.Collections.IList list)
                            {
                                if (listIndex >= list.Count) break;
                                current = list[listIndex];
                            }
                            else
                            {
                                current = next;
                            }

                            if (current == null) break;
                            currentType = current.GetType();
                        }
                    }
                }
            }
        }
    }

    bool GetSavedStateOrDefault(string key, bool def)
    {
        if (SaveLoadHandler.Instance == null || SaveLoadHandler.Instance.data == null) return def;
        if (SaveLoadHandler.Instance.data.modStates.TryGetValue(key, out var state)) return state;
        return def;
    }

    void AddToModListUI(ModEntry mod, bool initialState)
    {
        if (modEntryPrefab == null || modListContainer == null) return;

        var entry = Instantiate(modEntryPrefab, modListContainer);
        entry.name = "Mod_" + mod.name;

        var title = FindChildByName<TMP_Text>(entry.transform, "Title");
        if (title == null)
        {
            var legacy = FindChildByName<TextMeshProUGUI>(entry.transform, "ModNameText");
            if (legacy != null) title = legacy;
        }
        if (title != null) title.text = mod.name;

        var author = FindChildByName<TMP_Text>(entry.transform, "Author");
        if (author != null) author.text = string.IsNullOrWhiteSpace(mod.author) ? "Author: Unknown" : mod.author;

        var tog = entry.GetComponentInChildren<Toggle>(true);
        if (tog != null)
        {
            tog.isOn = initialState;
            if (mod.type == ModType.MEObject)
            {
                if (mod.instance != null) mod.instance.SetActive(initialState);
                tog.onValueChanged.AddListener(a =>
                {
                    if (mod.instance != null) mod.instance.SetActive(a);
                    PersistState(mod.name, a);
                });
            }
            else
            {
                tog.onValueChanged.AddListener(a =>
                {
                    HandleDanceToggle(mod, a);
                    PersistState(mod.name, a);
                    FindFirstObjectByType<CustomDancePlayer.DancePlayerUIManager>()?.RefreshDropdown();
                });
            }
        }

        var uploadBtn = FindChildByName<Button>(entry.transform, "Upload");
        if (uploadBtn != null)
        {
            var up = uploadBtn.GetComponent<ModUploadButton>();
            var progress = FindChildByName<Slider>(entry.transform, "Progress");
            if (up != null)
            {
                up.button = uploadBtn;
                up.filePath = mod.localPath;
                up.displayName = mod.name;
                up.author = mod.author.StartsWith("Author: ") ? mod.author.Substring(8) : mod.author;
                up.isNSFW = false;
                up.thumbnailPath = null;
                up.progressBar = progress;
            }
            else
            {
                uploadBtn.onClick.AddListener(() =>
                {
                    var handler = FindFirstObjectByType<SteamWorkshopHandler>();
                    if (handler != null) handler.BeginUploadMod(mod.localPath, progress);
                });
            }
        }

        var removeBtn = FindChildByName<Button>(entry.transform, "Remove");
        if (removeBtn != null)
        {
            var rm = removeBtn.GetComponent<ModRemoveButton>();
            if (rm != null)
            {
                rm.button = removeBtn;
                rm.filePath = mod.localPath;
                rm.workshopId = ResolveWorkshopIdForPath(mod.localPath);
            }
            else
            {
                removeBtn.onClick.AddListener(() => RemoveMod(mod, entry));
            }
        }

        var hueShifter = FindFirstObjectByType<MenuHueShift>();
        if (hueShifter != null) hueShifter.RefreshNewGraphicsAndSelectables(entry.transform);
    }

    ulong ResolveWorkshopIdForPath(string localPath)
    {
        try
        {
            if (!SteamManager.Initialized) return 0UL;
            uint count = SteamUGC.GetNumSubscribedItems();
            if (count == 0) return 0UL;

            var ids = new Steamworks.PublishedFileId_t[count];
            SteamUGC.GetSubscribedItems(ids, count);

            string targetName = Path.GetFileName(localPath);
            for (int i = 0; i < ids.Length; i++)
            {
                if (!SteamUGC.GetItemInstallInfo(ids[i], out ulong _, out string installPath, 1024, out uint _)) continue;
                if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath)) continue;
                var top = Directory.GetFiles(installPath, "*", SearchOption.TopDirectoryOnly);
                for (int f = 0; f < top.Length; f++)
                {
                    if (string.Equals(Path.GetFileName(top[f]), targetName, StringComparison.OrdinalIgnoreCase))
                        return ids[i].m_PublishedFileId;
                }
            }
        }
        catch { }
        return 0UL;
    }



    void HandleDanceToggle(ModEntry mod, bool enable)
    {
        var mgr = FindFirstObjectByType<CustomDancePlayer.DanceResourceManager>();
        if (mgr == null) return;

        if (enable)
        {
            try { mgr.UnregisterInjected(mod.name); } catch { }
            try
            {
                if (mod.type == ModType.Unity3D)
                {
                    var bundle = AssetBundle.LoadFromFile(mod.localPath);
                    if (bundle != null) mgr.RegisterDanceBundle(bundle, mod.name);
                }
                else if (mod.type == ModType.MEDance)
                {
                    string bundlePath = Directory.GetFiles(mod.extractedPath, "*.bundle", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(bundlePath))
                    {
                        var bundle = AssetBundle.LoadFromFile(bundlePath);
                        if (bundle != null) mgr.RegisterDanceBundle(bundle, mod.name);
                    }
                }
            }
            catch { }
        }
        else
        {
            mgr.UnregisterInjected(mod.name);
        }
    }

    void PersistState(string name, bool a)
    {
        if (SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data != null)
        {
            SaveLoadHandler.Instance.data.modStates[name] = a;
            SaveLoadHandler.Instance.SaveToDisk();
        }
    }

    void RemoveMod(ModEntry mod, GameObject ui)
    {
        if (mod.type == ModType.MEObject)
        {
            if (mod.instance != null) Destroy(mod.instance);
        }
        else
        {
            var mgr = FindFirstObjectByType<CustomDancePlayer.DanceResourceManager>();
            if (mgr != null) mgr.UnregisterInjected(mod.name);
            FindFirstObjectByType<CustomDancePlayer.DancePlayerUIManager>()?.RefreshDropdown();
        }

        try { if (File.Exists(mod.localPath)) File.Delete(mod.localPath); } catch { }
        loadedMods.Remove(mod);
        Destroy(ui);
    }

    T FindChildByName<T>(Transform root, string name) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        var trs = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < trs.Length; i++)
            if (trs[i].name == name) return trs[i].GetComponent<T>();
        return null;
    }

    Type ResolveType(string name)
    {
        var t = Type.GetType(name);
        if (t != null) return t;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    [Serializable] class ModEntry { public string name; public GameObject instance; public string localPath; public string extractedPath; public ModType type; public bool enabled; public string author; }
    enum ModType { MEObject, Unity3D, MEDance }
    [Serializable] class RefPathMap { public List<string> keys = new List<string>(); public List<string> values = new List<string>(); }
    [Serializable] class SceneLinkMap { public List<string> keys = new List<string>(); public List<string> values = new List<string>(); }

    [Serializable]
    class DanceMeta
    {
        public string songName;
        public string songAuthor;
        public string mmdAuthor;
        public float songLength;
        public string placeholderClipName;
    }
}
