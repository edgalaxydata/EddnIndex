using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_MariaDB.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BodyDesignations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DesignationId = table.Column<int>(type: "int", nullable: true),
                    Designation = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesignationType = table.Column<string>(type: "enum('Unknown','StellarBarycentre','StellarBody','Belt','AsteroidCluster','Comet','PlanetaryBarycentre','PlanetaryBody','PlanetaryRing','PlanetaryComet','Moon1Barycentre','Moon1Body','Moon1Ring','Moon1Comet','Moon2Barycentre','Moon2Body','Moon2Ring','Moon2Comet','Moon3Barycentre','Moon3Body','Moon3Ring','Moon3Comet')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StarNum = table.Column<int>(type: "int", nullable: true),
                    StellarBarycentreLength = table.Column<int>(type: "int", nullable: true),
                    PlanetNum = table.Column<int>(type: "int", nullable: true),
                    Moon1Num = table.Column<int>(type: "int", nullable: true),
                    Moon2Num = table.Column<int>(type: "int", nullable: true),
                    Moon3Num = table.Column<int>(type: "int", nullable: true),
                    BarycentreLength = table.Column<int>(type: "int", nullable: true),
                    RingNum = table.Column<int>(type: "int", nullable: true),
                    ClusterNum = table.Column<int>(type: "int", nullable: true),
                    CometNum = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyDesignations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BodyNameOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemAddress = table.Column<long>(type: "bigint", nullable: false),
                    SystemName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyID = table.Column<int>(type: "int", nullable: false),
                    BodyName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyDesignation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyType = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArgOfPeriapsis = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    Inclination = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    SinceVersion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UntilVersion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyNameOverrides", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BodyNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyNames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BodySignalInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SignalType = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubCategory = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryID = table.Column<long>(type: "bigint", nullable: true),
                    Region = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SignalCount = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodySignalInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FilePrefixSchemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FilenamePrefix = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimarySchema = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsTest = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilePrefixSchemas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    PrimarySchema = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LineCount = table.Column<int>(type: "int", nullable: true),
                    CompressedSize = table.Column<long>(type: "bigint", nullable: true),
                    UncompressedSize = table.Column<long>(type: "bigint", nullable: true),
                    SystemLineCount = table.Column<int>(type: "int", nullable: true),
                    StationLineCount = table.Column<int>(type: "int", nullable: true),
                    NavRouteSystemCount = table.Column<int>(type: "int", nullable: true),
                    BodyLineCount = table.Column<int>(type: "int", nullable: true),
                    SignalCount = table.Column<int>(type: "int", nullable: true),
                    BodySignalCount = table.Column<int>(type: "int", nullable: true),
                    ErrorCount = table.Column<int>(type: "int", nullable: true),
                    IsTest = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ProcessedVersion = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.UniqueConstraint("AK_Files_FileName", x => x.FileName);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameVersionDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Season = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdateTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    UpdateStartTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    UpdateEndTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    Description = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAlphaOrBeta = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersionDates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GameVersion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GameBuild = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsOdyssey = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    IsHorizons = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ParentSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BodyID = table.Column<int>(type: "int", nullable: false),
                    BodyType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentSetId = table.Column<int>(type: "int", nullable: true),
                    ParentJson = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentSets_ParentSets_ParentSetId",
                        column: x => x.ParentSetId,
                        principalTable: "ParentSets",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X0 = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    Y0 = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    Z0 = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    SizeX = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    SizeY = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    SizeZ = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    SectorAddress = table.Column<int>(type: "int", nullable: true),
                    IsHASector = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    HASectorPriority = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SignalInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SignalName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SignalType = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsStation = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SignalInfoSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FirstSignalId = table.Column<int>(type: "int", nullable: false),
                    LastSignalId = table.Column<int>(type: "int", nullable: false),
                    SignalCount = table.Column<int>(type: "int", nullable: false),
                    SignalSetJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalInfoSets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Software",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftwareName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoftwareVersion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Software", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MarketId = table.Column<long>(type: "bigint", nullable: true),
                    SystemName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StationName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StationType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SystemAddress = table.Column<long>(type: "bigint", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IsRejected = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SystemNameOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemAddress = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    Y = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    Z = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNameOverrides", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SystemNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Systems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemNameId = table.Column<long>(type: "bigint", nullable: true),
                    ModSystemAddress = table.Column<long>(type: "bigint", nullable: true),
                    NameModSystemAddress = table.Column<long>(type: "bigint", nullable: true),
                    X = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    Y = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    Z = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    IsRejected = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    SectorId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "if(`SystemNameId` >= 1 << 60,(`SystemNameId` >> 40) - 0x100000,NULL)"),
                    SectorAddress = table.Column<int>(type: "int", nullable: true, computedColumnSql: "if(`SystemNameId` > 0 and `SystemNameId` < 1 << 60,`SystemNameId` >> 40,NULL)"),
                    PGSuffix = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, computedColumnSql: "if(`SystemNameId` >= 0,concat(' ',char((`SystemNameId` >> 16 & 0x1fffff) MOD 26 + 65),char(floor((`SystemNameId` >> 16 & 0x1fffff) / 26 MOD 26) + 65),'-',char(floor((`SystemNameId` >> 16 & 0x1fffff) / (26 * 26) MOD 26) + 65),' ',char((`SystemNameId` >> 37 & 7) + 97),if(floor((`SystemNameId` >> 16 & 0x1fffff) / (26 * 26 * 26)) = 0,'',concat(floor((`SystemNameId` >> 16 & 0x1fffff) / (26 * 26 * 26)),'-')),`SystemNameId` & 65535),NULL)")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsNamedSystem = table.Column<bool>(type: "tinyint(1)", nullable: true, computedColumnSql: "`SystemNameId` < 0"),
                    IsHASystem = table.Column<bool>(type: "tinyint(1)", nullable: true, computedColumnSql: "`SystemNameId` >= 1 << 60"),
                    SystemAddress = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "(`ModSystemAddress` & 0xffff) << 44 - (`ModSystemAddress` >> 37 & 7) * 3 | (`ModSystemAddress` >> 40 & 0x7f) << 37 - (`ModSystemAddress` >> 37 & 7) * 3 | (`ModSystemAddress` >> 16 & 0x7f) << 30 - (`ModSystemAddress` >> 37 & 7) * 2 | (`ModSystemAddress` >> 47 & 0x3f) << 24 - (`ModSystemAddress` >> 37 & 7) * 2 | (`ModSystemAddress` >> 23 & 0x7f) << 17 - (`ModSystemAddress` >> 37 & 7) * 1 | (`ModSystemAddress` >> 53 & 0x7f) << 10 - (`ModSystemAddress` >> 37 & 7) * 1 | (`ModSystemAddress` >> 30 & 0x7f) << 3 | `ModSystemAddress` >> 37 & 7"),
                    SysAddr_PGSuffix = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, computedColumnSql: "concat(' ',char((`ModSystemAddress` >> 16 & 0x1fffff) MOD 26 + 65),char(floor((`ModSystemAddress` >> 16 & 0x1fffff) / 26 MOD 26) + 65),'-',char(floor((`ModSystemAddress` >> 16 & 0x1fffff) / (26 * 26) MOD 26) + 65),' ',char((`ModSystemAddress` >> 37 & 7) + 97),if(floor((`ModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)) = 0,'',concat(floor((`ModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)),'-')),`ModSystemAddress` & 65535)")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameSysAddr_PGSuffix = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, computedColumnSql: "concat(' ',char((`NameModSystemAddress` >> 16 & 0x1fffff) MOD 26 + 65),char(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / 26 MOD 26) + 65),'-',char(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / (26 * 26) MOD 26) + 65),' ',char((`NameModSystemAddress` >> 37 & 7) + 97),if(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)) = 0,'',concat(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)),'-')),`NameModSystemAddress` & 65535)")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SysAddr_SectorAddress = table.Column<int>(type: "int", nullable: true, computedColumnSql: "`ModSystemAddress` >> 40"),
                    NameSysAddr_SectorAddress = table.Column<int>(type: "int", nullable: true, computedColumnSql: "`NameModSystemAddress` >> 40")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Systems", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SignalInfoSetItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SignalInfoSetId = table.Column<int>(type: "int", nullable: false),
                    SignalInfoId = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalInfoSetItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignalInfoSetItem_SignalInfoSets_SignalInfoSetId",
                        column: x => x.SignalInfoSetId,
                        principalTable: "SignalInfoSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignalInfoSetItem_SignalInfo_SignalInfoId",
                        column: x => x.SignalInfoId,
                        principalTable: "SignalInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FileLineStations",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    GatewayTimestamp = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLineStations", x => new { x.FileId, x.LineNo });
                    table.ForeignKey(
                        name: "FK_FileLineStations_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bodies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    SystemNameId = table.Column<long>(type: "bigint", nullable: true),
                    BodyId = table.Column<int>(type: "int", nullable: true),
                    ParentSetId = table.Column<int>(type: "int", nullable: true),
                    BodyNameId = table.Column<int>(type: "int", nullable: true),
                    BodyDesignationId = table.Column<int>(type: "int", nullable: true),
                    ArgOfPeriapsis = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Inclination = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    SemiMajorAxis = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    SemiMajorAxisScale = table.Column<sbyte>(type: "tinyint", nullable: false),
                    IsRejected = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bodies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bodies_ParentSets_ParentSetId",
                        column: x => x.ParentSetId,
                        principalTable: "ParentSets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bodies_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FileLineInfo",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    LineLength = table.Column<int>(type: "int", nullable: false),
                    SoftwareId = table.Column<int>(type: "int", nullable: true),
                    SystemId = table.Column<int>(type: "int", nullable: true),
                    GameVersionId = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    GatewayTimestamp = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    IsBad = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ProcessedVersion = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLineInfo", x => new { x.FileId, x.LineNo });
                    table.ForeignKey(
                        name: "FK_FileLineInfo_GameVersions_GameVersionId",
                        column: x => x.GameVersionId,
                        principalTable: "GameVersions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileLineInfo_Software_SoftwareId",
                        column: x => x.SoftwareId,
                        principalTable: "Software",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileLineInfo_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FileLineNavRoutes",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    EntryNum = table.Column<int>(type: "int", nullable: false),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    GatewayTimestamp = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLineNavRoutes", x => new { x.FileId, x.LineNo, x.EntryNum });
                    table.ForeignKey(
                        name: "FK_FileLineNavRoutes_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FileLineSignals",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    SignalSetId = table.Column<int>(type: "int", nullable: false),
                    SystemId = table.Column<int>(type: "int", nullable: true),
                    GatewayTimestamp = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLineSignals", x => new { x.FileId, x.LineNo });
                    table.ForeignKey(
                        name: "FK_FileLineSignals_SignalInfoSets_SignalSetId",
                        column: x => x.SignalSetId,
                        principalTable: "SignalInfoSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileLineSignals_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FileLineBodies",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    EntryNum = table.Column<int>(type: "int", nullable: false),
                    BodyId = table.Column<long>(type: "bigint", nullable: false),
                    GatewayTimestamp = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLineBodies", x => new { x.FileId, x.LineNo, x.EntryNum });
                    table.ForeignKey(
                        name: "FK_FileLineBodies_Bodies_BodyId",
                        column: x => x.BodyId,
                        principalTable: "Bodies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FileLineBodySignals",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    EntryNum = table.Column<int>(type: "int", nullable: false),
                    BodySignalId = table.Column<int>(type: "int", nullable: false),
                    BodyId = table.Column<long>(type: "bigint", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    GatewayTimestamp = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLineBodySignals", x => new { x.FileId, x.LineNo, x.EntryNum });
                    table.ForeignKey(
                        name: "FK_FileLineBodySignals_Bodies_BodyId",
                        column: x => x.BodyId,
                        principalTable: "Bodies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileLineBodySignals_BodySignalInfo_BodySignalId",
                        column: x => x.BodySignalId,
                        principalTable: "BodySignalInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_BodyNameId",
                table: "Bodies",
                column: "BodyNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_BodyNameId_SystemId_ParentSetId",
                table: "Bodies",
                columns: new[] { "BodyNameId", "SystemId", "ParentSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_ParentSetId",
                table: "Bodies",
                column: "ParentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_SystemId_BodyId",
                table: "Bodies",
                columns: new[] { "SystemId", "BodyId" });

            migrationBuilder.CreateIndex(
                name: "IX_BodyDesignations_Designation",
                table: "BodyDesignations",
                column: "Designation");

            migrationBuilder.CreateIndex(
                name: "IX_BodyNames_Name",
                table: "BodyNames",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BodySignalInfo_SignalType",
                table: "BodySignalInfo",
                column: "SignalType");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodies_BodyId",
                table: "FileLineBodies",
                column: "BodyId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodySignals_BodyId",
                table: "FileLineBodySignals",
                column: "BodyId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineBodySignals_BodySignalId",
                table: "FileLineBodySignals",
                column: "BodySignalId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineInfo_GameVersionId",
                table: "FileLineInfo",
                column: "GameVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineInfo_SoftwareId",
                table: "FileLineInfo",
                column: "SoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineInfo_SystemId",
                table: "FileLineInfo",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineNavRoutes_SystemId",
                table: "FileLineNavRoutes",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineSignals_SignalSetId",
                table: "FileLineSignals",
                column: "SignalSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineSignals_SystemId",
                table: "FileLineSignals",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FileLineStations_StationId",
                table: "FileLineStations",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_GameVersions_GameVersion_GameBuild_IsOdyssey_IsHorizons",
                table: "GameVersions",
                columns: new[] { "GameVersion", "GameBuild", "IsOdyssey", "IsHorizons" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentSets_BodyID_BodyType_ParentJson",
                table: "ParentSets",
                columns: new[] { "BodyID", "BodyType", "ParentJson" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentSets_BodyID_BodyType_ParentSetId",
                table: "ParentSets",
                columns: new[] { "BodyID", "BodyType", "ParentSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentSets_ParentSetId",
                table: "ParentSets",
                column: "ParentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_IsHASector_Z0_Y0_X0",
                table: "Sectors",
                columns: new[] { "IsHASector", "Z0", "Y0", "X0" });

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_Name",
                table: "Sectors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_SectorAddress",
                table: "Sectors",
                column: "SectorAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SignalInfo_SignalName",
                table: "SignalInfo",
                column: "SignalName");

            migrationBuilder.CreateIndex(
                name: "IX_SignalInfoSetItem_SignalInfoId",
                table: "SignalInfoSetItem",
                column: "SignalInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalInfoSetItem_SignalInfoSetId",
                table: "SignalInfoSetItem",
                column: "SignalInfoSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalInfoSets_FirstSignalId_LastSignalId_SignalCount",
                table: "SignalInfoSets",
                columns: new[] { "FirstSignalId", "LastSignalId", "SignalCount" });

            migrationBuilder.CreateIndex(
                name: "IX_Software_SoftwareName_SoftwareVersion",
                table: "Software",
                columns: new[] { "SoftwareName", "SoftwareVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Stations_MarketId_SystemName_StationName",
                table: "Stations",
                columns: new[] { "MarketId", "SystemName", "StationName" });

            migrationBuilder.CreateIndex(
                name: "IX_Stations_StationName",
                table: "Stations",
                column: "StationName");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_SystemName_StationName_MarketId",
                table: "Stations",
                columns: new[] { "SystemName", "StationName", "MarketId" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNames_Name",
                table: "SystemNames",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Systems_ModSystemAddress",
                table: "Systems",
                column: "ModSystemAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Systems_NameModSystemAddress",
                table: "Systems",
                column: "NameModSystemAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Systems_SystemNameId",
                table: "Systems",
                column: "SystemNameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyDesignations");

            migrationBuilder.DropTable(
                name: "BodyNameOverrides");

            migrationBuilder.DropTable(
                name: "BodyNames");

            migrationBuilder.DropTable(
                name: "FileLineBodies");

            migrationBuilder.DropTable(
                name: "FileLineBodySignals");

            migrationBuilder.DropTable(
                name: "FileLineInfo");

            migrationBuilder.DropTable(
                name: "FileLineNavRoutes");

            migrationBuilder.DropTable(
                name: "FileLineSignals");

            migrationBuilder.DropTable(
                name: "FileLineStations");

            migrationBuilder.DropTable(
                name: "FilePrefixSchemas");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "GameVersionDates");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropTable(
                name: "SignalInfoSetItem");

            migrationBuilder.DropTable(
                name: "SystemNameOverrides");

            migrationBuilder.DropTable(
                name: "SystemNames");

            migrationBuilder.DropTable(
                name: "Bodies");

            migrationBuilder.DropTable(
                name: "BodySignalInfo");

            migrationBuilder.DropTable(
                name: "GameVersions");

            migrationBuilder.DropTable(
                name: "Software");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "SignalInfoSets");

            migrationBuilder.DropTable(
                name: "SignalInfo");

            migrationBuilder.DropTable(
                name: "ParentSets");

            migrationBuilder.DropTable(
                name: "Systems");
        }
    }
}
