using FileStorageManagement.Data;
using FileStorageManagement.Models;
using FileStorageManagement.RequestClasses;
using FileStorageManagement.ResponseClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using FileStorageManagement.Helpers;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using FileStorageManagement.Utils;
using FileStorageManagement.Enums;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileStorageManagement.Hubs;
using Microsoft.AspNetCore.SignalR;
using Azure;

namespace FileStorageManagement.Controllers
{
   [Route("api/[controller]")]
   [ApiController]
   public class FilesController(FileStorageDbContext _context, IHubContext<ChatHub> _hubContext) : ControllerBase
   {
      //dotnet run --urls "http://localhost:5235"
      private string _DownloadTempPathRoot = $"C:\\FileStorage\\DownloadTemp";
      private string _Root = @"C:\FileStorage";
      private string _Area = "Cert";
      private string _TempFolder = @"C:\Temp\000";

      private static readonly FormOptions _defaultFormOptions = new FormOptions();
      //private readonly IHubContext<ChatHub> _hubContext;

      [HttpGet("hash/{fileHash}/exists")]
      [ProducesResponseType(typeof(ApiResponse), 200)]
      public async Task<IActionResult> CheckFileExists(string fileHash)
      {
         ApiResponse response = new ApiResponse { StatusCode = 200, Message = "" };
         try
         {
            var fileRegistry = await _context.FileRegistries.FirstOrDefaultAsync(x => x.SHA256Hash == fileHash);

            if (fileRegistry != null) // File is present in the store
            {
               response.StatusCode = (int)FileResponseEnums.Available;
               response.Result = fileRegistry.FileRegistryId;
            }
            else
            {
               // Check if file is partially uploaded
               string tempFileName = FileUtils.GetMD5Hash(fileHash);
               string tempFilePath = $"{_TempFolder}\\{tempFileName}";
               long fileSize = FileUtils.GetFileSize(tempFilePath);
               //long fileSize = await BlobUtils.GetBlobSize(FileStoreBlobContainers.TempContainerClient!, tempFileName);
               if (fileSize > -1)
               {
                  response.StatusCode = (int)FileResponseEnums.PartiallyUploaded;
                  response.Result = new { FileGuid = tempFileName, FileSize = fileSize };
               }
               else
               {
                  response.StatusCode = (int)FileResponseEnums.Unavailable;
                  response.Result = 0; // File doesn't exist in file storage
               }
            }
         }
         catch (Exception ex)
         {
            response.StatusCode = 500;
            response.Message = ex.Message;
            response.Result = "";
         }

         return Ok(response);
      }

      [HttpGet("{fileRegistryId:long}/fileName/{fileName}/download")]
      public async Task<IActionResult> GetFile(long fileRegistryId, string fileName)
      {
         ClearFileDownloadCache(_DownloadTempPathRoot);

         var result = await GetFilePath(fileRegistryId);
         if (result == null)
         {
            return NotFound("");
         }
         else
         {
            string path = $"{result.Path}";

            string[] ar = path.Split('/');
            string containerName = ar[0];
            string blobName = ar[1];

            BlobContainerClient? containerClient = await FileStoreBlobContainers.GetFileStoreBlobContainerClient(containerName);
            if (containerClient == null)
            {
               return NotFound();
            }

            // Set the content type based on the file extension
            string contentType = "application/octet-stream"; // GetContentType(path);

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            string downloadTempPath = $"{_DownloadTempPathRoot}\\{blobName}";

            //await blobClient.DownloadToAsync(downloadTempPath);

            await this.DownloadBlobAsync(containerClient, blobClient, downloadTempPath);

            return PhysicalFile(downloadTempPath, contentType, fileName);
         }
      }

      private void ClearFileDownloadCache(string path)
      {
         DirectoryInfo dirInfo = new DirectoryInfo(path); 

         FileInfo[] files = dirInfo.GetFiles("*.*");

         foreach (FileInfo file in files)
         {
            var hours = (DateTime.Now - file.CreationTime).Hours;
            if (hours > 24) 
            {
               file.Delete();
            }
         }
      }

