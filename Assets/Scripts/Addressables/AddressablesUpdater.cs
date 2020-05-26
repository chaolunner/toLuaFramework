using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.IO;
using System;

public enum RequestDownloadFeedback
{
    None,
    Agree,
    Disagree,
}

public class AddressablesUpdater : MonoBehaviour
{
    public static event Action OnCompleted;
    public static event Action<long, long> OnDownload;
    public static event Action<long> OnDownloadCompleted;
    public static Func<IEnumerator> AfterDownloadHandle;
    public static Func<RequestDownloadFeedback> RequestDownloadHandle;

    private static string addressablesDir = Application.persistentDataPath + "/" + addressablesFolder;
    private static string backupDir = Application.persistentDataPath + "/" + backupFolder;
    private const string addressablesFolder = "com.unity.addressables";
    private const string backupFolder = "backup.addressables";

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        yield return StartCoroutine(DownloadUpdateAsync());
        OnCompleted?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator DownloadUpdateAsync()
    {
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;
        var catalogs = checkHandle.Result;
        Addressables.Release(checkHandle);
        if (catalogs == null || catalogs.Count <= 0) { yield break; }
        SaveCatalogs();
        var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
        yield return updateHandle;
        long downloadSize = 0;
        long size = 0;
        var sizeMap = new Dictionary<int, long>();
        var locators = updateHandle.Result;
        Addressables.Release(updateHandle);
        for (int i = 0; i < locators.Count; i++)
        {
            IList<IResourceLocation> locations;
            var e = locators[i].Keys.GetEnumerator();
            var j = 0;
            while (e.MoveNext())
            {
                if (locators[i].Locate(e.Current, typeof(object), out locations))
                {
                    for (int k = 0; k < locations.Count; k++)
                    {
                        if (locations[k].Data != null && locations[k].Data is AssetBundleRequestOptions && ResourceManagerConfig.IsPathRemote(locations[k].InternalId))
                        {
                            size += (locations[k].Data as AssetBundleRequestOptions).BundleSize;
                        }
                    }
                    sizeMap.Add(j, size);
                    downloadSize += size;
                    size = 0;
                }
                j++;
            }
        }
        if (downloadSize <= 0) { yield break; }
        while (RequestDownloadHandle?.Invoke() == RequestDownloadFeedback.None)
        {
            yield return null;
        }
        if (RequestDownloadHandle?.Invoke() == RequestDownloadFeedback.Disagree)
        {
            LoadCatalogs();
            for (int i = 0; i < locators.Count; i++)
            {
                Addressables.RemoveResourceLocator(locators[i]);
            }
            var catalogPaths = GetCatalogs(addressablesDir, "json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < catalogPaths.Length; i++)
            {
                var catalogHandle = Addressables.LoadContentCatalogAsync(catalogPaths[i]);
                yield return catalogHandle;
                Addressables.AddResourceLocator(catalogHandle.Result);
                Addressables.Release(catalogHandle);
            }
#if UNITY_EDITOR
            Debug.Log(string.Format("Cancel Download Size: {0}", downloadSize));
#endif
            yield break;
        }
        DeleteCatalogs();
        for (int i = 0; i < locators.Count; i++)
        {
            IList<IResourceLocation> locations;
            var e = locators[i].Keys.GetEnumerator();
            var j = 0;
            while (e.MoveNext())
            {
                if (locators[i].Locate(e.Current, typeof(object), out locations))
                {
                    var downloadHandle = Addressables.DownloadDependenciesAsync(locations);
                    while (downloadHandle.PercentComplete < 1 && !downloadHandle.IsDone)
                    {
                        OnDownload?.Invoke(size + (long)(downloadHandle.PercentComplete * sizeMap[j]), downloadSize);
#if UNITY_EDITOR
                        Debug.Log(string.Format("Download Size: {0}/{1}", size + (long)(downloadHandle.PercentComplete * sizeMap[j]), downloadSize));
#endif
                        yield return null;
                    }
                    size += sizeMap[j];
                    Addressables.Release(downloadHandle);
                }
                j++;
            }
        }
        OnDownloadCompleted?.Invoke(downloadSize);
        yield return StartCoroutine(AfterDownloadHandle?.Invoke());
    }

    private string[] GetCatalogs(string path, string suffix, SearchOption searchOption)
    {
        return Directory.GetFiles(path, "catalog_*." + suffix, searchOption);
    }

    private string[] GetCatalogs(string path, SearchOption searchOption)
    {
        var hashs = GetCatalogs(path, "hash", searchOption);
        var jsons = GetCatalogs(path, "json", searchOption);
        var result = new string[hashs.Length + jsons.Length];
        for (int i = 0; i < hashs.Length; i++)
        {
            result[i] = hashs[i];
        }
        for (int j = 0; j < jsons.Length; j++)
        {
            result[hashs.Length + j] = jsons[j];
        }
        return result;
    }

    private void SaveCatalogs()
    {
        if (!Directory.Exists(addressablesDir)) { return; }
        var catalogs = GetCatalogs(addressablesDir, SearchOption.TopDirectoryOnly);
        if (!Directory.Exists(backupDir)) { Directory.CreateDirectory(backupDir); }
        for (int i = 0; i < catalogs.Length; i++)
        {
            File.Copy(catalogs[i], catalogs[i].Replace(addressablesFolder, backupFolder));
        }
    }

    private void LoadCatalogs()
    {
        if (!Directory.Exists(addressablesDir)) { return; }
        var catalogs = GetCatalogs(addressablesDir, SearchOption.TopDirectoryOnly);
        for (int i = 0; i < catalogs.Length; i++)
        {
            File.Delete(catalogs[i]);
        }
        if (!Directory.Exists(backupDir)) { return; }
        catalogs = GetCatalogs(backupDir, SearchOption.TopDirectoryOnly);
        for (int i = 0; i < catalogs.Length; i++)
        {
            File.Move(catalogs[i], catalogs[i].Replace(backupFolder, addressablesFolder));
        }
    }

    private void DeleteCatalogs()
    {
        if (!Directory.Exists(backupDir)) { return; }
        var catalogs = GetCatalogs(backupDir, SearchOption.TopDirectoryOnly);
        for (int i = 0; i < catalogs.Length; i++)
        {
            File.Delete(catalogs[i]);
        }
    }
}
