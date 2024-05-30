#if UNITY_EDITOR
using Amazon;
using UnityEditor;
using UnityEngine;

namespace AWSUtils.Editor
{
    public class S3UploaderWindow : EditorWindow
    {
        [SerializeField] private bool _useSecrets = false;

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

        [MenuItem("AWS/S3UploadWindow")]
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
            _useSecrets = EditorGUILayout.Toggle("認証ファイルを使用した認証", _useSecrets);
            if (!_useSecrets)
            {
                EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_accessKey)}"));
                EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_secretKey)}"));

                EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_bucket)}"));
                _popupIndex = EditorGUILayout.Popup(
                    label: new GUIContent("Region"),
                    selectedIndex: _popupIndex,
                    displayedOptions: _popupDisplayOptions
                );
            }

            _isClearCache = EditorGUILayout.Toggle("CloudFrontのキャッシュ削除", _isClearCache);
            if (_isClearCache)
            {
                if (!_useSecrets)
                {
                    EditorGUILayout.PropertyField(m_SerializedObject.FindProperty($"{nameof(_cloudFrontDistributionId)}"));
                }
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
                    var key = _accessKey;
                    var secret = _secretKey;
                    var region = RegionEndpoint.GetBySystemName(_popupDisplayOptions[_popupIndex].text);
                    var bucket = _bucket;
                    // 認証ファイルを使って上書き
                    if (_useSecrets)
                    {
                        key = AwsSecrets.Auth.AccessKey;
                        secret = AwsSecrets.Auth.SecretKey;
                        region = AwsSecrets.Bucket.Region;
                        bucket = AwsSecrets.Bucket.BucketName;
                    }
                    
                    Debug.Log($"[S3Uplader]Region:{region}");

                    // S3へのアップロード
                    using (var s3 = S3Utils.CreateClient(key, secret, region))
                    {
                        await S3Utils.UploadFolder(s3, bucket, _directoryPath);
                    }

                    // キャッシュ削除する場合アップロード後削除
                    if (_isClearCache && !string.IsNullOrEmpty(_cloudFrontDistributionId) &&
                        !string.IsNullOrEmpty(_clearPath))
                    {
                        using (var client = AWSUtils.CloudFrontUtils.CreateClient(key, secret, region))
                        {
                            var dist = _cloudFrontDistributionId;
                            if (_useSecrets)
                            {
                                dist = AwsSecrets.CloudFront.DistrubutionId;
                            }
                            
                            // 削除中出ないことを確認する
                            if (!await CloudFrontUtils.IsInvalidationProgress(client, dist))
                            {
                                await CloudFrontUtils.CreateInvalidationAsync(client, dist, _clearPath);
                            }
                            else
                            {
                                Debug.LogError("キャッシュ削除実施中。しばらくしてから実行してください");
                            }
                        }

                        return;
                    }
                }
            }

            // キャッシュだけ消したい場合
            if (GUILayout.Button("キャッシュ削除のみ"))
            {
                if (_isClearCache && !string.IsNullOrEmpty(_cloudFrontDistributionId) &&
                    !string.IsNullOrEmpty(_clearPath))
                {
                    var key = _accessKey;
                    var secret = _secretKey;
                    var region = RegionEndpoint.GetBySystemName(_popupDisplayOptions[_popupIndex].text);
                    var dist = _cloudFrontDistributionId;
                    // 認証ファイルを使って上書き
                    if (_useSecrets)
                    {
                        key = AwsSecrets.Auth.AccessKey;
                        secret = AwsSecrets.Auth.SecretKey;
                        region = AwsSecrets.Bucket.Region;
                        dist = AwsSecrets.CloudFront.DistrubutionId;
                    }
                    
                    using (var client = CloudFrontUtils.CreateClient(key, secret, region))
                    {
                        if (!await CloudFrontUtils.IsInvalidationProgress(client, dist))
                        {
                            await CloudFrontUtils.CreateInvalidationAsync(client, dist, _clearPath);
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
}

#endif