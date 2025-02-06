using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileStorageManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_Consumer",
                columns: table => new
                {
                    ConsumerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Consumer", x => x.ConsumerId);
                });

            migrationBuilder.CreateTable(
                name: "tb_FileRegistry",
                columns: table => new
                {
                    FileRegistryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SHA256Hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Compressed = table.Column<bool>(type: "bit", nullable: false),
                    RefCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_FileRegistry", x => x.FileRegistryId);
                });

            migrationBuilder.CreateTable(
                name: "tb_FileRef",
                columns: table => new
                {
                    FileRefId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileRegistryId = table.Column<long>(type: "bigint", nullable: false),
                    ConsumerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_FileRef", x => x.FileRefId);
                    table.ForeignKey(
                        name: "FK_tb_FileRef_tb_Consumer_ConsumerId",
                        column: x => x.ConsumerId,
                        principalTable: "tb_Consumer",
                        principalColumn: "ConsumerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_FileRef_tb_FileRegistry_FileRegistryId",
                        column: x => x.FileRegistryId,
                        principalTable: "tb_FileRegistry",
                        principalColumn: "FileRegistryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_FileRef_ConsumerId",
                table: "tb_FileRef",
                column: "ConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_FileRef_FileRegistryId",
                table: "tb_FileRef",
                column: "FileRegistryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_FileRef");

            migrationBuilder.DropTable(
                name: "tb_Consumer");

            migrationBuilder.DropTable(
                name: "tb_FileRegistry");
        }
    }
}
