using Amazon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace AWSUtils.Editor
{
    public class S3UploaderWindow : EditorWindow
    {
        [SerializeField] private bool _loadSecrets = false;
        [SerializeField] private bool _saveSecrets = false;
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
            _loadSecrets = EditorGUILayout.Toggle("認証ファイルを使用した認証", _loadSecrets);
            
            // 認証ファイルを使用しない場合手動入力（保存されない）
            if (!_loadSecrets)
            {
                _saveSecrets = EditorGUILayout.Toggle("認証情報の自動保存", _saveSecrets);
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
                if (!_loadSecrets)
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
                    AwsSecrets secrets = null;
                    secrets = new AwsSecrets()
                    {
                        AccessKey = _accessKey,
                        SecretKey = _secretKey,
                        BucketName = _bucket,
                        RegionName = _popupDisplayOptions[_popupIndex].text,
                        DistrubutionId = _cloudFrontDistributionId
                    };

                    // 認証ファイルを使って上書き
                    if (_loadSecrets)
                    {
                        secrets = AwsSecrets.Load(AwsSecrets.DefaultPath);
                    }

                    var region = RegionEndpoint.GetBySystemName(secrets.RegionName);
                    
                    Debug.Log($"[S3Uplader]Region:{region}");

                    // S3へのアップロード
                    using (var s3 = S3Utils.CreateClient(secrets.AccessKey, secrets.SecretKey, region))
                    {
                        await S3Utils.UploadAll(s3, secrets.BucketName, _directoryPath);
                    }

                    // キャッシュ削除する場合アップロード後削除
                    if (_isClearCache && !string.IsNullOrEmpty(_cloudFrontDistributionId) &&
                        !string.IsNullOrEmpty(_clearPath))
                    {
                        using (var client = AWSUtils.CloudFrontUtils.CreateClient(secrets.AccessKey, secrets.SecretKey, region))
                        {
                            var dist = _cloudFrontDistributionId;
                            if (_loadSecrets)
                            {
                                dist = secrets.DistrubutionId;
                            }
                            
                            // 削除中でないことを確認する
                            if (!await CloudFrontUtils.IsInvalidationProgress(client, dist))
                            {
                                await CloudFrontUtils.CreateInvalidationAsync(client, dist, _clearPath);
                                if (!_loadSecrets && _saveSecrets)
                                {
                                    AwsSecrets.Save(secrets, AwsSecrets.DefaultPath);
                                }
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
                    AwsSecrets secrets = null;
                    secrets = new AwsSecrets()
                    {
                        AccessKey = _accessKey,
                        SecretKey = _secretKey,
                        BucketName = _bucket,
                        RegionName =_popupDisplayOptions[_popupIndex].text,
                        DistrubutionId = _cloudFrontDistributionId
                    };

                    // 認証ファイルを使って上書き
                    if (_loadSecrets)
                    {
                        secrets = AwsSecrets.Load(AwsSecrets.DefaultPath);
                    }
                    
                    var region = RegionEndpoint.GetBySystemName(secrets.RegionName);
                    
                    using (var client = CloudFrontUtils.CreateClient(secrets.AccessKey, secrets.SecretKey, region))
                    {
                        if (!await CloudFrontUtils.IsInvalidationProgress(client, secrets.DistrubutionId))
                        {
                            await CloudFrontUtils.CreateInvalidationAsync(client, secrets.DistrubutionId, _clearPath);
                            if (!_loadSecrets && _saveSecrets)
                            {
                                AwsSecrets.Save(secrets, AwsSecrets.DefaultPath);
                            }
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