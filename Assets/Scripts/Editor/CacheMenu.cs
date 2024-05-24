#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace STak4.AssetCICD
{
    
    public class CacheMenu
    {
        [MenuItem("STak4/Cache/ClearAllCache")]
        public static void ClearAllCache()
        {
            Debug.Log("clear cache at " + Application.persistentDataPath);
            var list = Directory.GetDirectories(Application.persistentDataPath);
 
            // foreach (var item in list)
            // {
            //     Debug.Log("Delete" + " " + item);
            //     Directory.Delete(item, true);
            // }
 
            Caching.ClearCache();
        }
    }
}
#endif