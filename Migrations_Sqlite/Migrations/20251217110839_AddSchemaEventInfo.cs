using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaEventInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimarySchemaEventId",
                table: "Files",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchemaEventId",
                table: "FileLineInfo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SchemaEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Schema = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_PrimarySchemaEventId",
                table: "Files",
                column: "PrimarySchemaEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineInfo_SchemaEventId",
                table: "FileLineInfo",
                column: "SchemaEventId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaEvents_Schema_EventType",
                table: "SchemaEvents",
                columns: new[] { "Schema", "EventType" });

            migrationBuilder.AddForeignKey(
                name: "FK_FileLineInfo_SchemaEvents_SchemaEventId",
                table: "FileLineInfo",
                column: "SchemaEventId",
                principalTable: "SchemaEvents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_SchemaEvents_PrimarySchemaEventId",
                table: "Files",
                column: "PrimarySchemaEventId",
                principalTable: "SchemaEvents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileLineInfo_SchemaEvents_SchemaEventId",
                table: "FileLineInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_SchemaEvents_PrimarySchemaEventId",
                table: "Files");

            migrationBuilder.DropTable(
                name: "SchemaEvents");

            migrationBuilder.DropIndex(
                name: "IX_Files_PrimarySchemaEventId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_FileLineInfo_SchemaEventId",
                table: "FileLineInfo");

            migrationBuilder.DropColumn(
                name: "PrimarySchemaEventId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "SchemaEventId",
                table: "FileLineInfo");
        }
    }
}
