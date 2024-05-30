using Amazon;

namespace AWSUtils
{
    /// <summary>
    /// DO NOT UPLOAD Secrets
    /// </summary>
    public static class AwsSecrets
    {
        public static class Auth
        {
            public static readonly string AccessKey = "YOUR_ACCESS_KEY";
            public static readonly string SecretKey = "YOUR_SECRET_KEY";
        }

        public static class Bucket
        {
            public static readonly string BucketName = "YOUR_BUCKET_NAME";
            public static readonly RegionEndpoint Region = RegionEndpoint.APNortheast1;
        }
        
        public static class CloudFront
        {
            public static readonly string DistrubutionId = "YOUR_CLOUDFRONT_DESTRIBUTION_ID";
        }
    }
}
