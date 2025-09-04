using Microsoft.EntityFrameworkCore;

namespace EddnIndexUpdate.Models
{
    public class EDDNContext(DbContextOptions<EDDNContext> options) : DbContext(options)
    {
        private static DateTime? TruncateDateTime(DateTime? val)
        {
            if (val is not DateTime dt) return null;
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Body>(m =>
            {
                m.ToTable("Bodies");
                m.HasKey(e => e.Id);
                m.HasIndex(e => e.BodyNameId);
                m.HasIndex(e => new { e.BodyNameId, e.SystemId, e.ParentSetId });
                m.HasIndex(e => new { e.SystemId, e.BodyId });
                m.Property(e => e.ArgOfPeriapsis).HasPrecision(9, 6);
                m.Property(e => e.Inclination).HasPrecision(9, 6);
                m.Property(e => e.SemiMajorAxis).HasPrecision(9, 6);
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
                m.HasOne(e => e.ParentSet).WithMany().HasForeignKey(e => e.ParentSetId).HasPrincipalKey(e => e.Id);
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
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
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
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<File>(m =>
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
                m.HasOne(e => e.Body).WithMany().HasForeignKey(e => e.BodyId).HasPrincipalKey(e => e.Id);
                m.Property(e => e.GatewayTimestamp).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<FileLineBodySignal>(m =>
            {
                m.ToTable("FileLineBodySignals");
                m.HasKey(e => new { e.FileId, e.LineNo, e.EntryNum });
                m.HasIndex(e => e.BodyId);
                m.HasIndex(e => e.BodySignalId);
                m.Property(e => e.Latitude).HasPrecision(9, 6);
                m.Property(e => e.Longitude).HasPrecision(9, 6);
                m.Property(e => e.GatewayTimestamp).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.HasOne(e => e.Body).WithMany().HasForeignKey(e => e.BodyId).HasPrincipalKey(e => e.Id);
                m.HasOne(e => e.Signal).WithMany().HasForeignKey(e => e.BodySignalId).HasPrincipalKey(e => e.Id);
            });

            modelBuilder.Entity<FileLineInfo>(m =>
            {
                m.ToTable("FileLineInfo");
                m.HasKey(e => new { e.FileId, e.LineNo });
                m.HasIndex(e => e.SystemId);
                m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
                m.HasOne(e => e.Software).WithMany().HasForeignKey(e => e.SoftwareId).HasPrincipalKey(e => e.Id);
                m.HasOne(e => e.GameVersion).WithMany().HasForeignKey(e => e.GameVersionId).HasPrincipalKey(e => e.Id);
                m.Property(e => e.Timestamp).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.GatewayTimestamp).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<FileLineNavRoute>(m =>
            {
                m.ToTable("FileLineNavRoutes");
                m.HasKey(e => new { e.FileId, e.LineNo, e.EntryNum });
                m.HasIndex(e => e.SystemId);
                m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
                m.Property(e => e.GatewayTimestamp).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<FileLineSignal>(m =>
            {
                m.ToTable("FileLineSignals");
                m.HasKey(e => new { e.FileId, e.LineNo });
                m.HasIndex(e => e.SignalSetId);
                m.HasIndex(e => e.SystemId);
                m.HasOne(e => e.SignalInfoSet).WithMany().HasForeignKey(e => e.SignalSetId).HasPrincipalKey(e => e.Id);
                m.HasOne(e => e.System).WithMany().HasForeignKey(e => e.SystemId).HasPrincipalKey(e => e.Id);
                m.Property(e => e.GatewayTimestamp).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<FileLineStation>(m =>
            {
                m.ToTable("FileLineStations");
                m.HasKey(e => new { e.FileId, e.LineNo });
                m.HasIndex(e => e.StationId);
                m.HasOne(e => e.Station).WithMany().HasForeignKey(e => e.StationId).HasPrincipalKey(e => e.Id);
                m.Property(e => e.GatewayTimestamp).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<GameVersionDate>(m =>
            {
                m.ToTable("GameVersionDates");
                m.HasKey(e => e.Id);
                m.Property(e => e.Season).HasMaxLength(50);
                m.Property(e => e.Version).HasMaxLength(50);
                m.Property(e => e.Description).HasMaxLength(100);
                m.Property(e => e.UpdateTime).HasPrecision(0).HasConversion(e => (DateTime)TruncateDateTime(e)!, e => DateTime.SpecifyKind(e, DateTimeKind.Utc));
                m.Property(e => e.UpdateStartTime).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.UpdateEndTime).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<GameVersionInfo>(m =>
            {
                m.ToTable("GameVersions");
                m.HasKey(e => e.Id);
                m.HasIndex(e => new { e.GameVersion, e.GameBuild, e.IsOdyssey, e.IsHorizons });
                m.Property(e => e.GameVersion).HasMaxLength(100);
                m.Property(e => e.GameBuild).HasMaxLength(100);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
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
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<SignalInfo>(m =>
            {
                m.ToTable("SignalInfo");
                m.HasKey(e => e.Id);
                m.HasIndex(e => e.SignalName);
                m.Property(e => e.SignalName).HasMaxLength(255);
                m.Property(e => e.SignalType).HasMaxLength(255);
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<SignalInfoSet>(m =>
            {
                m.ToTable("SignalInfoSets");
                m.HasKey(e => e.Id);
                m.HasIndex(e => new { e.FirstSignalId, e.LastSignalId, e.SignalCount });
                m.OwnsMany(e => e.SignalSetItems, o =>
                {
                    o.HasKey(e => e.Id);
                    o.WithOwner().HasForeignKey(e => e.SignalInfoSetId).HasPrincipalKey(e => e.Id);
                    o.HasOne(e => e.Signal).WithMany().HasForeignKey(e => e.SignalInfoId).HasPrincipalKey(e => e.Id);
                });
            });

            modelBuilder.Entity<SoftwareInfo>(m =>
            {
                m.ToTable("Software");
                m.HasKey(e => e.Id);
                m.HasIndex(e => new { e.SoftwareName, e.SoftwareVersion });
                m.Property(e => e.SoftwareName).HasMaxLength(255);
                m.Property(e => e.SoftwareVersion).HasMaxLength(255);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<Station>(m =>
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
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<System>(m =>
            {
                m.ToTable("Systems");
                m.HasKey(e => e.Id);
                m.HasIndex(e => e.SystemNameId);
                m.HasIndex(e => e.ModSystemAddress);
                m.HasIndex(e => e.NameModSystemAddress);
                m.Property(e => e.X).HasPrecision(12, 6);
                m.Property(e => e.Y).HasPrecision(12, 6);
                m.Property(e => e.Z).HasPrecision(12, 6);
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.FirstSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.LastSeen).HasPrecision(6).HasConversion(e => e, e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);

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
                m.Property(e => e.ValidFrom).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
                m.Property(e => e.ValidTo).HasPrecision(0).HasConversion(e => TruncateDateTime(e), e => e != null ? DateTime.SpecifyKind((DateTime)e, DateTimeKind.Utc) : null);
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
    }
}
