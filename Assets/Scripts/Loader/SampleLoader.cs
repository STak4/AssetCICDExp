using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;
using Task = System.Threading.Tasks.Task;

namespace STak4.AssetCICD.Loader
{
    public class SampleLoader : MonoBehaviour
    {
        [SerializeField] private string _label;
        [SerializeField] private string _key;

        [SerializeField] private RawImage _target;
        
        // Start is called before the first frame update
        void Start()
        {
            Download();
        }

        public async void Download()
        {
            // ラベルと型からResourceLocationを取得
            var all = await Addressables.LoadResourceLocationsAsync(_label, typeof(Texture2D)).Task;
            
            // ダウンロード用のハンドル
            var downloadHandles = new List<AsyncOperationHandle>();
            
            // 各バンドルのサイズ(Bytes）
            var bundleSizes = new List<long>();
            
            // 経過時間計測用
            var start = DateTime.Now;
            
            // DependencyHashCodeでグルーピングをし、アセットバンドル単位でダウンロードする
            // https://blog.gigacreation.jp/entry/2021/12/28/195920
            foreach (IGrouping<int, IResourceLocation> groupedLocations in all.GroupBy(x => x.DependencyHashCode))
            {
                var locations = groupedLocations.ToList();
                
                // GetDownloadSizeAsyncでダウンロード容量を取得する。
                // キャッシュ済みの場合常に0になる（LoadPathがリモートでない場合0になる）
                // https://baba-s.hatenablog.com/entry/2020/03/19/033000
                var size = Addressables.GetDownloadSizeAsync(locations);
                size.Completed += op =>
                {
                    bundleSizes.Add(op.Result);
                    Debug.Log($"Locations[{groupedLocations.Key}]: Download start. size:{ConvertFileSize(op.Result)}");
                    Addressables.Release(size);
                };
                
                // グルーピングされたLocationをもとにアセットバンドルファイル単位でダウンロードする
                Debug.Log($"Locations[{groupedLocations.Key}]: Download start. total files:{locations.Count}");
                var download = Addressables.DownloadDependenciesAsync(locations);
                download.Completed += (op =>
                {
                    Debug.Log(
                        $"Locations[{groupedLocations.Key}]: Download Complete. total files:{locations.Count}");
                    // 本来はダウンロードを完了したらReleaseするが、この後進捗表示用に使用するためここではReleaseしない
                    // Releaseしないとロード時にSystem.Exception: Unable to load dependent bundle from location Download Dependenciesになる
                    //Addressables.Release(download);
                });
                downloadHandles.Add(download);
            }
            
            // 個別のダウンロード状況（サイズ）を表示する
            var groupHandle = Addressables.ResourceManager.CreateGenericGroupOperation(downloadHandles);
            while (!groupHandle.IsDone)
            {
                for (int i = 0; i < bundleSizes.Count; i++)
                {
                    Debug.Log($"[Bundle{i}]: {ConvertFileSize(downloadHandles[i].GetDownloadStatus().DownloadedBytes)}/{ConvertFileSize(bundleSizes[i])}");
                }
                
                // 全体の進捗率（パーセンテージ）
                // 微妙に扱いにくいのでサイズの表示を推奨
                //Debug.Log($"Overall progress:{groupHandle.PercentComplete}");
                await Task.Delay(100);
            }
            
            // ダウンロードしたファイルを使用する前に全てReleaseする
            Addressables.Release(groupHandle);
            Load();
            
            // 経過時間用
            var elapsed = DateTime.Now - start;
            Debug.Log($"[Debug] Elapsed time:{elapsed.TotalSeconds:F2}");
        }

        public async void Load()
        {
            var tex2D = await Addressables.LoadAssetAsync<Texture2D>(_key).Task;
            _target.texture = tex2D;
        }

        public void ClearCache()
        {
            _target.texture = null;
            
            // $MEMO: ランタイム実行時、アセットバンドルを先にアンロードしないと機能しない
            AssetBundle.UnloadAllAssetBundles(true);
            var result = Caching.ClearCache();
            Debug.Log($"Clear Cache result:{result}");
        }
        
        /// <summary>
        /// 個人的に好きなBytes->MB等ファイルサイズ表示の実装
        /// https://stackoverflow.com/a/62698159
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ConvertFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }
    }
}
