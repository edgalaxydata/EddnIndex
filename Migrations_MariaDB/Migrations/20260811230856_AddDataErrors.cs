using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_MariaDB.Migrations;

/// <inheritdoc />
public partial class AddDataErrors : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FileLineDataErrors",
            columns: table => new
            {
                FileId = table.Column<int>(type: "int", nullable: false),
                LineNo = table.Column<int>(type: "int", nullable: false),
                ErrorIndex = table.Column<int>(type: "int", nullable: false),
                ErrorMessage = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileLineDataErrors", x => new { x.FileId, x.LineNo, x.ErrorIndex });
            })
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FileLineDataErrors");
    }
}
