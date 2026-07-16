using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_MariaDB.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalSystemIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SignalInfoSetItem_SignalInfoId_SystemId",
                table: "SignalInfoSetItem",
                columns: new[] { "SignalInfoId", "SystemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignalInfoSetItem_SignalInfoId_SystemId",
                table: "SignalInfoSetItem");
        }
    }
}
