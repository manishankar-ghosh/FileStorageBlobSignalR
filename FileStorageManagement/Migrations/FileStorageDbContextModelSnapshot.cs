﻿// <auto-generated />
using FileStorageManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FileStorageManagement.Migrations
{
    [DbContext(typeof(FileStorageDbContext))]
    partial class FileStorageDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("FileStorageManagement.Models.Consumer", b =>
                {
                    b.Property<int>("ConsumerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ConsumerId"));

                    b.Property<string>("ConsumerName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ConsumerId");

                    b.ToTable("tb_Consumer");
                });

            modelBuilder.Entity("FileStorageManagement.Models.FileRef", b =>
                {
                    b.Property<long>("FileRefId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("FileRefId"));

                    b.Property<int>("ConsumerId")
                        .HasColumnType("int");

                    b.Property<long>("FileRegistryId")
                        .HasColumnType("bigint");

                    b.HasKey("FileRefId");

                    b.HasIndex("ConsumerId");

                    b.HasIndex("FileRegistryId");

                    b.ToTable("tb_FileRef");
                });

            modelBuilder.Entity("FileStorageManagement.Models.FileRegistry", b =>
                {
                    b.Property<long>("FileRegistryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("FileRegistryId"));

                    b.Property<bool>("Compressed")
                        .HasColumnType("bit");

                    b.Property<long>("FileSize")
                        .HasColumnType("bigint");

                    b.Property<string>("Path")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RefCount")
                        .HasColumnType("int");

                    b.Property<string>("SHA256Hash")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("FileRegistryId");

                    b.ToTable("tb_FileRegistry");
                });

            modelBuilder.Entity("FileStorageManagement.Models.FileRef", b =>
                {
                    b.HasOne("FileStorageManagement.Models.Consumer", "Consumer")
                        .WithMany("FileRefs")
                        .HasForeignKey("ConsumerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FileStorageManagement.Models.FileRegistry", "FileRegistry")
                        .WithMany("FileRefs")
                        .HasForeignKey("FileRegistryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Consumer");

                    b.Navigation("FileRegistry");
                });

            modelBuilder.Entity("FileStorageManagement.Models.Consumer", b =>
                {
                    b.Navigation("FileRefs");
                });

            modelBuilder.Entity("FileStorageManagement.Models.FileRegistry", b =>
                {
                    b.Navigation("FileRefs");
                });
#pragma warning restore 612, 618
        }
    }
}
