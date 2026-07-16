using EddnIndex.Common.EFConverters;
using Microsoft.EntityFrameworkCore;

namespace EddnIndex.Common.Models;

public class EDDNContext(DbContextOptions<EDDNContext> options) : DbContext(options)
{
    public static bool UseComputedFields { get; } = true;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new UTCDateTimeConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BodyInfo>(m =>
        {
            m.ToTable("Bodies");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.BodyNameId);
            m.HasIndex(e => new { e.BodyNameId, e.SystemId, e.ParentSetId });
            m.HasIndex(e => new { e.SystemId, e.BodyId });
            m.Property(e => e.ArgOfPeriapsis).HasPrecision(9, 6);
            m.Property(e => e.Inclination).HasPrecision(9, 6);
            m.Property(e => e.SemiMajorAxis).HasPrecision(9, 6);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);
            m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
            m.HasOne(e => e.ParentSet).WithMany().HasForeignKey(e => e.ParentSetId).HasPrincipalKey(e => e.Id);
            m.Ignore(e => e.SysName_PGSuffix);
            m.Ignore(e => e.SysName_SectorId);
            m.Ignore(e => e.SysName_SectorAddress);
        });

        modelBuilder.Entity<BodyName>(m =>
        {
            m.ToTable("BodyNames");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.Name);
            m.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<BodyNameOverride>(m =>
        {
            m.ToTable("BodyNameOverrides");
            m.HasKey(e => e.Id);
            m.Property(e => e.SystemName).HasMaxLength(255);
            m.Property(e => e.BodyName).HasMaxLength(255);
            m.Property(e => e.BodyType).HasMaxLength(255);
            m.Property(e => e.BodyDesignation).HasMaxLength(255);
            m.Property(e => e.Inclination).HasPrecision(12, 6);
            m.Property(e => e.ArgOfPeriapsis).HasPrecision(12, 6);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
        });

        modelBuilder.Entity<BodyDesignation>(m =>
        {
            m.ToTable("BodyDesignations");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.Designation);
            m.Property(e => e.Designation).HasMaxLength(128);
            m.Property(e => e.DesignationType).HasConversion<string>();

            if (Database.IsMySql())
            {
                m.Property(e => e.DesignationType).HasColumnType($"enum('{string.Join("','", Enum.GetNames<DesignationType>())}')");
            }
            else if (Database.IsSqlServer())
            {
                m.Property(e => e.DesignationType).HasMaxLength(32);
            }
        });

        modelBuilder.Entity<BodySignalInfo>(m =>
        {
            m.ToTable("BodySignalInfo");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.SignalType);
            m.Property(e => e.SignalType).HasMaxLength(255);
            m.Property(e => e.Category).HasMaxLength(255);
            m.Property(e => e.SubCategory).HasMaxLength(255);
            m.Property(e => e.Region).HasMaxLength(255);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);
        });

        modelBuilder.Entity<FileInfo>(m =>
        {
            m.ToTable("Files");
            m.HasKey(e => e.Id);
            m.HasAlternateKey(e => e.FileName);
            m.Property(e => e.FileName).HasMaxLength(128);
            m.Property(e => e.PrimarySchema).HasMaxLength(128);
            m.Property(e => e.EventType).HasMaxLength(32);
        });

        modelBuilder.Entity<FileLineBody>(m =>
        {
            m.ToTable("FileLineBodies");
            m.HasKey(e => new { e.FileId, e.LineNo, e.EntryNum });
            m.HasIndex(e => e.BodyId);
            m.HasIndex(e => new { e.BodyId, e.GatewayTimestamp });
            m.HasOne(e => e.Body).WithMany().HasForeignKey(e => e.BodyId).HasPrincipalKey(e => e.Id);
            m.Property(e => e.GatewayTimestamp).HasPrecision(6);
        });

        modelBuilder.Entity<FileLineBodySignal>(m =>
        {
            m.ToTable("FileLineBodySignals");
            m.HasKey(e => new { e.FileId, e.LineNo, e.EntryNum });
            m.HasIndex(e => e.BodyId);
            m.HasIndex(e => e.BodySignalId);
            m.HasIndex(e => new { e.BodyId, e.GatewayTimestamp });
            m.HasIndex(e => new { e.BodySignalId, e.GatewayTimestamp });
            m.Property(e => e.Latitude).HasPrecision(9, 6);
            m.Property(e => e.Longitude).HasPrecision(9, 6);
            m.Property(e => e.GatewayTimestamp).HasPrecision(6);
            m.HasOne(e => e.Body).WithMany().HasForeignKey(e => e.BodyId).HasPrincipalKey(e => e.Id);
            m.HasOne(e => e.Signal).WithMany().HasForeignKey(e => e.BodySignalId).HasPrincipalKey(e => e.Id);
        });

        modelBuilder.Entity<FileLineInfo>(m =>
        {
            m.ToTable("FileLineInfo");
            m.HasKey(e => new { e.FileId, e.LineNo });
            m.HasIndex(e => e.SystemId);
            m.HasIndex(e => new { e.SystemId, e.GatewayTimestamp });
            m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
            m.HasOne(e => e.Software).WithMany().HasForeignKey(e => e.SoftwareId).HasPrincipalKey(e => e.Id);
            m.HasOne(e => e.GameVersion).WithMany().HasForeignKey(e => e.GameVersionId).HasPrincipalKey(e => e.Id);
            m.Property(e => e.Timestamp).HasPrecision(0);
            m.Property(e => e.GatewayTimestamp).HasPrecision(6);
        });

        modelBuilder.Entity<FileLineNavRoute>(m =>
        {
            m.ToTable("FileLineNavRoutes");
            m.HasKey(e => new { e.FileId, e.LineNo, e.EntryNum });
            m.HasIndex(e => e.SystemId);
            m.HasIndex(e => new { e.SystemId, e.GatewayTimestamp });
            m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
            m.Property(e => e.GatewayTimestamp).HasPrecision(6);
        });

        modelBuilder.Entity<FileLineSignal>(m =>
        {
            m.ToTable("FileLineSignals");
            m.HasKey(e => new { e.FileId, e.LineNo });
            m.HasIndex(e => e.SignalSetId);
            m.HasIndex(e => e.SystemId);
            m.HasIndex(e => new { e.SignalSetId, e.GatewayTimestamp });
            m.HasIndex(e => new { e.SystemId, e.GatewayTimestamp });
            m.HasOne(e => e.SignalInfoSet).WithMany().HasForeignKey(e => e.SignalSetId).HasPrincipalKey(e => e.Id);
            m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
            m.Property(e => e.GatewayTimestamp).HasPrecision(6);
        });

        modelBuilder.Entity<FileLineStation>(m =>
        {
            m.ToTable("FileLineStations");
            m.HasKey(e => new { e.FileId, e.LineNo });
            m.HasIndex(e => e.StationId);
            m.HasIndex(e => new { e.StationId, e.GatewayTimestamp });
            m.HasOne(e => e.Station).WithMany().HasForeignKey(e => e.StationId).HasPrincipalKey(e => e.Id);
            m.Property(e => e.GatewayTimestamp).HasPrecision(6);
        });

        modelBuilder.Entity<GameVersionDate>(m =>
        {
            m.ToTable("GameVersionDates");
            m.HasKey(e => e.Id);
            m.Property(e => e.Season).HasMaxLength(50);
            m.Property(e => e.Version).HasMaxLength(50);
            m.Property(e => e.Description).HasMaxLength(100);
            m.Property(e => e.UpdateTime).HasPrecision(0);
            m.Property(e => e.UpdateStartTime).HasPrecision(0);
            m.Property(e => e.UpdateEndTime).HasPrecision(0);
        });

        modelBuilder.Entity<GameVersionInfo>(m =>
        {
            m.ToTable("GameVersions");
            m.HasKey(e => e.Id);
            m.HasIndex(e => new { e.GameVersion, e.GameBuild, e.IsOdyssey, e.IsHorizons });
            m.Property(e => e.GameVersion).HasMaxLength(100);
            m.Property(e => e.GameBuild).HasMaxLength(100);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);
        });

        modelBuilder.Entity<ParentSet>(m =>
        {
            m.ToTable("ParentSets");
            m.HasKey(e => e.Id);
            m.HasIndex(e => new { e.BodyID, e.BodyType, e.ParentJson });
            m.HasIndex(e => new { e.BodyID, e.BodyType, e.ParentSetId });
            m.Property(e => e.ParentJson).HasMaxLength(255);
            m.Property(e => e.BodyType).HasMaxLength(50);
            m.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentSetId).HasPrincipalKey(e => e.Id);
        });

        modelBuilder.Entity<Sector>(m =>
        {
            m.ToTable("Sectors");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.Name);
            m.HasIndex(e => e.SectorAddress);
            m.HasIndex(e => new { e.IsHASector, e.Z0, e.Y0, e.X0 });
            m.Property(e => e.Name).HasMaxLength(128);
            m.Property(e => e.X0).HasPrecision(12, 6);
            m.Property(e => e.Y0).HasPrecision(12, 6);
            m.Property(e => e.Z0).HasPrecision(12, 6);
            m.Property(e => e.SizeX).HasPrecision(12, 6);
            m.Property(e => e.SizeY).HasPrecision(12, 6);
            m.Property(e => e.SizeZ).HasPrecision(12, 6);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);
        });

        modelBuilder.Entity<SignalInfo>(m =>
        {
            m.ToTable("SignalInfo");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.SignalName);
            m.Property(e => e.SignalName).HasMaxLength(255);
            m.Property(e => e.SignalType).HasMaxLength(255);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);
        });

        modelBuilder.Entity<SignalInfoSet>(m =>
        {
            m.ToTable("SignalInfoSets");
            m.HasKey(e => e.Id);
            m.HasIndex(e => new { e.FirstSignalId, e.LastSignalId, e.SignalCount });
            m.HasMany(e => e.SignalSetItems).WithOne().HasForeignKey(e => e.SignalInfoSetId).HasPrincipalKey(e => e.Id);
            m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
        });

        modelBuilder.Entity<SignalInfoSetItem>(m =>
        {
            m.ToTable("SignalInfoSetItem");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.SignalInfoId);
            m.HasIndex(e => e.SignalInfoSetId);
            m.HasIndex(e => new { e.SignalInfoId, e.SystemId });
            m.HasIndex(e => new { e.SignalInfoId, e.LastSeen, e.FirstSeen, e.SystemId });
            m.HasOne(e => e.Signal).WithMany().HasForeignKey(e => e.SignalInfoId).HasPrincipalKey(e => e.Id);
            m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
        });

        modelBuilder.Entity<SoftwareInfo>(m =>
        {
            m.ToTable("Software");
            m.HasKey(e => e.Id);
            m.HasIndex(e => new { e.SoftwareName, e.SoftwareVersion });
            m.Property(e => e.SoftwareName).HasMaxLength(255);
            m.Property(e => e.SoftwareVersion).HasMaxLength(255);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);
        });

        modelBuilder.Entity<SchemaEventInfo>(m =>
        {
            m.ToTable("SchemaEvents");
            m.HasKey(e => e.Id);
            m.HasIndex(e => new { e.Schema, e.EventType });
            m.Property(e => e.Schema).HasMaxLength(128);
            m.Property(e => e.EventType).HasMaxLength(32);
        });

        modelBuilder.Entity<StationInfo>(m =>
        {
            m.ToTable("Stations");
            m.HasKey(e => e.Id);
            m.HasIndex(e => new { e.SystemName, e.StationName, e.MarketId });
            m.HasIndex(e => new { e.MarketId, e.SystemName, e.StationName });
            m.HasIndex(e => e.StationName);
            m.Property(e => e.SystemName).HasMaxLength(128);
            m.Property(e => e.StationName).HasMaxLength(128);
            m.Property(e => e.BodyName).HasMaxLength(128);
            m.Property(e => e.Latitude).HasPrecision(9, 6);
            m.Property(e => e.Longitude).HasPrecision(9, 6);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);
        });

        modelBuilder.Entity<SystemInfo>(m =>
        {
            m.ToTable("Systems");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.SystemNameId);
            m.HasIndex(e => e.ModSystemAddress);
            m.HasIndex(e => e.NameModSystemAddress);
            m.Property(e => e.X).HasPrecision(12, 6);
            m.Property(e => e.Y).HasPrecision(12, 6);
            m.Property(e => e.Z).HasPrecision(12, 6);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
            m.Property(e => e.FirstSeen).HasPrecision(6);
            m.Property(e => e.LastSeen).HasPrecision(6);

            if (Database.IsMySql())
            {
                m.Property(e => e.SectorId).HasComputedColumnSql("if(`SystemNameId` >= 1 << 60,(`SystemNameId` >> 40) - 0x100000,NULL)");
                m.Property(e => e.SectorAddress).HasComputedColumnSql("if(`SystemNameId` > 0 and `SystemNameId` < 1 << 60,`SystemNameId` >> 40,NULL)");
                m.Property(e => e.PGSuffix).HasMaxLength(128).HasComputedColumnSql("if(`SystemNameId` >= 0,concat(' ',char((`SystemNameId` >> 16 & 0x1fffff) MOD 26 + 65),char(floor((`SystemNameId` >> 16 & 0x1fffff) / 26 MOD 26) + 65),'-',char(floor((`SystemNameId` >> 16 & 0x1fffff) / (26 * 26) MOD 26) + 65),' ',char((`SystemNameId` >> 37 & 7) + 97),if(floor((`SystemNameId` >> 16 & 0x1fffff) / (26 * 26 * 26)) = 0,'',concat(floor((`SystemNameId` >> 16 & 0x1fffff) / (26 * 26 * 26)),'-')),`SystemNameId` & 65535),NULL)");
                m.Property(e => e.IsNamedSystem).HasComputedColumnSql("`SystemNameId` < 0");
                m.Property(e => e.IsHASystem).HasComputedColumnSql("`SystemNameId` >= 1 << 60");
                m.Property(e => e.SystemAddress).HasComputedColumnSql("(`ModSystemAddress` & 0xffff) << 44 - (`ModSystemAddress` >> 37 & 7) * 3 | (`ModSystemAddress` >> 40 & 0x7f) << 37 - (`ModSystemAddress` >> 37 & 7) * 3 | (`ModSystemAddress` >> 16 & 0x7f) << 30 - (`ModSystemAddress` >> 37 & 7) * 2 | (`ModSystemAddress` >> 47 & 0x3f) << 24 - (`ModSystemAddress` >> 37 & 7) * 2 | (`ModSystemAddress` >> 23 & 0x7f) << 17 - (`ModSystemAddress` >> 37 & 7) * 1 | (`ModSystemAddress` >> 53 & 0x7f) << 10 - (`ModSystemAddress` >> 37 & 7) * 1 | (`ModSystemAddress` >> 30 & 0x7f) << 3 | `ModSystemAddress` >> 37 & 7");
                m.Property(e => e.SysAddr_PGSuffix).HasMaxLength(128).HasComputedColumnSql("concat(' ',char((`ModSystemAddress` >> 16 & 0x1fffff) MOD 26 + 65),char(floor((`ModSystemAddress` >> 16 & 0x1fffff) / 26 MOD 26) + 65),'-',char(floor((`ModSystemAddress` >> 16 & 0x1fffff) / (26 * 26) MOD 26) + 65),' ',char((`ModSystemAddress` >> 37 & 7) + 97),if(floor((`ModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)) = 0,'',concat(floor((`ModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)),'-')),`ModSystemAddress` & 65535)");
                m.Property(e => e.NameSysAddr_PGSuffix).HasMaxLength(128).HasComputedColumnSql("concat(' ',char((`NameModSystemAddress` >> 16 & 0x1fffff) MOD 26 + 65),char(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / 26 MOD 26) + 65),'-',char(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / (26 * 26) MOD 26) + 65),' ',char((`NameModSystemAddress` >> 37 & 7) + 97),if(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)) = 0,'',concat(floor((`NameModSystemAddress` >> 16 & 0x1fffff) / (26 * 26 * 26)),'-')),`NameModSystemAddress` & 65535)");
                m.Property(e => e.SysAddr_SectorAddress).HasComputedColumnSql("`ModSystemAddress` >> 40");
                m.Property(e => e.NameSysAddr_SectorAddress).HasComputedColumnSql("`NameModSystemAddress` >> 40");
            }
            else if (Database.IsSqlServer())
            {
                m.Property(e => e.SectorId).HasComputedColumnSql("CAST(IIF(SystemNameId >= 0x1000000000000000, (SystemNameId >> 40) - 0x100000, NULL) AS INT)");
                m.Property(e => e.SectorAddress).HasComputedColumnSql("CAST(IIF(SystemNameId >= 0 AND SystemNameId < 0x1000000000000000, (SystemNameId >> 40), NULL) AS INT)");
                m.Property(e => e.PGSuffix).HasComputedColumnSql("IIF(SystemNameId >= 0,CONCAT(' ',CHAR((SystemNameId >> 16 & 0x1FFFFF) % 26 + 65),CHAR(FLOOR((SystemNameId >> 16 & 0x1FFFFF) / 26) % 26 + 65),'-',CHAR(FLOOR((SystemNameId >> 16 & 0x1FFFFF) / (26 * 26)) % 26 + 65),' ',CHAR(SystemNameId >> 37 & 7 + 97),IIF(FLOOR((SystemNameId >> 16 & 0x1FFFFF) / (26 * 26 * 26)) > 0,CONCAT(FLOOR((SystemNameId >> 16 & 0x1FFFFF) / (26 * 26 * 26)), '-'),''),SystemNameId & 0xFFFF),NULL)");
                m.Property(e => e.IsNamedSystem).HasComputedColumnSql("CAST(IIF(SystemNameId < 0, 1, 0) AS BIT)");
                m.Property(e => e.IsHASystem).HasComputedColumnSql("CAST(IIF(SystemNameId >= 0x1000000000000000, 1, 0) AS BIT)");
                m.Property(e => e.SystemAddress).HasComputedColumnSql("((ModSystemAddress & 0xFFFF) << (44 - (ModSystemAddress >> 37 & 7) * 3)) | ((ModSystemAddress >> 40 & 0x7F) << (37 - (ModSystemAddress >> 37 & 7) * 3)) | ((ModSystemAddress >> 16 & 0x7F) << (30 - (ModSystemAddress >> 37 & 7) * 2)) | ((ModSystemAddress >> 47 & 0x3F) << (24 - (ModSystemAddress >> 37 & 7) * 2)) | ((ModSystemAddress >> 23 & 0x7F) << (17 - (ModSystemAddress >> 37 & 7) * 1)) | ((ModSystemAddress >> 53 & 0x7F) << (10 - (ModSystemAddress >> 37 & 7) * 1)) | ((ModSystemAddress >> 16 & 0x7F) << 3) | (ModSystemAddress >> 37 & 7)");
                m.Property(e => e.SysAddr_PGSuffix).HasComputedColumnSql("CONCAT(' ',CHAR((ModSystemAddress >> 16 & 0x1FFFFF) % 26 + 65),CHAR(FLOOR((ModSystemAddress >> 16 & 0x1FFFFF) / 26) % 26 + 65),'-',CHAR(FLOOR((ModSystemAddress >> 16 & 0x1FFFFF) / (26 * 26)) % 26 + 65),' ',CHAR(ModSystemAddress >> 37 & 7 + 97),IIF(FLOOR((ModSystemAddress >> 16 & 0x1FFFFF) / (26 * 26 * 26)) > 0,CONCAT(FLOOR((ModSystemAddress >> 16 & 0x1FFFFF) / (26 * 26 * 26)), '-'),''),ModSystemAddress & 0xFFFF)");
                m.Property(e => e.NameSysAddr_PGSuffix).HasComputedColumnSql("CONCAT(' ',CHAR((NameModSystemAddress >> 16 & 0x1FFFFF) % 26 + 65),CHAR(FLOOR((NameModSystemAddress >> 16 & 0x1FFFFF) / 26) % 26 + 65),'-',CHAR(FLOOR((NameModSystemAddress >> 16 & 0x1FFFFF) / (26 * 26)) % 26 + 65),' ',CHAR(NameModSystemAddress >> 37 & 7 + 97),IIF(FLOOR((NameModSystemAddress >> 16 & 0x1FFFFF) / (26 * 26 * 26)) > 0,CONCAT(FLOOR((NameModSystemAddress >> 16 & 0x1FFFFF) / (26 * 26 * 26)), '-'),''),NameModSystemAddress & 0xFFFF)");
                m.Property(e => e.SysAddr_SectorAddress).HasComputedColumnSql("CAST(ModSystemAddress >> 40 AS INT)");
                m.Property(e => e.NameSysAddr_SectorAddress).HasComputedColumnSql("CAST(NameModSystemAddress >> 40 AS INT)");
            }
        });

        modelBuilder.Entity<SystemName>(m =>
        {
            m.ToTable("SystemNames");
            m.HasKey(e => e.Id);
            m.HasIndex(e => e.Name);
            m.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<SystemNameOverride>(m =>
        {
            m.ToTable("SystemNameOverrides");
            m.HasKey(e => e.Id);
            m.Property(e => e.Name).HasMaxLength(255);
            m.Property(e => e.X).HasPrecision(12, 6);
            m.Property(e => e.Y).HasPrecision(12, 6);
            m.Property(e => e.Z).HasPrecision(12, 6);
            m.Property(e => e.ValidFrom).HasPrecision(0);
            m.Property(e => e.ValidTo).HasPrecision(0);
        });

        modelBuilder.Entity<FilePrefixSchema>(m =>
        {
            m.ToTable("FilePrefixSchemas");
            m.HasKey(e => e.Id);
            m.Property(e => e.FilenamePrefix).HasMaxLength(128);
            m.Property(e => e.PrimarySchema).HasMaxLength(128);
            m.Property(e => e.EventType).HasMaxLength(32);
        });
    }

    public virtual async Task<Dictionary<int, int>> GetSystemMatchCountsAsync(ICollection<int> systemIds, CancellationToken canceltoken)
    {
        return await Set<FileLineInfo>()
            .Where(e => e.SystemId != null && systemIds.Contains(e.SystemId.Value))
            .GroupBy(e => e.SystemId!.Value)
            .Select(g => new { SystemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(e => e.SystemId, e => e.Count, cancellationToken: canceltoken);
    }

    public virtual async Task<Dictionary<long, int>> GetBodyMatchCountsAsync(ICollection<long> bodyIds, CancellationToken canceltoken)
    {
        return await Set<FileLineBody>()
            .Where(e => bodyIds.Contains(e.BodyId))
            .GroupBy(e => e.BodyId)
            .Select(g => new { BodyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(e => e.BodyId, e => e.Count, cancellationToken: canceltoken);
    }

    public virtual async Task<Dictionary<int, int>> GetStationMatchCountsAsync(ICollection<int> stationIds, CancellationToken canceltoken)
    {
        return await Set<FileLineStation>()
            .Where(e => stationIds.Contains(e.StationId))
            .GroupBy(e => e.StationId)
            .Select(g => new { StationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(e => e.StationId, e => e.Count, cancellationToken: canceltoken);
    }

    public virtual async Task<Dictionary<int, int>> GetSignalMatchCountsAsync(Dictionary<int, List<int>> signalSetIds, CancellationToken canceltoken)
    {
        return await signalSetIds
            .ToAsyncEnumerable()
            .Where(e => e.Value.Count < 1000)
            .ToDictionaryAsync(
                async (kvp, ct) => await ValueTask.FromResult(kvp.Key),
                async (kvp, ct) => await Set<FileLineSignal>().CountAsync(e => kvp.Value.Contains(e.SignalSetId), canceltoken),
                cancellationToken: canceltoken
            );
    }

    public virtual IQueryable<(FileInfo File, FileLineInfo Info, FileLineBody? Body, FileLineStation? Station)> QuerySystemMatchLines(
            int systemId,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            int? maxResults
        )
    {
        var query = Set<FileLineInfo>()
            .Where(e => e.SystemId == systemId)
            .Include(e => e.Software)
            .Include(e => e.SchemaEvent)
            .Include(e => e.GameVersion)
            .LeftJoin(
                Set<FileInfo>(),
                o => o.FileId,
                i => i.Id,
                (o, i) => new { Info = o, File = i }
            )
            .LeftJoin(
                Set<FileLineBody>(),
                o => new { o.Info.FileId, o.Info.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.File, o.Info, Body = i }
            )
            .LeftJoin(
                Set<FileLineStation>()
                    .Include(e => e.Station),
                o => new { o.Info.FileId, o.Info.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.File, o.Info, o.Body, Station = i }
            );

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.Info.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.Info.GatewayTimestamp <= maxTS);
        }

        return
            query
                .OrderByDescending(e => e.Info.GatewayTimestamp)
                .Take(maxResults ?? 1000)
                .Where(e => e.File != null)
                .Select(e => new ValueTuple<FileInfo, FileLineInfo, FileLineBody?, FileLineStation?>(
                    e.File!,
                    e.Info,
                    e.Body,
                    e.Station
                 ));
    }

    public virtual IQueryable<(FileInfo File, FileLineNavRoute RouteEntry, FileLineInfo Info)> QuerySystemRouteMatchLines(
            int systemId,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            int? maxResults
        )
    {
        var query = Set<FileLineNavRoute>()
            .Where(e => e.SystemId == systemId)
            .LeftJoin(
                Set<FileInfo>(),
                o => o.FileId,
                i => i.Id,
                (o, i) => new { RouteEntry = o, File = i }
            )
            .LeftJoin(
                Set<FileLineInfo>()
                    .Include(e => e.Software)
                    .Include(e => e.SchemaEvent)
                    .Include(e => e.GameVersion),
                o => new { o.RouteEntry.FileId, o.RouteEntry.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.File, Info = i, o.RouteEntry }
            );

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.RouteEntry.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.RouteEntry.GatewayTimestamp <= maxTS);
        }

        return
            query
                .OrderByDescending(e => e.RouteEntry.GatewayTimestamp)
                .Take(maxResults ?? 1000)
                .Where(e => e.File != null && e.Info != null)
                .Select(e => new ValueTuple<FileInfo, FileLineNavRoute, FileLineInfo>(
                    e.File!,
                    e.RouteEntry,
                    e.Info!
                ));
    }

    public virtual IQueryable<(FileInfo File, FileLineBody Body, FileLineInfo Info, FileLineStation? Station)> QueryBodyMatchLines(
            long bodyId,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            int? maxResults
        )
    {
        var query = Set<FileLineBody>()
            .Where(e => e.BodyId == bodyId)
            .LeftJoin(
                Set<FileInfo>(),
                o => o.FileId,
                i => i.Id,
                (o, i) => new { Body = o, File = i }
            )
            .LeftJoin(
                Set<FileLineInfo>()
                    .Include(e => e.Software)
                    .Include(e => e.SchemaEvent)
                    .Include(e => e.GameVersion),
                o => new { o.Body.FileId, o.Body.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.File, Info = i, o.Body }
            )
            .LeftJoin(
                Set<FileLineStation>()
                    .Include(e => e.Station),
                o => new { o.Body.FileId, o.Body.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.File, o.Info, o.Body, Station = i }
            );

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.Body.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.Body.GatewayTimestamp <= maxTS);
        }

        return
            query
                .OrderByDescending(e => e.Body.GatewayTimestamp)
                .Take(maxResults ?? 1000)
                .Where(e => e.File != null && e.Info != null)
                .Select(e => new ValueTuple<FileInfo, FileLineBody, FileLineInfo, FileLineStation?>(
                    e.File!,
                    e.Body,
                    e.Info!,
                    e.Station
                 ));
    }

    public virtual IQueryable<(FileInfo File, FileLineStation Station, FileLineInfo Info, FileLineBody? Body)> QueryStationMatchLines(
            int stationId,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            int? maxResults
        )
    {
        var query = Set<FileLineStation>()
            .Where(e => e.StationId == stationId)
            .LeftJoin(
                Set<FileInfo>(),
                o => o.FileId,
                i => i.Id,
                (o, i) => new { Station = o, File = i }
            )
            .LeftJoin(
                Set<FileLineInfo>()
                    .Include(e => e.Software)
                    .Include(e => e.SchemaEvent)
                    .Include(e => e.GameVersion),
                o => new { o.Station.FileId, o.Station.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.File, Info = i, o.Station }
            )
            .LeftJoin(
                Set<FileLineBody>(),
                o => new { o.Station.FileId, o.Station.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.File, o.Info, Body = i, o.Station }
            );

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.Station.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.Station.GatewayTimestamp <= maxTS);
        }

        return
            query
                .OrderByDescending(e => e.Station.GatewayTimestamp)
                .Take(maxResults ?? 1000)
                .Where(e => e.File != null && e.Info != null)
                .Select(e => new ValueTuple<FileInfo, FileLineStation, FileLineInfo, FileLineBody?>(
                    e.File!,
                    e.Station,
                    e.Info!,
                    e.Body
                 ));
    }

    public virtual IQueryable<(FileInfo File, FileLineSignal SignalLine, FileLineInfo Info)> QuerySignalMatchLines(
            List<int> signalSetIds,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            int? maxResults
        )
    {
        var query = Set<FileLineSignal>()
            .Where(e => signalSetIds.Contains(e.SignalSetId))
            .LeftJoin(
                Set<FileInfo>(),
                o => o.FileId,
                i => i.Id,
                (o, i) => new { SignalLine = o, File = i }
            )
            .LeftJoin(
                Set<FileLineInfo>()
                    .Include(e => e.Software)
                    .Include(e => e.SchemaEvent)
                    .Include(e => e.GameVersion),
                o => new { o.SignalLine.FileId, o.SignalLine.LineNo },
                i => new { i.FileId, i.LineNo },
                (o, i) => new { o.SignalLine, o.File, Info = i }
            );

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.SignalLine.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.SignalLine.GatewayTimestamp <= maxTS);
        }

        return
            query
                .OrderByDescending(e => e.SignalLine.GatewayTimestamp)
                .Take(maxResults ?? 1000)
                .Where(e => e.File != null && e.Info != null)
                .Select(e => new ValueTuple<FileInfo, FileLineSignal, FileLineInfo>(
                    e.File!,
                    e.SignalLine,
                    e.Info!
                 ));
    }
}
