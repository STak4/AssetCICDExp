#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEditor;
using UnityEngine;

namespace S3Tools
{
    public class S3UploaderWindow : EditorWindow
    {
        // 別スクリプトから設定値を参照できるように公開する

        [SerializeField] private string _accessKey;
        [SerializeField] private string _secretKey;
        [SerializeField] private string _bucket;

        [SerializeField] private bool _isClearCache = false;
        [SerializeField] private string _cloudFrontDistributionId;
        [SerializeField] private string _clearPath;
        
        private string _directoryPath;
        
        private UnityEngine.GUIContent[] _popupDisplayOptions;
        private int _popupIndex;
        
        private SerializedObject m_SerializedObject = null;
        
        [MenuItem("S3/UploadWindow")]
        private static void Open()
        {
            GetWindow<S3UploaderWindow>();
        }

        private void OnEnable()
        {
            m_SerializedObject = new SerializedObject(this);
            _popupDisplayOptions = new[]
            {
                new UnityEngine.GUIContent("ap-northeast-1"),
                new UnityEngine.GUIContent("ap-northeast-2"),
                new UnityEngine.GUIContent("ap-northeast-3"),
                new UnityEngine.GUIContent("ap-south-1"),
                new UnityEngine.GUIContent("ap-south-2"),
                new UnityEngine.GUIContent("ap-southeast-1"),
                new UnityEngine.GUIContent("ap-southeast-2"),
                new UnityEngine.GUIContent("ap-southeast-3"),
                new UnityEngine.GUIContent("ap-southeast-4"),
            };
        }

        private void Update()
        {
            Repaint(); // 毎フレーム内容が更新されるようにする
        }
        
