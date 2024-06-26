using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
            Download().Forget();
        }

        public async UniTaskVoid Download()
        {
            // ラベルと型からResourceLocationを取得
            var all = await Addressables.LoadResourceLocationsAsync(_label, typeof(Texture2D)).Task;
            
            // 経過時間計測用
            var start = DateTime.Now;
            
            Debug.Log($"[Debug] Download called. locations:{all.Count}");
            // DependencyHashCodeでグルーピングをし、アセットバンドル単位でダウンロードする
            // https://blog.gigacreation.jp/entry/2021/12/28/195920
            // 直列ダウンロード
            foreach (IGrouping<int, IResourceLocation> groupedLocations in all.GroupBy(x => x.DependencyHashCode))
            {
                var locations = groupedLocations.ToList();
                
                // GetDownloadSizeAsyncでダウンロード容量を取得する。
                // キャッシュ済みの場合常に0になる（LoadPathがリモートでない場合0になる）
                // https://baba-s.hatenablog.com/entry/2020/03/19/033000
                var size = await Addressables.GetDownloadSizeAsync(locations).ToUniTask(cancellationToken: default);
                Debug.Log($"Locations[{groupedLocations.Key}]: Download start. size:{ConvertFileSize(size)}, files:{locations.Count}");
                
                // グルーピングされたLocationをもとにアセットバンドルファイル単位でダウンロードする
                var handle = Addressables.DownloadDependenciesAsync(locations);
                try
                {
                    // $MEMO: AddressableのProgress（％表示はそのままだと扱いにくいのでサイズで計算したほうが良い
                    await handle.ToUniTask(Progress.Create<float>(_ => GetDownloadProgress(handle, size)) , cancellationToken: destroyCancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    Debug.Log($"Locations[{groupedLocations.Key}]: Download Canceled. {e.Message}");
                    Addressables.Release(handle);
                    return;
                }

                Debug.Log($"Locations[{groupedLocations.Key}]: Download Complete.");
                // autoReleaseHandle = trueにしても良い
                Addressables.Release(handle);
            }
            await Load();
            
            // 経過時間用
            var elapsed = DateTime.Now - start;
            Debug.Log($"[Debug] Elapsed time:{elapsed.TotalSeconds:F2}");
        }

        // 進捗表示
        private void GetDownloadProgress(AsyncOperationHandle handle, long total)
        {
            var current = handle.GetDownloadStatus().DownloadedBytes;
            Debug.Log($"[Progress] {ConvertFileSize(current)}/{ConvertFileSize(total)}");
        }

        public async UniTask Load()
        {
            var tex2D = await Addressables.LoadAssetAsync<Texture2D>(_key).ToUniTask(cancellationToken: destroyCancellationToken);
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
