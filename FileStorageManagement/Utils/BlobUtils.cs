using Azure.Storage.Blobs;

namespace FileStorageManagement.Utils
{
   public class BlobUtils
   {
      public static async Task<long> GetBlobSize(BlobContainerClient blobContainerClient, string blobName)
      {
         long blobSize = -1;

         var blobClient = blobContainerClient.GetBlobClient(blobName);
         if (blobClient.Exists())
         {
            var properties = await blobClient.GetPropertiesAsync();

            blobSize = properties.Value.ContentLength;
         }

         return blobSize;
      }
   }
}
