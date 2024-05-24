using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

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

        // Update is called once per frame
        void Update()
        {
        
        }

        public async void Download()
        {
            var all = await Addressables.LoadResourceLocationsAsync(_label, typeof(Texture2D)).Task;
            
            foreach (var location in all)
            {
                Addressables.LoadAssetAsync<Texture2D>(location).Completed += (op =>
                {
                    Debug.Log($"Location:{location.PrimaryKey},  FileName:{op.Result.name} loaded");
                });
            }
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
    }
}
