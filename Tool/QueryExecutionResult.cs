using System;
using System.Collections.Generic;
using SqlServerStatisticsParser;

namespace SqlServerStatisticsParser.DotNet.Cli;

/// <summary>
/// Result of executing a query with statistics
/// </summary>
public class QueryExecutionResult
{
    public DatabaseConfiguration Configuration { get; set; } = new DatabaseConfiguration();
    public TimeSpan ExecutionTime { get; set; }
    public string RawStatisticsOutput { get; set; } = string.Empty;
    public ParsedStatistics ParsedStatistics { get; set; } = new ParsedStatistics();
    public Dictionary<string, long> SqlConnectionStatistics { get; set; } = new Dictionary<string, long>();
    public Exception? Error { get; set; }
    public bool Success => Error == null;
}
