using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SqlServerStatisticsParser;

namespace SqlServerStatisticsParser.DotNet.Cli
{
    /// <summary>
    /// Example usage and testing
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Example usage
            string sampleInput = @"
Table 'Orders'. Scan count 1, logical reads 3, physical reads 0, read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0.
Table 'OrderDetails'. Scan count 5, logical reads 150, physical reads 10, read-ahead reads 5, lob logical reads 2, lob physical reads 0, lob read-ahead reads 0.

SQL Server parse and compile time: 
   CPU time = 15 ms, elapsed time = 18 ms.

SQL Server Execution Times:
   CPU time = 125 ms, elapsed time = 1205 ms.
";

            try
            {
                var parsedStats = StatisticsParser.Parse(sampleInput);
                
                Console.WriteLine("=== PARSED STATISTICS ===");
                Console.WriteLine(StatisticsParser.FormatStatistics(parsedStats));
                
                Console.WriteLine("=== JSON OUTPUT ===");
                Console.WriteLine(StatisticsParser.ToJson(parsedStats));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing statistics: {ex.Message}");
            }
        }
    }
}
