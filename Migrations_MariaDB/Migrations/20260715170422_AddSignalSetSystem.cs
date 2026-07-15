using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_MariaDB.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalSetSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SystemId",
                table: "SignalInfoSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SystemId",
                table: "SignalInfoSetItem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalInfoSets_SystemId",
                table: "SignalInfoSets",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalInfoSetItem_SystemId",
                table: "SignalInfoSetItem",
                column: "SystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SignalInfoSetItem_Systems_SystemId",
                table: "SignalInfoSetItem",
                column: "SystemId",
                principalTable: "Systems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SignalInfoSets_Systems_SystemId",
                table: "SignalInfoSets",
                column: "SystemId",
                principalTable: "Systems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SignalInfoSetItem_Systems_SystemId",
                table: "SignalInfoSetItem");

            migrationBuilder.DropForeignKey(
                name: "FK_SignalInfoSets_Systems_SystemId",
                table: "SignalInfoSets");

            migrationBuilder.DropIndex(
                name: "IX_SignalInfoSets_SystemId",
                table: "SignalInfoSets");

            migrationBuilder.DropIndex(
                name: "IX_SignalInfoSetItem_SystemId",
                table: "SignalInfoSetItem");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "SignalInfoSets");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "SignalInfoSetItem");
        }
    }
}
