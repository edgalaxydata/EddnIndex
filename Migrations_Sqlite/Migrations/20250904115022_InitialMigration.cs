using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EddnIndexUpdate.Migrations_Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BodyDesignations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DesignationId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DesignationType = table.Column<string>(type: "TEXT", nullable: false),
                    StarNum = table.Column<int>(type: "INTEGER", nullable: true),
                    StellarBarycentreLength = table.Column<int>(type: "INTEGER", nullable: true),
                    PlanetNum = table.Column<int>(type: "INTEGER", nullable: true),
                    Moon1Num = table.Column<int>(type: "INTEGER", nullable: true),
                    Moon2Num = table.Column<int>(type: "INTEGER", nullable: true),
                    Moon3Num = table.Column<int>(type: "INTEGER", nullable: true),
                    BarycentreLength = table.Column<int>(type: "INTEGER", nullable: true),
                    RingNum = table.Column<int>(type: "INTEGER", nullable: true),
                    ClusterNum = table.Column<int>(type: "INTEGER", nullable: true),
                    CometNum = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyDesignations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BodyNameOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemAddress = table.Column<long>(type: "INTEGER", nullable: false),
                    SystemName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    BodyID = table.Column<int>(type: "INTEGER", nullable: false),
                    BodyName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    BodyDesignation = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    BodyType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ArgOfPeriapsis = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    Inclination = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    SinceVersion = table.Column<string>(type: "TEXT", nullable: true),
                    UntilVersion = table.Column<string>(type: "TEXT", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyNameOverrides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BodyNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BodySignalInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SignalType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SubCategory = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    EntryID = table.Column<long>(type: "INTEGER", nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SignalCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodySignalInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FilePrefixSchemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilenamePrefix = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PrimarySchema = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    IsTest = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilePrefixSchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PrimarySchema = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    LineCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CompressedSize = table.Column<long>(type: "INTEGER", nullable: true),
                    UncompressedSize = table.Column<long>(type: "INTEGER", nullable: true),
                    SystemLineCount = table.Column<int>(type: "INTEGER", nullable: true),
                    StationLineCount = table.Column<int>(type: "INTEGER", nullable: true),
                    NavRouteSystemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyLineCount = table.Column<int>(type: "INTEGER", nullable: true),
                    SignalCount = table.Column<int>(type: "INTEGER", nullable: true),
                    BodySignalCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: true),
                    IsTest = table.Column<bool>(type: "INTEGER", nullable: true),
                    ProcessedVersion = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.UniqueConstraint("AK_Files_FileName", x => x.FileName);
                });

            migrationBuilder.CreateTable(
                name: "GameVersionDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Season = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    UpdateStartTime = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    UpdateEndTime = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsAlphaOrBeta = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersionDates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GameBuild = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsOdyssey = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsHorizons = table.Column<bool>(type: "INTEGER", nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParentSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BodyID = table.Column<int>(type: "INTEGER", nullable: false),
                    BodyType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ParentSetId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentJson = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentSets_ParentSets_ParentSetId",
                        column: x => x.ParentSetId,
                        principalTable: "ParentSets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    X0 = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    Y0 = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    Z0 = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    SizeX = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    SizeY = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    SizeZ = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    SectorAddress = table.Column<int>(type: "INTEGER", nullable: true),
                    IsHASector = table.Column<bool>(type: "INTEGER", nullable: true),
                    HASectorPriority = table.Column<int>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignalInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SignalName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SignalType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsStation = table.Column<bool>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignalInfoSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstSignalId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSignalId = table.Column<int>(type: "INTEGER", nullable: false),
                    SignalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SignalSetJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalInfoSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Software",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SoftwareVersion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Software", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarketId = table.Column<long>(type: "INTEGER", nullable: true),
                    SystemName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StationName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StationType = table.Column<string>(type: "TEXT", nullable: true),
                    BodyName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SystemAddress = table.Column<long>(type: "INTEGER", nullable: true),
                    Latitude = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    IsRejected = table.Column<bool>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemNameOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemAddress = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    X = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    Y = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    Z = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNameOverrides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Systems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemNameId = table.Column<long>(type: "INTEGER", nullable: true),
                    ModSystemAddress = table.Column<long>(type: "INTEGER", nullable: true),
                    NameModSystemAddress = table.Column<long>(type: "INTEGER", nullable: true),
                    X = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    Y = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    Z = table.Column<decimal>(type: "TEXT", precision: 12, scale: 6, nullable: true),
                    IsRejected = table.Column<bool>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    SectorId = table.Column<int>(type: "INTEGER", nullable: true),
                    SectorAddress = table.Column<int>(type: "INTEGER", nullable: true),
                    PGSuffix = table.Column<string>(type: "TEXT", nullable: true),
                    IsNamedSystem = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsHASystem = table.Column<bool>(type: "INTEGER", nullable: true),
                    SystemAddress = table.Column<long>(type: "INTEGER", nullable: true),
                    SysAddr_PGSuffix = table.Column<string>(type: "TEXT", nullable: true),
                    NameSysAddr_PGSuffix = table.Column<string>(type: "TEXT", nullable: true),
                    SysAddr_SectorAddress = table.Column<int>(type: "INTEGER", nullable: true),
                    NameSysAddr_SectorAddress = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Systems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignalInfoSetItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SignalInfoSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SignalInfoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "FileLineStations",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                    StationId = table.Column<int>(type: "INTEGER", nullable: false),
                    GatewayTimestamp = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "Bodies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemNameId = table.Column<long>(type: "INTEGER", nullable: true),
                    BodyId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentSetId = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyNameId = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyDesignationId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArgOfPeriapsis = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    Inclination = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    SemiMajorAxis = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    SemiMajorAxisScale = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    IsRejected = table.Column<bool>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "FileLineInfo",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                    LineLength = table.Column<int>(type: "INTEGER", nullable: false),
                    SoftwareId = table.Column<int>(type: "INTEGER", nullable: true),
                    SystemId = table.Column<int>(type: "INTEGER", nullable: true),
                    GameVersionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    GatewayTimestamp = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true),
                    IsBad = table.Column<bool>(type: "INTEGER", nullable: true),
                    ProcessedVersion = table.Column<int>(type: "INTEGER", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "FileLineNavRoutes",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryNum = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    GatewayTimestamp = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "FileLineSignals",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                    SignalSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemId = table.Column<int>(type: "INTEGER", nullable: true),
                    GatewayTimestamp = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "FileLineBodies",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryNum = table.Column<int>(type: "INTEGER", nullable: false),
                    BodyId = table.Column<long>(type: "INTEGER", nullable: false),
                    GatewayTimestamp = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "FileLineBodySignals",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryNum = table.Column<int>(type: "INTEGER", nullable: false),
                    BodySignalId = table.Column<int>(type: "INTEGER", nullable: false),
                    BodyId = table.Column<long>(type: "INTEGER", nullable: true),
                    Latitude = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    GatewayTimestamp = table.Column<DateTime>(type: "TEXT", precision: 6, nullable: true)
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
                });

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
