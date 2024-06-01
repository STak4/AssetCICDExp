using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;

namespace STak4.AssetCICD.Editor
{
    [InitializeOnLoad]
    public class PlatformUtils : IActiveBuildTargetChanged
    {
        public static (BuildTargetGroup, BuildTarget)[] BuildOrder = new[]
        {
            (BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64),
            (BuildTargetGroup.Android, BuildTarget.Android),
            (BuildTargetGroup.iOS, BuildTarget.iOS)
        };

        private static readonly string isReservedKey = "IsReserved";
        private static readonly string buildGroupKey = "BuildGroup";
        private static readonly string buildTargetKey = "BuildTarget";
        private static readonly string buildAppKey = "BuildApp";
        private static readonly string buildAddressableKey = "BuildAddressable";
        private static readonly string buildAddressableAllKey = "BuildAddressableAll";
        private static readonly string buildOrderIndexKey = "BuildOrderIndex";
        
        static PlatformUtils()
        {
            
        }

        /// <summary>
        /// プラットフォームが切り替わった時に呼ばれる
        /// https://docs.unity3d.com/ja/2021.2/ScriptReference/Build.IActiveBuildTargetChanged.OnActiveBuildTargetChanged.html
        /// </summary>
        public int callbackOrder { get {return 0;}}
        public async void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            Debug.Log($"[PlatformUtils] Platform changed from {previousTarget} to {newTarget}");
            
            // 関係ない切替は除外
            if (PlayerPrefs.GetInt(isReservedKey) != 1) return;
            
            // TODO: 正しくSwitchPlatformが終了したことを検知する方法
            // 以下の方法は機能しない
            // while (EditorApplication.isCompiling || EditorApplication.isUpdating)
            // {
            //     await Task.Delay(100);
            // }

            await Task.Delay(1000);
            
            // 目的のプラットフォームかどうか
            var target = Enum.Parse<BuildTarget>(PlayerPrefs.GetString(buildTargetKey));
            if (target != newTarget)
            {
                Debug.Log($"[PlatformUtils] Not target");
                return;
            }
            
            Debug.Log($"[PlatformUtils] Detect reserved platform. Checking build or not...");

            if (PlayerPrefs.GetInt(buildAppKey) == 1)
            {
                BuildApp();
            }

            if (PlayerPrefs.GetInt(buildAddressableKey) == 1)
            {
                BuildAddressable();
            }
            
            if(PlayerPrefs.GetInt(buildAddressableAllKey) != 1) ClearAllKeys();
        }

        public static async void BuildAddressableAll()
        {
            var progress = ProgressWindow.ShowWindow("Build Addressable");
            progress.SetProgress(0, $"Building... : {BuildOrder[0]}");
            // ウィンドウが表示される前に切り替えると真っ白になる
            await Task.Delay(10);
            PlayerPrefs.SetInt(buildAddressableAllKey, 1);
            PlayerPrefs.SetInt(buildOrderIndexKey, 0);
            
            SwitchPlatform(BuildOrder[0].Item1, BuildOrder[0].Item2, false, true);
        }

        public static void SwitchPlatform(BuildTargetGroup group, BuildTarget target, bool buildApp = false, bool buildAddressable = false)
        {
            // 切替不要
            if (target == EditorUserBuildSettings.activeBuildTarget)
            {
                if (buildApp) BuildApp();
                if(buildAddressable) BuildAddressable();
                return;
            }
            
            PlayerPrefs.SetInt(isReservedKey, 1);
            PlayerPrefs.SetString(buildGroupKey, group.ToString());
            PlayerPrefs.SetString(buildTargetKey, target.ToString());
            PlayerPrefs.SetInt(buildAppKey, buildApp ? 1 : 0);
            PlayerPrefs.SetInt(buildAddressableKey, buildAddressable ? 1 : 0);
            
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(group, target);
            Debug.Log($"[PlatformUtils] Platform target:{target}");
        }

        public static void BuildApp()
        {
            Debug.Log($"[PlatformUtils] Start build");
        }

        public static void BuildAddressable()
        {
            Debug.Log($"[PlatformUtils] Start addressable build");
            AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent();
            
            Debug.Log($"[PlatformUtils] Complete addressable build");
            CheckCompleted();
        }

        private static void CheckCompleted()
        {
            if (PlayerPrefs.GetInt(buildAddressableAllKey) == 1)
            {
                var progress = ProgressWindow.ShowWindow("Build Addressable");
                var index = PlayerPrefs.GetInt(buildOrderIndexKey);
                if (index >= BuildOrder.Length - 1)
                {
                    Debug.Log($"[PlatformUtils][BuildAll] Complete build all");

                    progress.SetProgress(1, $"Complete");
                    progress.Close();
                    // キーをクリア
                    ClearAllKeys();
                }
                else
                {
                    Debug.Log($"[PlatformUtils][BuildAll] Go next");
                    // 次のビルドへ
                    index++;
                    progress.SetProgress((float)Math.Round((float)index / BuildOrder.Length,3), $"Building... : {BuildOrder[index]}");
                    PlayerPrefs.SetInt(buildOrderIndexKey, index);
                    SwitchPlatform(BuildOrder[index].Item1, BuildOrder[index].Item2, false, true);
                }
            }
            else
            {
                // TODO: Build App
            }
        }

        private static void ClearAllKeys()
        {
            PlayerPrefs.DeleteKey(isReservedKey);
            PlayerPrefs.DeleteKey(buildGroupKey);
            PlayerPrefs.DeleteKey(buildTargetKey);
            PlayerPrefs.DeleteKey(buildAppKey);
            PlayerPrefs.DeleteKey(buildAddressableKey);
            PlayerPrefs.DeleteKey(buildAddressableAllKey);
            PlayerPrefs.DeleteKey(buildOrderIndexKey);
        }
    }

}
