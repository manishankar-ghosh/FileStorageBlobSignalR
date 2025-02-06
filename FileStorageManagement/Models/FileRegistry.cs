using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileStorageManagement.Models
{
   [Table("tb_FileRegistry")]
   public class FileRegistry
   {
      [Key]
      public long FileRegistryId { get; set; }
      public string? SHA256Hash { get; set;}
      public string? Path { get; set; }
      public long FileSize { get; set; }
      public bool Compressed { get; set; }
      public int RefCount { get; set; }

      public List<FileRef>? FileRefs { get; set;}
   }
}
