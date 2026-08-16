using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_MariaDB.Migrations;

/// <inheritdoc />
public partial class AddSignalItemFirstLastSeen : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "FirstSeen",
            table: "SignalInfoSetItem",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastSeen",
            table: "SignalInfoSetItem",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_SignalInfoSetItem_SignalInfoId_LastSeen_FirstSeen_SystemId",
            table: "SignalInfoSetItem",
            columns: ["SignalInfoId", "LastSeen", "FirstSeen", "SystemId"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SignalInfoSetItem_SignalInfoId_LastSeen_FirstSeen_SystemId",
            table: "SignalInfoSetItem");

        migrationBuilder.DropColumn(
            name: "FirstSeen",
            table: "SignalInfoSetItem");

        migrationBuilder.DropColumn(
            name: "LastSeen",
            table: "SignalInfoSetItem");
    }
}
