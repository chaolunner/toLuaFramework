using System.Collections;
using UnityEngine;

public class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager instance;
    private static bool isDisposed;

    public static CoroutineManager Instance
    {
        get
        {
            if (instance == null && !isDisposed)
            {
                instance = new GameObject("CoroutineManager").AddComponent<CoroutineManager>();
            }
            return instance;
        }
    }

    private void OnDestroy()
    {
        isDisposed = true;
    }

    private void OnApplicationQuit()
    {
        isDisposed = true;
    }

    public static Coroutine DoCoroutine(IEnumerator routine)
    {
        return Instance.StartCoroutine(routine);
    }
}
