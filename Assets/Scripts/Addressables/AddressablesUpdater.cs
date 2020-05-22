using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class AddressablesUpdater : MonoBehaviour
{
    public static AddressablesUpdater Instance { private set; get; }

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        new GameObject("AddressablesUpdater").AddComponent<AddressablesUpdater>();
    }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(DownloadUpdateAsync());
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
    }
}
