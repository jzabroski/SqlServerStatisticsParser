using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SqlServerStatisticsParser
{
    /// <summary>
    /// Represents totals for IO statistics
    /// </summary>
    public class IOTotals
    {
        public int TotalScanCount { get; set; }
        public int TotalLogicalReads { get; set; }
        public int TotalPhysicalReads { get; set; }
        public int TotalPageServerReads { get; set; }
        public int TotalReadAheadReads { get; set; }
        public int TotalPageServerReadAheadReads { get; set; }
        public int TotalLobLogicalReads { get; set; }
        public int TotalLobPhysicalReads { get; set; }
        public int TotalLobPageServerReads { get; set; }
        public int TotalLobReadAheadReads { get; set; }
        public int TotalLobPageServerReadAheadReads { get; set; }
    }
}
