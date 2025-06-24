using System;
using System.Collections.Generic;

namespace SqlServerStatisticsParser
{
    /// <summary>
    /// Represents the parsed statistics output
    /// </summary>
    public class ParsedStatistics
    {
        public List<StatisticsIOData> IOStatistics { get; set; } = new List<StatisticsIOData>();
        public StatisticsTimeData? TimeStatistics { get; set; }
        public IOTotals Totals { get; set; } = new IOTotals();
    }
}
