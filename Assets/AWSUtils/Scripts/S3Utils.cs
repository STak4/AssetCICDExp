using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEngine;

namespace AWSUtils
{
    public static class S3Utils
    {
        public static AmazonS3Client CreateClient(string awsAccessKeyId, string awsSecretAccessKey, Amazon.RegionEndpoint region)
        {
            return new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, region);
        }
        
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
}
