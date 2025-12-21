using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddCountsToInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BodySignalCount",
                table: "FileLineInfo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBody",
                table: "FileLineInfo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasStation",
                table: "FileLineInfo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NavRouteSystemCount",
                table: "FileLineInfo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignalCount",
                table: "FileLineInfo",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodySignalCount",
                table: "FileLineInfo");

            migrationBuilder.DropColumn(
                name: "HasBody",
                table: "FileLineInfo");

            migrationBuilder.DropColumn(
                name: "HasStation",
                table: "FileLineInfo");

            migrationBuilder.DropColumn(
                name: "NavRouteSystemCount",
                table: "FileLineInfo");

            migrationBuilder.DropColumn(
                name: "SignalCount",
                table: "FileLineInfo");
        }
    }
}
