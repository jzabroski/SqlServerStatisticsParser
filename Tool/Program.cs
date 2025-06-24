using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SqlServerStatisticsParser;

namespace SqlServerStatisticsParser.DotNet.Cli
{
    /// <summary>
    /// Program with smart SQL statistics analysis and A/B testing
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SQL Server Statistics Analyzer");
            Console.WriteLine("==============================");

            if (args.Length < 2)
            {
                ShowUsage();
                return;
            }

            var connectionString = args[0];
            var query = args[1];
            var mode = args.Length > 2 ? args[2].ToLower() : "single";

            try
            {
                var analyzer = new SqlStatisticsAnalyzer(connectionString);

                switch (mode)
                {
                    case "single":
                        await RunSingleQueryAnalysis(analyzer, query);
                        break;
                    case "abtest":
                        await RunABTest(analyzer, query);
                        break;
                    case "compatibility":
                        await RunCompatibilityTest(analyzer, query);
                        break;
                    default:
                        Console.WriteLine($"Unknown mode: {mode}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        private static async Task RunSingleQueryAnalysis(SqlStatisticsAnalyzer analyzer, string query)
        {
            Console.WriteLine("Running single query analysis...\n");

            var config = new DatabaseConfiguration
            {
                Name = "Default Configuration",
                StatisticsIO = true,
                StatisticsTime = true
            };

            var result = await analyzer.ExecuteQueryWithStatisticsAsync(query, config);

            if (result.Success)
            {
                Console.WriteLine($"Execution Time: {result.ExecutionTime.TotalMilliseconds:F2} ms\n");
                Console.WriteLine("=== PARSED STATISTICS ===");
                Console.WriteLine(StatisticsParser.FormatStatistics(result.ParsedStatistics));
                
                Console.WriteLine("=== SQL CONNECTION STATISTICS ===");
                foreach (var stat in result.SqlConnectionStatistics.OrderBy(kvp => kvp.Key))
                {
                    Console.WriteLine($"{stat.Key}: {stat.Value:N0}");
                }
            }
            else
            {
                Console.WriteLine($"Query execution failed: {result.Error?.Message}");
            }
        }

        private static async Task RunABTest(SqlStatisticsAnalyzer analyzer, string query)
        {
            Console.WriteLine("Running A/B test with different configurations...\n");

            var configurations = new List<DatabaseConfiguration>
            {
                new DatabaseConfiguration
                {
                    Name = "Default",
                    StatisticsIO = true,
                    StatisticsTime = true
                },
                new DatabaseConfiguration
                {
                    Name = "MAXDOP 1",
                    MaxDOP = 1,
                    StatisticsIO = true,
                    StatisticsTime = true
                },
                new DatabaseConfiguration
                {
                    Name = "MAXDOP 4",
                    MaxDOP = 4,
                    StatisticsIO = true,
                    StatisticsTime = true
                },
                new DatabaseConfiguration
                {
                    Name = "Force Index Scan",
                    QueryHint = "OPTION (TABLE HINT([TableName], INDEX(1)))",
                    StatisticsIO = true,
                    StatisticsTime = true
                }
            };

            var results = await analyzer.PerformABTestAsync(query, configurations, iterations: 3);

            Console.WriteLine("\n=== A/B TEST RESULTS ===");
            Console.WriteLine($"{"Configuration",-20} {"Avg Time (ms)",-15} {"Logical Reads",-15} {"Physical Reads",-15}");
            Console.WriteLine(new string('=', 80));

            foreach (var result in results.Where(r => r.Success))
            {
                var totalLogicalReads = result.ParsedStatistics.Totals.TotalLogicalReads;
                var totalPhysicalReads = result.ParsedStatistics.Totals.TotalPhysicalReads;
                
                Console.WriteLine($"{result.Configuration.Name,-20} {result.ExecutionTime.TotalMilliseconds,-15:F2} {totalLogicalReads,-15:N0} {totalPhysicalReads,-15:N0}");
            }
        }

        private static async Task RunCompatibilityTest(SqlStatisticsAnalyzer analyzer, string query)
        {
            Console.WriteLine("Running compatibility level test...\n");

            var compatibilityLevels = new[] { 130, 140, 150, 160 }; // SQL Server 2016, 2017, 2019, 2022
            var configurations = compatibilityLevels.Select(level => new DatabaseConfiguration
            {
                Name = $"Compatibility {level}",
                CompatibilityLevel = level,
                StatisticsIO = true,
                StatisticsTime = true
            }).ToList();

            var results = await analyzer.PerformABTestAsync(query, configurations, iterations: 2);

            Console.WriteLine("\n=== COMPATIBILITY LEVEL TEST RESULTS ===");
            Console.WriteLine($"{"Compatibility Level",-20} {"Avg Time (ms)",-15} {"Logical Reads",-15} {"Physical Reads",-15}");
            Console.WriteLine(new string('=', 80));

            foreach (var result in results.Where(r => r.Success))
            {
                var totalLogicalReads = result.ParsedStatistics.Totals.TotalLogicalReads;
                var totalPhysicalReads = result.ParsedStatistics.Totals.TotalPhysicalReads;
                
                Console.WriteLine($"{result.Configuration.Name,-20} {result.ExecutionTime.TotalMilliseconds,-15:F2} {totalLogicalReads,-15:N0} {totalPhysicalReads,-15:N0}");
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  StatisticsParser.exe <connectionString> <query> [mode]");
            Console.WriteLine();
            Console.WriteLine("Modes:");
            Console.WriteLine("  single        - Run query once with statistics (default)");
            Console.WriteLine("  abtest        - Run A/B test with different configurations");
            Console.WriteLine("  compatibility - Test different database compatibility levels");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  StatisticsParser.exe \"Server=.;Database=MyDB;Integrated Security=true\" \"SELECT * FROM Users\" single");
            Console.WriteLine("  StatisticsParser.exe \"Server=.;Database=MyDB;Integrated Security=true\" \"SELECT * FROM Users\" abtest");
            Console.WriteLine("  StatisticsParser.exe \"Server=.;Database=MyDB;Integrated Security=true\" \"SELECT * FROM Users\" compatibility");
        }
    }
}