        private async void OnGUI()
        {
            m_SerializedObject.Update();

            // プロパティを表示して編集可能にする
            EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_accessKey)}"));
            EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_secretKey)}"));

            _popupIndex = EditorGUILayout.Popup(
                label: new GUIContent("Region"),
                selectedIndex: _popupIndex,
                displayedOptions: _popupDisplayOptions
            );

            EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_bucket)}"));

           _isClearCache = EditorGUILayout.Toggle("Clear Cache?", _isClearCache);
            if (_isClearCache)
            {
                EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_cloudFrontDistributionId)}"));
                EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_clearPath)}"));
            }

            // ボタンで実行
            if (GUILayout.Button("アップロードするフォルダを選択"))
            {
                _directoryPath = EditorUtility.OpenFolderPanel("アップロードするディレクトリ", "", "");
            }
            

            
            m_SerializedObject.ApplyModifiedProperties();

            // ボタンでアップロード実行
            if (GUILayout.Button("Upload"))
            {
                if (!(string.IsNullOrEmpty(_accessKey) || string.IsNullOrEmpty(_secretKey) ||
                    string.IsNullOrEmpty(_bucket) || string.IsNullOrEmpty(_directoryPath)))
                {
                    var region = RegionEndpoint.GetBySystemName(_popupDisplayOptions[_popupIndex].text);
                    Debug.Log($"[S3Uplader]Region:{region}");

                    // S3へのアップロード
                    var s3 = new AmazonS3Client(_accessKey, _secretKey, region);
                    await S3Uploader.UploadFolder(s3, _bucket, _directoryPath);
                    
                    // キャッシュ削除する場合アップロード後削除
                    if (_isClearCache && !string.IsNullOrEmpty(_cloudFrontDistributionId) &&
                        !string.IsNullOrEmpty(_clearPath))
                    {
                        using (var client = new AmazonCloudFrontClient(_accessKey, _secretKey, region))
                        {
                            // 削除中出ないことを確認する
                            if(!await CloudFrontUtils.IsInvalidationProgress(client, _cloudFrontDistributionId))
                            {
                                await CloudFrontUtils.CreateInvalidationAsync(client, _cloudFrontDistributionId, _clearPath);

                            }
                            else
                            {
                                Debug.LogError("キャッシュ削除実施中。しばらくしてから実行してください");
                            }
                            return;
                        }
                    }
                }
            }

            // キャッシュだけ消したい場合
            if (GUILayout.Button("Clear Cache Only"))
            {
                if (_isClearCache && !string.IsNullOrEmpty(_cloudFrontDistributionId) &&
                    !string.IsNullOrEmpty(_clearPath))
                {
                    var region = RegionEndpoint.GetBySystemName(_popupDisplayOptions[_popupIndex].text);
                    using (var client = new AmazonCloudFrontClient(_accessKey, _secretKey, region))
                    {
                        if(!await CloudFrontUtils.IsInvalidationProgress(client, _cloudFrontDistributionId))
                        {
                            await CloudFrontUtils.CreateInvalidationAsync(client, _cloudFrontDistributionId, _clearPath);
                        }
                        else
                        {
                            Debug.LogError("キャッシュ削除実施中。しばらくしてから実行してください");
                        }
                    }
                }
                else
                {
                    Debug.LogError("認証情報が不足しています");
                }
            }
        }

    }
    
    public static class S3Uploader
    {
        public static async Task UploadFolder(IAmazonS3 client, string bucketName, string folderPath)
        {
            Debug.Log($"[S3Uploader]Path: {folderPath}");
            var files = GetAllFiles(folderPath);

            foreach (var file in files)
            {
                Debug.Log($"[S3Uploader]Uploading... {file}");
                await client.PutObjectAsync(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = Path.GetFileName(file),
                    FilePath = file
                });
                Debug.Log($"[S3Uploader]Uploaded!");
            }
        }
        
        // ディレクトリ内のすべてのファイルパスを取得する関数
        public static string[] GetAllFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        }
    }

    public static class CloudFrontUtils
    {
        /// <summary>
        /// キャッシュ削除を実行する
        /// </summary>
        /// <param name="client">CloudFrontClient</param>
        /// <param name="distributionId">CloudFront配信のID</param>
        /// <param name="itemPath">削除するパス(/*で残削除）</param>
        public static async Task<string> CreateInvalidationAsync(AmazonCloudFrontClient client, string distributionId, string itemPath)
        {
            Debug.Log($"[CloudFront][CreateInvalidation] キャッシュ削除開始");
            var invalidationBatch = new InvalidationBatch
            {
                CallerReference = DateTime.Now.Ticks.ToString(), // 現在の時間をナノ秒単位で表したものを使用します(他の無効化リクエストと同じにならないようにします)
                Paths = new Paths
                {
                    Quantity = 1, // 無効化するパスの数です
                    Items = new List<string> { itemPath } // 無効化するパスのリストです
                }
            };

            var request = new CreateInvalidationRequest
            {
                DistributionId = distributionId, // CloudFront配信のIDを入力してください
                InvalidationBatch = invalidationBatch
            };

            var response = await client.CreateInvalidationAsync(request); 
            if (response.HttpStatusCode == System.Net.HttpStatusCode.Created)
            {
                Debug.Log($"[CloudFront][CreateInvalidation] キャッシュ削除リクエスト完了. ID:{response.Invalidation.Id}");
                return response.Invalidation.Id;
            }
            
            return string.Empty;
        }

        /// <summary>
        /// キャッシュ削除中でないことを確認する
        /// </summary>
        /// <param name="client">CloudFrontClient</param>
        /// <param name="distributionId">CloudFront配信のID</param>
        public static async Task<bool> IsInvalidationProgress(AmazonCloudFrontClient client, string distributionId)
        {
            Debug.Log($"[CloudFront][CreateInvalidation] キャッシュ削除ステータス確認.");
            
            var request = new ListInvalidationsRequest()
            {
                DistributionId = distributionId
            };

            var response = await client.ListInvalidationsAsync(request);

            int inProgress = 0;
            foreach (var item in response.InvalidationList.Items)
            {
                if (item.Status == "InProgress")
                {
                    inProgress++;
                }
            }

            Debug.Log($"[CloudFront][CreateInvalidation] キャッシュ削除進行中か？ {inProgress > 0}");
            return inProgress > 0;
        }
    }
}

#endif
