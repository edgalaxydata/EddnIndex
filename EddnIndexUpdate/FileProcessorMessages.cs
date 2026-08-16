using Microsoft.Extensions.Logging;

namespace EddnIndexUpdate;

internal static partial class FileProcessorMessages
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Assert failure:\n{Message}\nExtraData={ExtraData}")]
    public static partial void LogAssertFailure(this ILogger logger, string? message, string? extraData);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading file info")]
    public static partial void LogLoadingFileInfo(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading file errors")]
    public static partial void LogLoadingFileErrors(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading software versions")]
    public static partial void LogLoadingSoftwareVersions(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading game versions")]
    public static partial void LogLoadingGameVersions(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading signals")]
    public static partial void LogLoadingSignals(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading schema events")]
    public static partial void LogLoadingSchemaEvents(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading body signals")]
    public static partial void LogLoadingBodySignals(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading stations")]
    public static partial void LogLoadingStations(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading signal counts")]
    public static partial void LogLoadingSignalCounts(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Writing indexed file {Filename}")]
    public static partial void LogWritingIndexedFile(this ILogger logger, string filename);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Filename}")]
    public static partial void LogProcessingFile(this ILogger logger, string filename);

    [LoggerMessage(Level = LogLevel.Information, Message = "Current: S:{CurLength} U:{CurUncLen} L:{CurLineCount} E:{CurErrorCount} V:{CurVersion} -> S:{UpdLength} V:{UpdVersion}")]
    public static partial void LogProcessingFileState(
        this ILogger logger,
        long? curLength,
        long? curUncLen,
        int? curLineCount,
        int? curErrorCount,
        int? curVersion,
        long updLength,
        int updVersion);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in file {FileName} line number {LineNo}: incomplete message")]
    public static partial void LogIncompleteMessage(this ILogger logger, string fileName, int lineNo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in file {FileName} line number {LineNo}: no data available")]
    public static partial void LogNoDataAvailable(this ILogger logger, string fileName, int lineNo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in file {FileName} line number {LineNo}: {Message}")]
    public static partial void LogBadData(this ILogger logger, Exception? exception, string fileName, int lineNo, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading body names")]
    public static partial void LogLoadingBodyNames(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading body designations")]
    public static partial void LogLoadingBodyDesignations(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading parent sets")]
    public static partial void LogLoadingParentSets(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Encountered potential anomalous body name parsing case. Name: {BodyName}, SystemName: {SystemName}")]
    public static partial void LogPotentialAnomalousBodyNameParsingCase(this ILogger logger, string bodyName, string systemName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Multiple body designation overrides matched. SystemName={SystemName}, BodyId={BodyId}, BodyType={BodyType}, Timestamp={Timestamp}, MatchCount={MatchCount}")]
    public static partial void LogMultipleBodyDesignationOverridesMatched(this ILogger logger, string? systemName, int? bodyId, string? bodyType, DateTime? timestamp, int matchCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading message types")]
    public static partial void LogLoadingMessageTypes(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process message types file")]
    public static partial void LogProcessingMessageTypesFile(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading game version dates")]
    public static partial void LogLoadingGameVersionDates(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieving game version dates")]
    public static partial void LogRetrievingGameVersionDates(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading system name overrides")]
    public static partial void LogLoadingSystemNameOverrides(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieving system name overrides")]
    public static partial void LogRetrievingSystemNameOverrides(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading body name overrides")]
    public static partial void LogLoadingBodyNameOverrides(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieving body overrides")]
    public static partial void LogRetrievingBodyOverrides(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading sectors")]
    public static partial void LogLoadingSectors(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing complete")]
    public static partial void LogProcessingComplete(this ILogger logger);
}
