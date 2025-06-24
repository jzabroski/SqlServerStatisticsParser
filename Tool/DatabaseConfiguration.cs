using System;
using System.Collections.Generic;

namespace SqlServerStatisticsParser.DotNet.Cli;

/// <summary>
/// Configuration for A/B testing different database settings
/// </summary>
public class DatabaseConfiguration
{
    public string Name { get; set; } = string.Empty;
    public int? CompatibilityLevel { get; set; }
    public bool? StatisticsIO { get; set; } = true;
    public bool? StatisticsTime { get; set; } = true;
    public int? MaxDOP { get; set; }
    public string? QueryHint { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
}
