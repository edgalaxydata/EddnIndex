using System;
using System.Collections.Generic;
using System.Text;

namespace EddnIndex.Common.Models
{
    public class FileLineDataError
    {
        public int FileId { get; init; }
        public int LineNo { get; init; }
        public int ErrorIndex { get; init; }
        public required string ErrorMessage { get; init; }
    }
}
