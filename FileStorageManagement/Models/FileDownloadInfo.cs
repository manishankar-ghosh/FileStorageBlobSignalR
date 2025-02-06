using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FileStorageManagement.Models
{
   [Keyless]
   public class FileDownloadInfo
   {
      //[Key] 
      //public long CatalogueId { get; set; }
      public string? FilePath { get; set; }
      public string? PhysicalFileName {  get; set; }
      public string? TagetFileName { get; set; }
   }
}
