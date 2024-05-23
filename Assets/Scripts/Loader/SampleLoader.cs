using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace STak4.AssetCICD.Loader
{
    public class SampleLoader : MonoBehaviour
    {
        [SerializeField] private AssetReference _assetReference;
        [SerializeField] private string _key;

        [SerializeField] private RawImage _target;
        
        // Start is called before the first frame update
        async void Start()
        {
            var tex2D = await Addressables.LoadAssetAsync<Texture2D>(_key).Task;
            _target.texture = tex2D;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
