using FileStorageManagement.Data;
using FileStorageManagement.Utils;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using FileStorageManagement.Hubs;

internal class Program
{
   private static void Main(string[] args)
   {
      string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=drvfilestore;AccountKey=sSCf0DbtXhTYz1DwBTGAI12/MqqrrGuUwwnKHQeRu7cFJ4oiHxg0W8tMYwaAbNo75iO5hXbwZe0p+ASt6SHbqg==;EndpointSuffix=core.windows.net";
      FileStoreBlobContainers.Initialize(ConnectionString);

      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddSignalR();

      // Add services to the container.
      builder.Services.AddDbContext<FileStorageDbContext>(options =>
      {
         string conStr = builder.Configuration.GetConnectionString("FileStorageDb")!;
         options.UseSqlServer(conStr);
      });

      builder.Services.Configure<KestrelServerOptions>(options =>
      {
         options.Limits.MaxRequestBodySize = 209715200; // 200 MB
      });

      builder.Services.AddControllers();
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      builder.Services.AddCors(options =>
      {
         options.AddPolicy("AllowAllOrigins",
           builder => builder
             .AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader());
      });

      var app = builder.Build();

      app.UseCors("AllowAllOrigins");

      // Configure the HTTP request pipeline.
      //if (app.Environment.IsDevelopment())
      {
         app.UseSwagger();
         app.UseSwaggerUI();
      }

      app.UseStaticFiles();
      app.UseHttpsRedirection();

      app.UseAuthorization();


      app.MapControllers();

      app.MapHub<ChatHub>("/chatHub");

      app.Run();
   }
}