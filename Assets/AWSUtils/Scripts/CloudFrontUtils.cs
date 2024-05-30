using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using UnityEngine;

namespace AWSUtils
{
    public static class CloudFrontUtils
    {
        public static AmazonCloudFrontClient CreateClient(string awsAccessKeyId, string awsSecretAccessKey, Amazon.RegionEndpoint region)
        {
            return new AmazonCloudFrontClient(awsAccessKeyId, awsSecretAccessKey, region);
        }
        
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

            try
            {
                var response = await client.CreateInvalidationAsync(request); 
                if (response.HttpStatusCode == System.Net.HttpStatusCode.Created)
                {
                    Debug.Log($"[CloudFront][CreateInvalidation] キャッシュ削除リクエスト完了. ID:{response.Invalidation.Id}");
                    return response.Invalidation.Id;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudFront][CreateInvalidation] {e.Message}");
                throw;
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
            Debug.Log($"[CloudFront][IsInvalidation] キャッシュ削除ステータス確認.");
            
            var request = new ListInvalidationsRequest()
            {
                DistributionId = distributionId
            };

            try
            {
                var response = await client.ListInvalidationsAsync(request);

                int inProgress = 0;
                foreach (var item in response.InvalidationList.Items)
                {
                    if (item.Status == "InProgress")
                    {
                        inProgress++;
                    }
                }
                
                Debug.Log($"[CloudFront][IsInvalidation] キャッシュ削除進行中か？ {inProgress > 0}");
                return inProgress > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Debug.LogError($"[CloudFront][IsInvalidation] {e.Message}");
                return false;
            }
        }
    }
}
