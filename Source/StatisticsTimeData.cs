using System;

namespace SqlServerStatisticsParser
{
    /// <summary>
    /// Represents SQL Server Statistics Time data
    /// </summary>
    public class StatisticsTimeData
    {
        public double CpuTime { get; set; }
        public double ElapsedTime { get; set; }
    }
}
