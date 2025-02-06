using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileStorageManagement.Models
{
   [Table("tb_Consumer")]
   public class Consumer
   {
      [Key]
      public int ConsumerId { get; set; }
      public string? ConsumerName { get; set; }

      public string? Description { get; set; }
      public List<FileRef>? FileRefs { get; set; }
   }
}
