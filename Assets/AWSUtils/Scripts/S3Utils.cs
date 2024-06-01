using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using UnityEngine;

namespace AWSUtils
{
    public static class S3Utils
    {
        public static AmazonS3Client CreateClient(string awsAccessKeyId, string awsSecretAccessKey, Amazon.RegionEndpoint region)
        {
            return new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, region);
        }
        
        /// <summary>
        /// フォルダの中身をアップロード
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <param name="folderPath"></param>
        public static async Task UploadFolder(IAmazonS3 client, string bucketName, string folderPath)
        {
            Debug.Log($"[S3Uploader]Path: {folderPath}");
            var files = GetAllFiles(folderPath);

            foreach (var file in files)
            {
                Debug.Log($"[S3Uploader]Uploading... {file}");
                try
                {
                    await client.PutObjectAsync(new PutObjectRequest()
                    {
                        BucketName = bucketName,
                        Key = Path.GetFileName(file),
                        FilePath = file
                    });
                    Debug.Log($"[S3Uploader]Uploaded!");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[S3Uploader]{e.Message}");
                    throw;
                }
            }
        }


        /// <summary>
        /// フォルダ以下の中身を全てアップロード（再帰）
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <param name="folderPath"></param>
        public static async Task UploadAll(IAmazonS3 client, string bucketName, string folderPath)
        {
            Debug.Log($"[S3Uploader][All] アップロード開始. Path:{folderPath}");
            var transferUtility = new TransferUtility(client);

            TransferUtilityUploadDirectoryRequest request = new TransferUtilityUploadDirectoryRequest
            {
                BucketName = bucketName,
                Directory = folderPath,
                SearchOption = SearchOption.AllDirectories,  // 再帰的にアップロード
                SearchPattern = "*"  // すべてのファイル
            };

            try
            {
                Debug.Log($"[S3Uploader][All] アップロード開始. Path:{folderPath}");
                await transferUtility.UploadDirectoryAsync(request);
                Debug.Log($"[S3Uploader][All] アップロード完了 Path:{folderPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[S3Uploader]{e.Message}");
                throw;
            }

        }
        
        
        // ディレクトリ内のすべてのファイルパスを取得する関数
        public static string[] GetAllFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        }
    }
}
