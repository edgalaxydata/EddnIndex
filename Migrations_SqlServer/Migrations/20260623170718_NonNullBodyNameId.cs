using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class NonNullBodyNameId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BodyNameId",
                table: "Bodies",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileLineStations_StationId",
                table: "FileLineStations",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineSignals_SignalSetId",
                table: "FileLineSignals",
                column: "SignalSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineSignals_SystemId",
                table: "FileLineSignals",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineNavRoutes_SystemId",
                table: "FileLineNavRoutes",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineInfo_SystemId",
                table: "FileLineInfo",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodySignals_BodyId",
                table: "FileLineBodySignals",
                column: "BodyId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodySignals_BodySignalId",
                table: "FileLineBodySignals",
                column: "BodySignalId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodies_BodyId",
                table: "FileLineBodies",
                column: "BodyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileLineStations_StationId",
                table: "FileLineStations");

            migrationBuilder.DropIndex(
                name: "IX_FileLineSignals_SignalSetId",
                table: "FileLineSignals");

            migrationBuilder.DropIndex(
                name: "IX_FileLineSignals_SystemId",
                table: "FileLineSignals");

            migrationBuilder.DropIndex(
                name: "IX_FileLineNavRoutes_SystemId",
                table: "FileLineNavRoutes");

            migrationBuilder.DropIndex(
                name: "IX_FileLineInfo_SystemId",
                table: "FileLineInfo");

            migrationBuilder.DropIndex(
                name: "IX_FileLineBodySignals_BodyId",
                table: "FileLineBodySignals");

            migrationBuilder.DropIndex(
                name: "IX_FileLineBodySignals_BodySignalId",
                table: "FileLineBodySignals");

            migrationBuilder.DropIndex(
                name: "IX_FileLineBodies_BodyId",
                table: "FileLineBodies");

            migrationBuilder.AlterColumn<int>(
                name: "BodyNameId",
                table: "Bodies",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
