namespace FileStorageManagement.RequestClasses
{
   public class FileUploadRequest
   {
      public string? Area { get; set; }
      //public string? Bucket { get; set; }
      //public string? FileName { get; set; }
      public string? FileData { get; set; }
      public long? FileSize { get; set; }
      public int ConsumerId { get; set; }
      //public string? BucketName { get; set; }
      public bool EOF { get; set; }
      public bool HasContent { get; set; }
   }
}
