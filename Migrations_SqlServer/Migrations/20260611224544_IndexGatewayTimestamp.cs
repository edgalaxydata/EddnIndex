using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class IndexGatewayTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FileLineStations_StationId_GatewayTimestamp",
                table: "FileLineStations",
                columns: new[] { "StationId", "GatewayTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FileLineSignals_SignalSetId_GatewayTimestamp",
                table: "FileLineSignals",
                columns: new[] { "SignalSetId", "GatewayTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FileLineSignals_SystemId_GatewayTimestamp",
                table: "FileLineSignals",
                columns: new[] { "SystemId", "GatewayTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FileLineNavRoutes_SystemId_GatewayTimestamp",
                table: "FileLineNavRoutes",
                columns: new[] { "SystemId", "GatewayTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FileLineInfo_SystemId_GatewayTimestamp",
                table: "FileLineInfo",
                columns: new[] { "SystemId", "GatewayTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodySignals_BodyId_GatewayTimestamp",
                table: "FileLineBodySignals",
                columns: new[] { "BodyId", "GatewayTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodySignals_BodySignalId_GatewayTimestamp",
                table: "FileLineBodySignals",
                columns: new[] { "BodySignalId", "GatewayTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodies_BodyId_GatewayTimestamp",
                table: "FileLineBodies",
                columns: new[] { "BodyId", "GatewayTimestamp" });
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
}
