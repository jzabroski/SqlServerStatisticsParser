using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SqlServerStatisticsParser
{
    /// <summary>
    /// Main parser class for SQL Server Statistics IO and Time output
    /// </summary>
    public class StatisticsParser
    {
        // Regex patterns for parsing Statistics IO output
        private static readonly Regex IoPattern = new Regex(
            @"Table\s+'([^']+)'[.\s]*Scan count\s+(\d+),\s*logical reads\s+(\d+),\s*physical reads\s+(\d+)(?:,\s*page server reads\s+(\d+))?(?:,\s*read-ahead reads\s+(\d+))?(?:,\s*page server read-ahead reads\s+(\d+))?(?:,\s*lob logical reads\s+(\d+))?(?:,\s*lob physical reads\s+(\d+))?(?:,\s*lob page server reads\s+(\d+))?(?:,\s*lob read-ahead reads\s+(\d+))?(?:,\s*lob page server read-ahead reads\s+(\d+))?",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Alternative pattern for different formats
        private static readonly Regex IoPatternAlt = new Regex(
            @"Table\s+""([^""]+)""[.\s]*Scan count\s+(\d+),\s*logical reads\s+(\d+),\s*physical reads\s+(\d+)(?:,\s*page server reads\s+(\d+))?(?:,\s*read-ahead reads\s+(\d+))?(?:,\s*page server read-ahead reads\s+(\d+))?(?:,\s*lob logical reads\s+(\d+))?(?:,\s*lob physical reads\s+(\d+))?(?:,\s*lob page server reads\s+(\d+))?(?:,\s*lob read-ahead reads\s+(\d+))?(?:,\s*lob page server read-ahead reads\s+(\d+))?",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Regex pattern for parsing Statistics Time output
        private static readonly Regex TimePattern = new Regex(
            @"SQL Server parse and compile time:\s*CPU time = (\d+) ms,\s*elapsed time = (\d+) ms\.|SQL Server Execution Times:\s*CPU time = (\d+) ms,\s*elapsed time = (\d+) ms\.",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        /// <summary>
        /// Parses SQL Server Statistics IO and Time output
        /// </summary>
        /// <param name="input">The raw statistics output from SQL Server</param>
        /// <returns>Parsed statistics data</returns>
        public static ParsedStatistics Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            var result = new ParsedStatistics();

            // Parse IO Statistics
            result.IOStatistics = ParseIOStatistics(input);

            // Parse Time Statistics
            result.TimeStatistics = ParseTimeStatistics(input);

            // Calculate totals
            result.Totals = CalculateTotals(result.IOStatistics);

            return result;
        }

        /// <summary>
        /// Parses IO statistics from the input
        /// </summary>
        private static List<StatisticsIOData> ParseIOStatistics(string input)
        {
            var ioData = new List<StatisticsIOData>();

            // Try primary pattern first
            var matches = IoPattern.Matches(input);
            if (matches.Count == 0)
            {
                // Try alternative pattern
                matches = IoPatternAlt.Matches(input);
            }

            foreach (Match match in matches)
            {
                var data = new StatisticsIOData
                {
                    TableName = match.Groups[1].Value,
                    ScanCount = int.Parse(match.Groups[2].Value),
                    LogicalReads = int.Parse(match.Groups[3].Value),
                    PhysicalReads = int.Parse(match.Groups[4].Value),
                    PageServerReads = ParseOptionalInt(match.Groups[5]),
                    ReadAheadReads = ParseOptionalInt(match.Groups[6]),
                    PageServerReadAheadReads = ParseOptionalInt(match.Groups[7]),
                    LobLogicalReads = ParseOptionalInt(match.Groups[8]),
                    LobPhysicalReads = ParseOptionalInt(match.Groups[9]),
                    LobPageServerReads = ParseOptionalInt(match.Groups[10]),
                    LobReadAheadReads = ParseOptionalInt(match.Groups[11]),
                    LobPageServerReadAheadReads = ParseOptionalInt(match.Groups[12])
                };

                ioData.Add(data);
            }

            return ioData;
        }

        /// <summary>
        /// Parses time statistics from the input
        /// </summary>
        private static StatisticsTimeData? ParseTimeStatistics(string input)
        {
            var matches = TimePattern.Matches(input);
            
            if (matches.Count == 0)
                return null;

            double totalCpuTime = 0;
            double totalElapsedTime = 0;

            foreach (Match match in matches)
            {
                // Check which groups have values (parse time vs execution time)
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    totalCpuTime += double.Parse(match.Groups[1].Value);
                    totalElapsedTime += double.Parse(match.Groups[2].Value);
                }
                else if (!string.IsNullOrEmpty(match.Groups[3].Value))
                {
                    totalCpuTime += double.Parse(match.Groups[3].Value);
                    totalElapsedTime += double.Parse(match.Groups[4].Value);
                }
            }

            return new StatisticsTimeData
            {
                CpuTime = totalCpuTime,
                ElapsedTime = totalElapsedTime
            };
        }

        /// <summary>
        /// Helper method to parse optional integer values from regex groups
        /// </summary>
        private static int ParseOptionalInt(Group group)
        {
            return group.Success && int.TryParse(group.Value, out int value) ? value : 0;
        }

        /// <summary>
        /// Calculates totals for IO statistics
        /// </summary>
        private static IOTotals CalculateTotals(List<StatisticsIOData> ioData)
        {
            return new IOTotals
            {
                TotalScanCount = ioData.Sum(x => x.ScanCount),
                TotalLogicalReads = ioData.Sum(x => x.LogicalReads),
                TotalPhysicalReads = ioData.Sum(x => x.PhysicalReads),
                TotalPageServerReads = ioData.Sum(x => x.PageServerReads),
                TotalReadAheadReads = ioData.Sum(x => x.ReadAheadReads),
                TotalPageServerReadAheadReads = ioData.Sum(x => x.PageServerReadAheadReads),
                TotalLobLogicalReads = ioData.Sum(x => x.LobLogicalReads),
                TotalLobPhysicalReads = ioData.Sum(x => x.LobPhysicalReads),
                TotalLobPageServerReads = ioData.Sum(x => x.LobPageServerReads),
                TotalLobReadAheadReads = ioData.Sum(x => x.LobReadAheadReads),
                TotalLobPageServerReadAheadReads = ioData.Sum(x => x.LobPageServerReadAheadReads)
            };
        }

        /// <summary>
        /// Formats the parsed statistics as a human-readable string
        /// </summary>
        /// <param name="stats">Parsed statistics</param>
        /// <returns>Formatted string representation</returns>
        public static string FormatStatistics(ParsedStatistics stats)
        {
            var result = new System.Text.StringBuilder();

            if (stats.IOStatistics.Any())
            {
                result.AppendLine("=== STATISTICS IO ===");
                result.AppendLine();

                foreach (var io in stats.IOStatistics)
                {
                    result.AppendLine($"Table: {io.TableName}");
                    result.AppendLine($"  Scan count: {io.ScanCount:N0}");
                    result.AppendLine($"  Logical reads: {io.LogicalReads:N0}");
                    result.AppendLine($"  Physical reads: {io.PhysicalReads:N0}");
                    
                    if (io.PageServerReads > 0)
                        result.AppendLine($"  Page server reads: {io.PageServerReads:N0}");
                    if (io.ReadAheadReads > 0)
                        result.AppendLine($"  Read-ahead reads: {io.ReadAheadReads:N0}");
                    if (io.PageServerReadAheadReads > 0)
                        result.AppendLine($"  Page server read-ahead reads: {io.PageServerReadAheadReads:N0}");
                    if (io.LobLogicalReads > 0)
                        result.AppendLine($"  LOB logical reads: {io.LobLogicalReads:N0}");
                    if (io.LobPhysicalReads > 0)
                        result.AppendLine($"  LOB physical reads: {io.LobPhysicalReads:N0}");
                    if (io.LobPageServerReads > 0)
                        result.AppendLine($"  LOB page server reads: {io.LobPageServerReads:N0}");
                    if (io.LobReadAheadReads > 0)
                        result.AppendLine($"  LOB read-ahead reads: {io.LobReadAheadReads:N0}");
                    if (io.LobPageServerReadAheadReads > 0)
                        result.AppendLine($"  LOB page server read-ahead reads: {io.LobPageServerReadAheadReads:N0}");
                    
                    result.AppendLine();
                }

                // Display totals if more than one table
                if (stats.IOStatistics.Count > 1)
                {
                    result.AppendLine("=== TOTALS ===");
                    result.AppendLine($"Total scan count: {stats.Totals.TotalScanCount:N0}");
                    result.AppendLine($"Total logical reads: {stats.Totals.TotalLogicalReads:N0}");
                    result.AppendLine($"Total physical reads: {stats.Totals.TotalPhysicalReads:N0}");
                    
                    if (stats.Totals.TotalPageServerReads > 0)
                        result.AppendLine($"Total page server reads: {stats.Totals.TotalPageServerReads:N0}");
                    if (stats.Totals.TotalReadAheadReads > 0)
                        result.AppendLine($"Total read-ahead reads: {stats.Totals.TotalReadAheadReads:N0}");
                    if (stats.Totals.TotalPageServerReadAheadReads > 0)
                        result.AppendLine($"Total page server read-ahead reads: {stats.Totals.TotalPageServerReadAheadReads:N0}");
                    if (stats.Totals.TotalLobLogicalReads > 0)
                        result.AppendLine($"Total LOB logical reads: {stats.Totals.TotalLobLogicalReads:N0}");
                    if (stats.Totals.TotalLobPhysicalReads > 0)
                        result.AppendLine($"Total LOB physical reads: {stats.Totals.TotalLobPhysicalReads:N0}");
                    if (stats.Totals.TotalLobPageServerReads > 0)
                        result.AppendLine($"Total LOB page server reads: {stats.Totals.TotalLobPageServerReads:N0}");
                    if (stats.Totals.TotalLobReadAheadReads > 0)
                        result.AppendLine($"Total LOB read-ahead reads: {stats.Totals.TotalLobReadAheadReads:N0}");
                    if (stats.Totals.TotalLobPageServerReadAheadReads > 0)
                        result.AppendLine($"Total LOB page server read-ahead reads: {stats.Totals.TotalLobPageServerReadAheadReads:N0}");
                    
                    result.AppendLine();
                }
            }

            if (stats.TimeStatistics != null)
            {
                result.AppendLine("=== STATISTICS TIME ===");
                result.AppendLine($"CPU time: {stats.TimeStatistics.CpuTime:N0} ms");
                result.AppendLine($"Elapsed time: {stats.TimeStatistics.ElapsedTime:N0} ms");
            }

            return result.ToString();
        }

        /// <summary>
        /// Exports the parsed statistics to JSON
        /// </summary>
        /// <param name="stats">Parsed statistics</param>
        /// <returns>JSON representation</returns>
        public static string ToJson(ParsedStatistics stats)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Serialize(stats, options);
        }
    }
}
