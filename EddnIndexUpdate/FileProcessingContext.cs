using System.IO.Abstractions;
using Models = EddnIndex.Common.Models;

namespace EddnIndexUpdate;

public class FileProcessingContext(
        string filepath,
        long filelen,
        Models.FileInfo file,
        Dictionary<int, Dictionary<int, Models.FileLineDataError>> errors,
        IPath path
    )
{
    public string FilePath { get; } = filepath;
    public long FileLength { get; } = filelen;
    public string IndexedFilename { get; }
        = path.Join(
            file.FileName.ContainsAny([path.DirectorySeparatorChar, path.AltDirectorySeparatorChar])
                ? null
                : file.Date?.ToString("yyyy-MM"),
            file.FileName
        );
    public Models.FileInfo File { get; } = file;
    public Dictionary<int, Models.FileLineInfo> NewLines { get; } = [];
    public Dictionary<(int LineNo, int EntryNum), Models.FileLineBody> NewBodyLines { get; } = [];
    public Dictionary<int, Models.FileLineStation> NewStationLines { get; } = [];
    public Dictionary<(int LineNo, int EntryNum), Models.FileLineNavRoute> NewNavRouteEntries { get; } = [];
    public Dictionary<int, Models.FileLineSignal> NewSignalEntries { get; } = [];
    public Dictionary<(int LineNo, int EntryNum), Models.FileLineBodySignal> NewBodySignalEntries { get; } = [];
    public Dictionary<(int LineNo, int EntryNum), Models.FileLineDataError> NewDataErrors { get; } = [];
    public Dictionary<int, Dictionary<int, Models.FileLineDataError>> DataErrors { get; } = errors;
    public int LineCount { get; set; } = 0;
    public int SystemLineCount { get; set; } = 0;
    public int StationLineCount { get; set; } = 0;
    public int NavRouteSystemCount { get; set; } = 0;
    public int BodyLineCount { get; set; } = 0;
    public int SignalCount { get; set; } = 0;
    public int BodySignalCount { get; set; } = 0;
    public int ErrorCount { get; set; } = 0;
}
