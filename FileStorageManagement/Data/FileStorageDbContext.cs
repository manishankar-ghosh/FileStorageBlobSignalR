using Microsoft.EntityFrameworkCore;
using FileStorageManagement.Models;
namespace FileStorageManagement.Data
{
   public class FileStorageDbContext : DbContext
   {
        public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options) : base(options) 
        {
            
        }

      public DbSet<FileRegistry> FileRegistries { get; set; }
      public DbSet<FileRef> FileRefs { get; set; }
      public DbSet<Consumer> Consumers { get; set; }
   }
}
