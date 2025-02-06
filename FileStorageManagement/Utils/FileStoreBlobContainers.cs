using Azure.Storage.Blobs;

namespace FileStorageManagement.Utils
{
   public class FileStoreBlobContainers
   {
      private static string? _ConnectionString { get; set; }
      public static BlobContainerClient? _FileStoreBlobContainerClient;
      public static BlobContainerClient? TempContainerClient;

      private static string? _containerName;
      public static void Initialize(string ConnectionString)
      {
         _ConnectionString = ConnectionString;
         // TempContainerClient is not in use, can be removed
         TempContainerClient = new BlobContainerClient(_ConnectionString, "tempcontainer");
         TempContainerClient.CreateIfNotExists();
      }

      public static async Task<BlobContainerClient?> GetFileStoreBlobContainerClient()
      {
         string containerName = DateTime.UtcNow.ToString("yyyyMMdd");
         if (_containerName != containerName)
         {
            _FileStoreBlobContainerClient = new BlobContainerClient(_ConnectionString, containerName);
            await _FileStoreBlobContainerClient.CreateIfNotExistsAsync();
            _containerName = containerName;
         }

         return _FileStoreBlobContainerClient;
      }

      public static async Task<BlobContainerClient?> GetFileStoreBlobContainerClient(string containerName)
      {
         var containerClient = new BlobContainerClient(_ConnectionString, containerName);
         if(!await containerClient.ExistsAsync())
         {
            return null;
         }

         return containerClient;
      }
   }
}
