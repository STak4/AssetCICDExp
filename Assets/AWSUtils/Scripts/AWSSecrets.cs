using System.IO;
using Amazon;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace AWSUtils
{
    /// <summary>
    /// AWS認証情報を扱うクラス
    /// </summary>
    public class AwsSecrets
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        
        public string BucketName { get; set; }
        
        public string RegionName { get; set; }
        //public RegionEndpoint Region { get; set; }
        
        public string DistrubutionId { get; set; }

        public static readonly string DefaultPath = Application.persistentDataPath + "/AWSSECRETS.json";
        public static AwsSecrets Load(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"Cannot find json file in specified path: {jsonFilePath}");
            }
            
            var json = File.ReadAllText(jsonFilePath);
            var secrets = JsonConvert.DeserializeObject<AwsSecrets>(json);
            return secrets;
        }

        /// <summary>
        /// TODO: 認証情報の暗号化（ローカル保存のため現在は考慮しない）
        /// </summary>
        /// <param name="secrets"></param>
        /// <param name="jsonFilePath"></param>
        public static void Save(AwsSecrets secrets, string jsonFilePath)
        {
            var json = JsonConvert.SerializeObject(secrets);
            File.WriteAllText(jsonFilePath, json);
        }
    }
}