      private async Task DownloadBlobAsync(BlobContainerClient containerClient, BlobClient blobClient, string downloadTempPath)
      {
         await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Starting download file from cloud...");
         await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Calculating...");

         var prop = await blobClient.GetPropertiesAsync();
         var length = prop.Value.ContentLength;
         var progressTracker = new ProgressTracker(UpdateDownloadProgressToClient, length);

         var download = await blobClient.DownloadStreamingAsync(new HttpRange(0, length),
             new BlobRequestConditions(),
         false,
             progressTracker,
             CancellationToken.None);

         var data = download.Value.Content;

         await using (var fileStream = System.IO.File.Create(downloadTempPath))
         {
            await data.CopyToAsync(fileStream);
         }

         await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "File download from cloud to server cache completed.");
         await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Starting download from server cache to your machine...");

      }

      private async Task UpdateDownloadProgressToClient(double progress)
      {
         //var progressHandler = new Progress<long>();
         //progressHandler.ProgressChanged += (s, e) => Console.WriteLine($"Uploaded {e} bytes");
         string cmd = "";
         string msg = $"{progress}% completed";
         await _hubContext.Clients.All.SendAsync("ReceiveMessage", cmd, msg);
      }

      private async Task DownloadBlobAsync2(BlobContainerClient containerClient, BlobClient blobClient, string downloadTempPath)
      {
         int bufferSize = 1024 * 1024 * 50; // Size of each chunk in bytes
 
         await using (Stream reader = await blobClient.OpenReadAsync(position: 0, bufferSize: bufferSize))
         {
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            long totalBytesRead = 0;

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Starting download file from cloud...");
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Calculating...");

            while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
               using (FileStream fileStream = new FileStream(downloadTempPath, FileMode.Append, FileAccess.Write))
               {
                  await fileStream.WriteAsync(buffer, 0, bytesRead);
               }

               totalBytesRead += bytesRead;
               await this.UpdateDownloadProgressToClient(totalBytesRead, reader.Length);
            }

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "File download from cloud to server cache completed.");
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Starting download from server cache to your machine...");
         }

      }

      private async Task UpdateDownloadProgressToClient(long totalBytesRead, long fileSize)
      {
         string cmd = "";
         decimal progress = Math.Round((decimal)totalBytesRead / fileSize, 2) * 100;
         string msg = $"{progress}% completed";
         await _hubContext.Clients.All.SendAsync("ReceiveMessage", cmd, msg);
      }

      [HttpDelete("{fileRegistryId:long}/delete")]
      public async Task<IActionResult> Delete(long fileRegistryId)
      {
         // Instead of physically deleting file here, mark it for deletion and include an extra column in db as LastAccessed
         // we can decide to delete file physically which are marked for deletion and not accessed for a certain period of time
         ApiResponse response = new ApiResponse { StatusCode = 200, Message = "" };

         try
         {
            var fileRegistryEntity = await _context.FileRegistries.FirstOrDefaultAsync(x => x.FileRegistryId == fileRegistryId);
            if (fileRegistryEntity != null)
            {
               if (fileRegistryEntity.RefCount < 2)
               {
                  this.DeleteFile(fileRegistryEntity.Path!);
                  var fileRef = await _context.FileRefs.FirstOrDefaultAsync(x => x.FileRegistryId == fileRegistryId);
                  if (fileRef != null)
                  {
                     _context.FileRefs.Remove(fileRef);
                  }

                  _context.FileRegistries.Remove(fileRegistryEntity);
               }
               else
               {
                  fileRegistryEntity.RefCount -= 1;
               }

               await _context.SaveChangesAsync();
            }
            else
            {
               response.StatusCode = (int)StatusCodes.Status404NotFound;
               response.Message = $"{fileRegistryId} not found!";
            }
         }
         catch (Exception ex)
         {
            response.StatusCode = 500;
            response.Message = ex.Message;
            response.Result = "";
         }

         return Ok(response);
      }

      // MultipartBodyLengthLimit  was needed for zip files with form data.
      // [DisableRequestSizeLimit] works for the KESTREL server, but not IIS server 
      // for IIS: webconfig... <requestLimits maxAllowedContentLength="102428800" />
      [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
      [DisableRequestSizeLimit]
      [Consumes("multipart/form-data")]
      [HttpPost("upload")]
      public async Task<IActionResult> Upload()
      {
         ApiResponse response = new ApiResponse
         {
            StatusCode = 200
         };

         if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType ?? string.Empty))
         {
            response.StatusCode = (int)StatusCodes.Status400BadRequest;
            response.Message = $"Expected a multipart request, but got {Request.ContentType}";
            response.Result = "";

            return Ok(response);
         }

         FileUploadRequest? fileUploadRequest = new FileUploadRequest();
         string fileGuid = HttpContext.Request.Headers["fileGuid"]!;
         string fileHash = HttpContext.Request.Headers["fileHash"]!;

         fileUploadRequest.Area = HttpContext.Request.Headers["area"];
         string? fileSize = HttpContext.Request.Headers["fileSize"];
         fileUploadRequest.FileSize = long.Parse(fileSize ?? "0");
         string? consumerId = HttpContext.Request.Headers["consumerId"];
         fileUploadRequest.ConsumerId = int.Parse(consumerId ?? "0");
         fileUploadRequest.EOF = bool.Parse(HttpContext.Request.Headers["eof"]!);
         fileUploadRequest.HasContent = bool.Parse(HttpContext.Request.Headers["hasContent"]!);

         if (HttpContext.Request.Body == null)
         {
            response.StatusCode = (int)StatusCodes.Status400BadRequest;
            response.Message = "Request is empty";
            response.Result = "";
         }
         else
         {
            try
            {
               var fileUploadResponse = new FileUploadResponse();
               string tempFilePath = string.Empty;

               if (string.IsNullOrEmpty(fileGuid))
               {
                  // Create file

                  fileGuid = FileUtils.GetMD5Hash(fileHash);
                  fileUploadResponse.FileGuid = fileGuid;

                  //tempBlobClient = await this.CreateFile(FileStoreBlobContainers.TempContainerClient!, fileUploadResponse.FileGuid, HttpContext, Request, fileUploadRequest.HasContent);
                  tempFilePath = await this.CreateFile(_TempFolder, fileUploadResponse.FileGuid, HttpContext, Request, fileUploadRequest.HasContent);

                  response.Result = fileUploadResponse;
               }
               else
               {
                  // Append file
                  //tempBlobClient = await this.AppendFile(FileStoreBlobContainers.TempContainerClient!, fileGuid, HttpContext, Request, fileUploadRequest.HasContent);
                  tempFilePath = await this.AppendFile(_TempFolder, fileGuid, HttpContext, Request, fileUploadRequest.HasContent);
               }

               if (fileUploadRequest.EOF) // File transfer completed
               {
                  BlobContainerClient? fileStoreBlobContainerClient = await FileStoreBlobContainers.GetFileStoreBlobContainerClient();

                  //string path = await this.MoveBlob(tempBlobClient!, fileStoreBlobContainerClient!);

                  await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Starting upload file from app server into cloud...");
                  await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "Calculating...");

                  string path = await this.UploadBlob(tempFilePath, fileStoreBlobContainerClient!);

                  fileUploadResponse.FileRegistryId = await RegisterFile(fileHash, fileUploadRequest, path);

                  await _hubContext.Clients.All.SendAsync("ReceiveMessage", "--createNewMessageContainer", "File upload completed.");

                  System.IO.File.Delete(tempFilePath);
               }

               fileUploadResponse.FileGuid = fileGuid;
               response.Result = fileUploadResponse;
            }
            catch (Exception ex)
            {
               response.StatusCode = 500;
               response.Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
               response.Result = "";
            }
         }

         //fileUploadRequest = null;
         return Ok(response);
      }

      [HttpPut("{fileRegistryId}/IncrementRefCount")]
      public async Task<IActionResult> IncrementRefCount(long fileRegistryId, [FromBody] IncrementRefCountRequest incrementRefCountRequest)
      {
         if (fileRegistryId > 0 && incrementRefCountRequest.ConsumerId > 0)
         {
            //await _context.FileRegistries.Where(x => x.FileRegistryId == fileRegistryId)
            //   .ExecuteUpdateAsync(entity => entity.SetProperty(entity => entity.RefCount, entity => entity.RefCount + 1));

            var fileRegistryEntity = await _context.FileRegistries.FirstOrDefaultAsync(x => x.FileRegistryId == fileRegistryId);
            if (fileRegistryEntity != null)
            {
               fileRegistryEntity.RefCount += 1;
               _context.FileRegistries.Update(fileRegistryEntity);

               FileRef consumer = new FileRef
               {
                  FileRegistryId = fileRegistryId,
                  ConsumerId = incrementRefCountRequest.ConsumerId
               };

               using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
               {
                  await _context.SaveChangesAsync();

                  scope.Complete();
               }

               return Ok();
            }
            else
            {
               return NotFound($"{fileRegistryId} not found");
            }
         }
         else
         {
            return BadRequest("fileRegistryId/consumerId can't be 0");
         }
      }
      private async void DeleteFile(string path)
      {
         string[] ar = path.Split('/');
         string containerName = ar[0];
         string blobName = ar[1];
         var containerClient = await FileStoreBlobContainers.GetFileStoreBlobContainerClient(containerName);
         if (containerClient == null)
         {
            throw new InvalidOperationException("Blob Container not found on the storage!");
         }
         var blobClient = containerClient.GetBlobClient(blobName);
         await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots);
      }

      private async Task<long> RegisterFile(string fileHash, FileUploadRequest fileUploadRequest, string path)
      {
         var fileRegistryEntity = new FileRegistry
         {
            Path = path,
            SHA256Hash = fileHash,
            FileSize = (long)fileUploadRequest.FileSize!,
            RefCount = 1
         };

         _context.Add(fileRegistryEntity);
         await _context.SaveChangesAsync();

         FileRef fileRefEntity = new FileRef
         {
            FileRegistryId = fileRegistryEntity.FileRegistryId,
            ConsumerId = fileUploadRequest.ConsumerId
         };

         _context.FileRefs.Add(fileRefEntity);
         using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
         {
            await _context.SaveChangesAsync();

            scope.Complete();
         }

         return fileRegistryEntity.FileRegistryId;
      }

      private async Task<FileRegistry?> GetFilePath(long fileRegistryId)
      {
         var result = await _context.FileRegistries.FirstOrDefaultAsync(x => x.FileRegistryId == fileRegistryId);

         return result;
      }

      private string GetContentType(string path)
      {
         var provider = new FileExtensionContentTypeProvider();
         if (!provider.TryGetContentType(path, out string? contentType))
         {
            contentType = "application/octet-stream";
         }
         return contentType;
      }

      public static bool IsValidFileName(string fileName)
      {
         return !(string.IsNullOrWhiteSpace(fileName)
            && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            && fileName.Length > 255
            && fileName.Equals("CON")
            && fileName.Equals("PRN")
            & fileName.Equals("AUX"));
      }

      private async Task<string> UploadBlob(string filePath, BlobContainerClient fileStoreBlobContainerClient)
      {
         string fileStoreBlobName = Guid.NewGuid().ToString();
         BlobClient blobClient = fileStoreBlobContainerClient.GetBlobClient(fileStoreBlobName);
         long fileSize = 0;
         double _progress = 0;

         var progressHandler = new Progress<long>();
         progressHandler.ProgressChanged += async (s, e) =>
         {
            var progress = Math.Round((double)e / fileSize * 100, 2);
            if (progress - _progress > 1 || progress >= 100)
            {
               _progress = progress;
               string cmd = "";
               string msg = $"{progress}% completed";
               await _hubContext.Clients.All.SendAsync("ReceiveMessage", cmd, msg);
            }
         };

         BlobUploadOptions options = new BlobUploadOptions
         {
            ProgressHandler = progressHandler
         };

         using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
         {
            fileSize = fileStream.Length;
            await blobClient.UploadAsync(fileStream, options, CancellationToken.None);
         }

         return $"{fileStoreBlobContainerClient.Name}/{fileStoreBlobName}";
      }

      private async Task<string> MoveBlob(AppendBlobClient tempBlobClient, BlobContainerClient fileStoreBlobContainerClient)
      {
         string fileStoreBlobName = Guid.NewGuid().ToString();
         BlobClient newblob = fileStoreBlobContainerClient.GetBlobClient(fileStoreBlobName);

         if (await tempBlobClient.ExistsAsync())
         {
            await newblob.StartCopyFromUriAsync(tempBlobClient.Uri);
            await tempBlobClient.DeleteIfExistsAsync();
         }

         return $"{fileStoreBlobContainerClient.Name}/{fileStoreBlobName}";
      }

      private string MoveFileIntoStore(string areaPath, string sourceFilePath, FileUploadRequest fileUploadRequest, string targetPath = "")
      {
         string targetDir = $"{areaPath}\\{DateTime.UtcNow.ToString("yyyyMMdd")}";
         if (!Directory.Exists(targetDir))
         {
            Directory.CreateDirectory(targetDir);
         }

         if (string.IsNullOrEmpty(targetPath))
         {
            targetPath = $"{targetDir}\\{Guid.NewGuid()}";
         }

         System.IO.File.Move(sourceFilePath, targetPath, true);

         return targetPath;
      }

      private async Task<AppendBlobClient> AppendFile(BlobContainerClient blobContainerClient, string fileGuid, HttpContext httpContext, HttpRequest httpRequest, bool hasContent)
      {
         AppendBlobClient tempBlobClient = blobContainerClient.GetAppendBlobClient(fileGuid);

         if (hasContent)
         {
            var boundary = MultipartRequestHelper.GetBoundary(
       MediaTypeHeaderValue.Parse(httpRequest.ContentType),
       _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, httpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
               var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

               if (hasContentDispositionHeader)
               {
                  if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition!))
                  {
                     await using (MemoryStream ms = new MemoryStream())
                     {
                        await section.Body.CopyToAsync(ms);
                        ms.Position = 0;
                        await tempBlobClient.AppendBlockAsync(ms);
                     }
                  }
               }

               section = await reader.ReadNextSectionAsync();
            }
         }

         return tempBlobClient;
      }

      private async Task<AppendBlobClient> CreateFile(BlobContainerClient blobContainerClient, string fileGuid, HttpContext httpContext, HttpRequest httpRequest, bool hasContent)
      {
         AppendBlobClient blobClient = blobContainerClient.GetAppendBlobClient(fileGuid);
         await blobClient.CreateIfNotExistsAsync();

         if (hasContent)
         {
            var boundary = MultipartRequestHelper.GetBoundary(
       MediaTypeHeaderValue.Parse(httpRequest.ContentType),
       _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, httpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
               var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

               if (hasContentDispositionHeader)
               {
                  if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition!))
                  {
                     await using (MemoryStream ms = new MemoryStream())
                     {
                        await section.Body.CopyToAsync(ms);
                        ms.Position = 0;
                        await blobClient.AppendBlockAsync(ms);
                     }
                  }
               }

               section = await reader.ReadNextSectionAsync();
            }
         }

         return blobClient;
      }

      private async Task<string> AppendFile(string tempFolder, string fileGuid, HttpContext httpContext, HttpRequest httpRequest, bool hasContent)
      {
         string path = $"{tempFolder}\\{fileGuid}";

         if (hasContent)
         {
            var boundary = MultipartRequestHelper.GetBoundary(
                   MediaTypeHeaderValue.Parse(httpRequest.ContentType),
                   _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, httpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
               var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

               if (hasContentDispositionHeader)
               {
                  if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition!))
                  {
                     using (FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write))
                     {
                        await section.Body.CopyToAsync(fileStream);
                        //await AppendStream(section.Body, fileStream);
                     }
                  }
               }

               section = await reader.ReadNextSectionAsync();
            }
         }

         return path;
      }

      private async Task<string> CreateFile(string tempFolder, string fileGuid, HttpContext httpContext, HttpRequest httpRequest, bool hasContent)
      {
         string path = $"{tempFolder}\\{fileGuid}";

         if (hasContent)
         {
            var boundary = MultipartRequestHelper.GetBoundary(
                   MediaTypeHeaderValue.Parse(httpRequest.ContentType),
                   _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, httpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
               var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

               if (hasContentDispositionHeader)
               {
                  if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition!))
                  {
                     using (var targetStream = System.IO.File.Create(path))
                     {
                        await section.Body.CopyToAsync(targetStream);
                     }
                  }
               }

               section = await reader.ReadNextSectionAsync();
            }
         }

         return path;
      }
   }
}
