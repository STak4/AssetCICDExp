#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Amazon;
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
        
        private void OnGUI()
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

            // ボタンで実行
            if (GUILayout.Button("アップロードするフォルダを選択"))
            {
                _directoryPath = EditorUtility.OpenFolderPanel("アップロードするディレクトリ", "", "");
            }
            

            
            m_SerializedObject.ApplyModifiedProperties();

            // ボタンで実行
            if (GUILayout.Button("Upload"))
            {
                // 選択中のオブジェクト情報をログに出す
                var rectTransform = Selection.activeGameObject?.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Debug.Log($"rect:{rectTransform.rect}");
                }

                if (!(string.IsNullOrEmpty(_accessKey) || string.IsNullOrEmpty(_secretKey) ||
                    string.IsNullOrEmpty(_bucket) || string.IsNullOrEmpty(_directoryPath)))
                {
                    var region = RegionEndpoint.GetBySystemName(_popupDisplayOptions[_popupIndex].text);
                    Debug.Log($"[S3Uplader]Region:{region}");
                    var client = new AmazonS3Client(_accessKey, _secretKey, region);
                    S3Uploader.UploadFolder(client, _bucket, _directoryPath);
                }
            }
            //
            // // readonlyなプロパティ表示で監視オブジェクトの情報を出してみたり...
            // using (new EditorGUI.DisabledScope(disabled: true))
            // {
            //     if (m_Props.TrackingObject != null)
            //     {
            //         EditorGUILayout.Vector3Field("pos", m_Props.TrackingObject.transform.position);
            //     }
            // }
        }

    }
    
    //[MenuItem("S3/Upload")]
    public static class S3Uploader
    {
        public static async void UploadFolder(IAmazonS3 client, string bucketName, string folderPath)
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
}

#endif
