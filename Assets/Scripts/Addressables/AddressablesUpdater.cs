using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.IO;
using System;

public enum RequestDownloadResult
{
    None,
    Agree,
    Disagree,
}

public class AddressablesUpdater : MonoBehaviour
{
    public static RequestDownloadResult Result;
    public static event Action OnCompleted;
    public static event Action<long, long> OnDownload;
    public static event Action<long> OnDownloadCompleted;
    public static Func<IEnumerator> AfterDownloadHandle;
    public static Func<long, IEnumerator> RequestDownloadHandle;

    private long downloadSize = 0;
    private IList<IResourceLocator> updateLocators;
    private Dictionary<int, long> sizeMap = new Dictionary<int, long>();
    private Dictionary<string, long> bundleSizeMap = new Dictionary<string, long>();

    private static string addressablesDir = Application.persistentDataPath + "/" + addressablesFolder;
    private static string originalDir = Application.persistentDataPath + "/" + originalFolder;
    private const string addressablesFolder = "com.unity.addressables";
    private const string originalFolder = "original.addressables";

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
        SaveOriginalCatalogs();
        yield return StartCoroutine(InitializeBundleSizeMapAsync());
        yield return StartCoroutine(UpdateCatalogsAsync(catalogs));
        CalculateUpdateSize();
        if (downloadSize <= 0) { yield break; }
        yield return StartCoroutine(RequestDownloadHandle?.Invoke(downloadSize));
        if (Result == RequestDownloadResult.Disagree)
        {
            yield return StartCoroutine(CancelDownloadAsync());
            yield break;
        }
        DeleteOriginalCatalogs();
        yield return StartCoroutine(DownloadAsync());
        OnDownloadCompleted?.Invoke(downloadSize);
        yield return StartCoroutine(AfterDownloadHandle?.Invoke());
    }

    private void OnDestroy()
    {
        LoadOriginalCatalogs();
    }

    private IEnumerator InitializeBundleSizeMapAsync()
    {
        bundleSizeMap.Clear();
        if (!Directory.Exists(addressablesDir)) { yield break; }
        var catalogPaths = GetCatalogs(addressablesDir, "json", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < catalogPaths.Length; i++)
        {
            var catalogHandle = Addressables.LoadContentCatalogAsync(catalogPaths[i]);
            yield return catalogHandle;
            IList<IResourceLocation> locations;
            var locator = catalogHandle.Result;
            var e = locator.Keys.GetEnumerator();
            while (e.MoveNext())
            {
                if (locator.Locate(e.Current, typeof(object), out locations))
                {
                    for (int k = 0; k < locations.Count; k++)
                    {
                        if (bundleSizeMap.ContainsKey(locations[k].InternalId)) { continue; }
                        if (ResourceManagerConfig.IsPathRemote(locations[k].InternalId) && locations[k].Data != null && locations[k].Data is AssetBundleRequestOptions)
                        {
                            bundleSizeMap.Add(locations[k].InternalId, 0);
                        }
                    }
                }
            }
            Addressables.Release(catalogHandle);
        }
    }

    private IEnumerator UpdateCatalogsAsync(List<string> catalogs)
    {
        var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
        yield return updateHandle;
        downloadSize = 0;
        sizeMap = new Dictionary<int, long>();
        updateLocators = updateHandle.Result;
        Addressables.Release(updateHandle);
    }

    private void CalculateUpdateSize()
    {
        for (int i = 0; i < updateLocators.Count; i++)
        {
            IList<IResourceLocation> locations;
            var e = updateLocators[i].Keys.GetEnumerator();
            var index = 0;
            while (e.MoveNext())
            {
                long size = 0;
                if (updateLocators[i].Locate(e.Current, typeof(object), out locations))
                {
                    for (int k = 0; k < locations.Count; k++)
                    {
                        if (bundleSizeMap.ContainsKey(locations[k].InternalId)) { continue; }
                        if (ResourceManagerConfig.IsPathRemote(locations[k].InternalId) && locations[k].Data != null && locations[k].Data is AssetBundleRequestOptions)
                        {
                            long bundleSize = (locations[k].Data as AssetBundleRequestOptions).BundleSize;
                            size += bundleSize;
                            bundleSizeMap.Add(locations[k].InternalId, bundleSize);
                        }
                    }
                }
                sizeMap.Add(index, size);
                downloadSize += size;
                index++;
            }
        }
    }

    private IEnumerator CancelDownloadAsync()
    {
        LoadOriginalCatalogs();
        for (int i = 0; i < updateLocators.Count; i++)
        {
            Addressables.RemoveResourceLocator(updateLocators[i]);
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
    }

    private IEnumerator DownloadAsync()
    {
        long size = 0;
        for (int i = 0; i < updateLocators.Count; i++)
        {
            IList<IResourceLocation> locations;
            var e = updateLocators[i].Keys.GetEnumerator();
            var index = 0;
            while (e.MoveNext())
            {
                if (updateLocators[i].Locate(e.Current, typeof(object), out locations))
                {
                    var downloadHandle = Addressables.DownloadDependenciesAsync(locations);
                    while (downloadHandle.PercentComplete < 1 && !downloadHandle.IsDone)
                    {
                        OnDownload?.Invoke(size + (long)(downloadHandle.PercentComplete * sizeMap[index]), downloadSize);
#if UNITY_EDITOR
                        Debug.Log(string.Format("Download Size: {0}/{1}", size + (long)(downloadHandle.PercentComplete * sizeMap[index]), downloadSize));
#endif
                        yield return null;
                    }
                    size += sizeMap[index];
                    Addressables.Release(downloadHandle);
                }
                index++;
            }
        }
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

    private void SaveOriginalCatalogs()
    {
        if (!Directory.Exists(addressablesDir)) { return; }
        var catalogs = GetCatalogs(addressablesDir, SearchOption.TopDirectoryOnly);
        if (!Directory.Exists(originalDir)) { Directory.CreateDirectory(originalDir); }
        for (int i = 0; i < catalogs.Length; i++)
        {
            File.Copy(catalogs[i], catalogs[i].Replace(addressablesFolder, originalFolder));
        }
    }

    private void LoadOriginalCatalogs()
    {
        if (!Directory.Exists(addressablesDir) || !Directory.Exists(originalDir)) { return; }
        var catalogs = GetCatalogs(addressablesDir, SearchOption.TopDirectoryOnly);
        for (int i = 0; i < catalogs.Length; i++)
        {
            File.Delete(catalogs[i]);
        }
        catalogs = GetCatalogs(originalDir, SearchOption.TopDirectoryOnly);
        for (int i = 0; i < catalogs.Length; i++)
        {
            File.Move(catalogs[i], catalogs[i].Replace(originalFolder, addressablesFolder));
        }
        DeleteOriginalCatalogs();
    }

    private void DeleteOriginalCatalogs()
    {
        if (Directory.Exists(originalDir))
        {
            Directory.Delete(originalDir, true);
        }
    }
}
