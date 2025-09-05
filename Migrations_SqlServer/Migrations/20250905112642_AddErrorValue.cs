using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "LatitudeError",
                table: "FileLineStations",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "LongitudeError",
                table: "FileLineStations",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "ArgOfPeriapsisError",
                table: "FileLineBodies",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "InclinationError",
                table: "FileLineBodies",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SemiMajorAxisError",
                table: "FileLineBodies",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatitudeError",
                table: "FileLineStations");

            migrationBuilder.DropColumn(
                name: "LongitudeError",
                table: "FileLineStations");

            migrationBuilder.DropColumn(
                name: "ArgOfPeriapsisError",
                table: "FileLineBodies");

            migrationBuilder.DropColumn(
                name: "InclinationError",
                table: "FileLineBodies");

            migrationBuilder.DropColumn(
                name: "SemiMajorAxisError",
                table: "FileLineBodies");
        }
    }
}
