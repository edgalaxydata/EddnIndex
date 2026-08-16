using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_Sqlite.Migrations;

/// <inheritdoc />
public partial class IndexGatewayTimestamp : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_FileLineStations_StationId_GatewayTimestamp",
            table: "FileLineStations",
            columns: ["StationId", "GatewayTimestamp"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileLineSignals_SignalSetId_GatewayTimestamp",
            table: "FileLineSignals",
            columns: ["SignalSetId", "GatewayTimestamp"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileLineSignals_SystemId_GatewayTimestamp",
            table: "FileLineSignals",
            columns: ["SystemId", "GatewayTimestamp"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileLineNavRoutes_SystemId_GatewayTimestamp",
            table: "FileLineNavRoutes",
            columns: ["SystemId", "GatewayTimestamp"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileLineInfo_SystemId_GatewayTimestamp",
            table: "FileLineInfo",
            columns: ["SystemId", "GatewayTimestamp"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileLineBodySignals_BodyId_GatewayTimestamp",
            table: "FileLineBodySignals",
            columns: ["BodyId", "GatewayTimestamp"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileLineBodySignals_BodySignalId_GatewayTimestamp",
            table: "FileLineBodySignals",
            columns: ["BodySignalId", "GatewayTimestamp"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileLineBodies_BodyId_GatewayTimestamp",
            table: "FileLineBodies",
            columns: ["BodyId", "GatewayTimestamp"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FileLineStations_StationId_GatewayTimestamp",
            table: "FileLineStations");

        migrationBuilder.DropIndex(
            name: "IX_FileLineSignals_SignalSetId_GatewayTimestamp",
            table: "FileLineSignals");

        migrationBuilder.DropIndex(
            name: "IX_FileLineSignals_SystemId_GatewayTimestamp",
            table: "FileLineSignals");

        migrationBuilder.DropIndex(
            name: "IX_FileLineNavRoutes_SystemId_GatewayTimestamp",
            table: "FileLineNavRoutes");

        migrationBuilder.DropIndex(
            name: "IX_FileLineInfo_SystemId_GatewayTimestamp",
            table: "FileLineInfo");

        migrationBuilder.DropIndex(
            name: "IX_FileLineBodySignals_BodyId_GatewayTimestamp",
            table: "FileLineBodySignals");

        migrationBuilder.DropIndex(
            name: "IX_FileLineBodySignals_BodySignalId_GatewayTimestamp",
            table: "FileLineBodySignals");

        migrationBuilder.DropIndex(
            name: "IX_FileLineBodies_BodyId_GatewayTimestamp",
            table: "FileLineBodies");
    }
}
