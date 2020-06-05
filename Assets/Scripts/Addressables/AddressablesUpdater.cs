using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.AddressableAssets.ResourceLocators;
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
    private IList<object> updateKeys;

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
        Addressables.Release(initHandle);
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;
        var catalogs = checkHandle.Result;
        Addressables.Release(checkHandle);
        if (catalogs == null || catalogs.Count <= 0) { yield break; }
        SaveOriginalCatalogs();
        yield return StartCoroutine(UpdateCatalogsAsync(catalogs));
        yield return StartCoroutine(CalculateUpdateSizeAsync());
        if (downloadSize <= 0) { yield break; }
        yield return StartCoroutine(RequestDownloadHandle?.Invoke(downloadSize));
        if (Result == RequestDownloadResult.Agree) { DeleteOriginalCatalogs(); }
        else { CancelDownload(); }
        yield return StartCoroutine(UpdateResourceLocatorsAsync());
        if (Result == RequestDownloadResult.Disagree) { yield break; }
        yield return StartCoroutine(DownloadAsync());
        OnDownloadCompleted?.Invoke(downloadSize);
        yield return StartCoroutine(AfterDownloadHandle?.Invoke());
    }

    private void OnDestroy()
    {
        LoadOriginalCatalogs();
    }

    private IEnumerator UpdateCatalogsAsync(List<string> catalogs)
    {
        var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
        yield return updateHandle;
        downloadSize = 0;
        updateKeys = new List<object>();
        updateLocators = updateHandle.Result;
        Addressables.Release(updateHandle);
    }

    private IEnumerator CalculateUpdateSizeAsync()
    {
        for (int i = 0; i < updateLocators.Count; i++)
        {
            IList<IResourceLocation> locations;
            var e = updateLocators[i].Keys.GetEnumerator();
            while (e.MoveNext())
            {
                if (updateLocators[i].Locate(e.Current, typeof(object), out locations))
                {
                    for (int j = 0; j < locations.Count; j++)
                    {
                        if (updateKeys.Contains(locations[j].PrimaryKey)) { continue; }
                        updateKeys.Add(locations[j].PrimaryKey);
                    }
                }
            }
        }
        var downloadSizeHandle = Addressables.GetDownloadSizeAsync(updateKeys);
        yield return downloadSizeHandle;
        downloadSize = downloadSizeHandle.Result;
        Addressables.Release(downloadSizeHandle);
    }

    private void CancelDownload()
    {
        LoadOriginalCatalogs();
#if UNITY_EDITOR
        Debug.Log(string.Format("Cancel Download Size: {0}", downloadSize));
#endif
    }

    private IEnumerator DownloadAsync()
    {
        long downloadedSize = 0;
        for (int i = 0; i < updateKeys.Count; i++)
        {
            var downloadSizeHandle = Addressables.GetDownloadSizeAsync(updateKeys[i]);
            yield return downloadSizeHandle;
            long size = downloadSizeHandle.Result;
            Addressables.Release(downloadSizeHandle);
            if (size <= 0) { continue; }

            var downloadHandle = Addressables.DownloadDependenciesAsync(updateKeys[i]);
            while (downloadHandle.PercentComplete < 1 && !downloadHandle.IsDone)
            {
                OnDownload?.Invoke(downloadedSize + (long)(downloadHandle.PercentComplete * size), downloadSize);
#if UNITY_EDITOR
                Debug.Log(string.Format("Download Size: {0}/{1}", downloadedSize + (long)(downloadHandle.PercentComplete * size), downloadSize));
#endif
                yield return null;
            }
            downloadedSize += size;
            Addressables.Release(downloadHandle);
            if (downloadedSize >= downloadSize) { break; }
        }
    }

    private string[] GetCatalogs(string path, string suffix, SearchOption searchOption)
    {
        return Directory.GetFiles(path, "catalog*." + suffix, searchOption);
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

    private IEnumerator UpdateResourceLocatorsAsync()
    {
        Addressables.ClearResourceLocators();

        if (Directory.Exists(addressablesDir))
        {
            string[] catalogs = GetCatalogs(addressablesDir, "json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < catalogs.Length; i++)
            {
                var catalogHandle = Addressables.LoadContentCatalogAsync(catalogs[i]);
                yield return catalogHandle;
                Addressables.AddResourceLocator(catalogHandle.Result);
                Addressables.Release(catalogHandle);
            }
        }
        else
        {
            string[] catalogs = GetCatalogs(Addressables.RuntimePath, "json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < catalogs.Length; i++)
            {
                var catalogHandle = Addressables.LoadContentCatalogAsync(catalogs[i]);
                yield return catalogHandle;
                Addressables.AddResourceLocator(catalogHandle.Result);
                Addressables.Release(catalogHandle);
            }
        }
    }

    private void SaveOriginalCatalogs()
    {
        if (!Directory.Exists(originalDir)) { Directory.CreateDirectory(originalDir); }
        if (!Directory.Exists(addressablesDir)) { return; }
        var catalogs = GetCatalogs(addressablesDir, SearchOption.TopDirectoryOnly);
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
        if (Directory.Exists(originalDir)) { Directory.Delete(originalDir, true); }
    }
}
