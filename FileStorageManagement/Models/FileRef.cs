using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileStorageManagement.Models
{
   [Table("tb_FileRef")]
   public class FileRef
   {
      [Key]
      public long FileRefId { get; set; }
      public long FileRegistryId { get; set; }

      public int ConsumerId { get; set; }
      //public string? Area { get; set; }
      //public string? Bucket { get; set; }
      //public string? FileName { get; set; }
      //public DateTime UploadDate { get; set; }
      //public string? Comment { get; set; }

      [ForeignKey(nameof(FileRegistryId))]
      public FileRegistry? FileRegistry { get; set; }

      [ForeignKey(nameof(ConsumerId))]
      public Consumer? Consumer { get; set; }
   }
}
