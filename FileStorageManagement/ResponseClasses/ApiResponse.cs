namespace FileStorageManagement.ResponseClasses
{
   public class ApiResponse
   {
      public int StatusCode { get; set; }
      public string? Message { get; set; }
      public object? Result { get; set; }
   }
}
