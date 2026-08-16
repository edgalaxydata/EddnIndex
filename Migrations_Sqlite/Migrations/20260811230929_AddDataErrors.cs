using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_Sqlite.Migrations;

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
                FileId = table.Column<int>(type: "INTEGER", nullable: false),
                LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                ErrorIndex = table.Column<int>(type: "INTEGER", nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileLineDataErrors", x => new { x.FileId, x.LineNo, x.ErrorIndex });
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FileLineDataErrors");
    }
}
