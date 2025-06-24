using System;

namespace SqlServerStatisticsParser
{
    /// <summary>
    /// Represents SQL Server Statistics IO data for a table
    /// </summary>
    public class StatisticsIOData
    {
        public string TableName { get; set; } = string.Empty;
        public int ScanCount { get; set; }
        public int LogicalReads { get; set; }
        public int PhysicalReads { get; set; }
        public int PageServerReads { get; set; }
        public int ReadAheadReads { get; set; }
        public int PageServerReadAheadReads { get; set; }
        public int LobLogicalReads { get; set; }
        public int LobPhysicalReads { get; set; }
        public int LobPageServerReads { get; set; }
        public int LobReadAheadReads { get; set; }
        public int LobPageServerReadAheadReads { get; set; }
    }
}
