
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;

namespace STak4.AssetCICD.Editor
{
    public static class AddressableBuildMenu
    {
        [MenuItem("STak4/Addressable/Build All Platforms")]
        public static void BuildAllPlatforms()
        {
            Debug.ClearDeveloperConsole();
            Debug.Log($"[Build][Addressable] Build All");
            PlatformUtils.BuildAddressableAll();
        }
    }
}
