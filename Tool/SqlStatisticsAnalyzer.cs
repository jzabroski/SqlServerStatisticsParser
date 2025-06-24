using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SqlServerStatisticsParser.DotNet.Cli;

/// <summary>
/// Smart SQL statistics analyzer and A/B testing framework
/// </summary>
public class SqlStatisticsAnalyzer
{
    private readonly string _connectionString;

    public SqlStatisticsAnalyzer(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Executes a query and retrieves statistics
    /// </summary>
    public async Task<QueryExecutionResult> ExecuteQueryWithStatisticsAsync(string query, DatabaseConfiguration config)
    {
        var result = new QueryExecutionResult { Configuration = config };
        var statisticsOutput = new StringBuilder();

        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Apply database configuration
            await ApplyDatabaseConfigurationAsync(connection, config);

            // Enable statistics collection
            connection.StatisticsEnabled = true;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Create command with query hints if specified
            var finalQuery = config.QueryHint != null ? $"{query} {config.QueryHint}" : query;
            using var command = new Microsoft.Data.SqlClient.SqlCommand(finalQuery, connection);
            command.CommandTimeout = 300; // 5 minutes

            // Execute query and capture output
            using var reader = await command.ExecuteReaderAsync();
            
            // Consume all result sets
            do
            {
                while (await reader.ReadAsync())
                {
                    // Just consume the results
                }
            } while (await reader.NextResultAsync());

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            // Retrieve connection statistics
            var stats = connection.RetrieveStatistics();
            result.SqlConnectionStatistics = stats.Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kvp => kvp.Key.ToString()!, kvp => Convert.ToInt64(kvp.Value));

            // Get statistics output using InfoMessage events
            var statisticsMessages = await GetStatisticsMessagesAsync(connection, finalQuery);
            result.RawStatisticsOutput = statisticsMessages;

            // Parse the statistics
            if (!string.IsNullOrWhiteSpace(result.RawStatisticsOutput))
            {
                result.ParsedStatistics = StatisticsParser.Parse(result.RawStatisticsOutput);
            }
        }
        catch (Exception ex)
        {
            result.Error = ex;
        }

        return result;
    }

    /// <summary>
    /// Performs A/B testing with multiple database configurations
    /// </summary>
    public async Task<List<QueryExecutionResult>> PerformABTestAsync(string query, List<DatabaseConfiguration> configurations, int iterations = 1)
    {
        var results = new List<QueryExecutionResult>();

        foreach (var config in configurations)
        {
            Console.WriteLine($"Testing configuration: {config.Name}");
            
            var configResults = new List<QueryExecutionResult>();
            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine($"  Iteration {i + 1}/{iterations}");
                var result = await ExecuteQueryWithStatisticsAsync(query, config);
                configResults.Add(result);
                
                if (!result.Success)
                {
                    Console.WriteLine($"  Error: {result.Error?.Message}");
                }
                
                // Small delay between iterations
                if (i < iterations - 1)
                    await Task.Delay(1000);
            }

            // Add average result for multiple iterations
            if (configResults.Count > 1)
            {
                var avgResult = CalculateAverageResult(configResults, config);
                results.Add(avgResult);
            }
            else
            {
                results.AddRange(configResults);
            }
        }

        return results;
    }

    /// <summary>
    /// Applies database configuration settings
    /// </summary>
    private async Task ApplyDatabaseConfigurationAsync(Microsoft.Data.SqlClient.SqlConnection connection, DatabaseConfiguration config)
    {
        var commands = new List<string>();

        // Set compatibility level
        if (config.CompatibilityLevel.HasValue)
        {
            commands.Add($"ALTER DATABASE [{connection.Database}] SET COMPATIBILITY_LEVEL = {config.CompatibilityLevel.Value}");
        }

        // Set MAXDOP
        if (config.MaxDOP.HasValue)
        {
            commands.Add($"ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = {config.MaxDOP.Value}");
        }

        // Enable/disable statistics
        if (config.StatisticsIO.HasValue)
        {
            commands.Add($"SET STATISTICS IO {(config.StatisticsIO.Value ? "ON" : "OFF")}");
        }

        if (config.StatisticsTime.HasValue)
        {
            commands.Add($"SET STATISTICS TIME {(config.StatisticsTime.Value ? "ON" : "OFF")}");
        }

        // Apply custom settings
        foreach (var setting in config.CustomSettings)
        {
            commands.Add($"SET {setting.Key} {setting.Value}");
        }

        // Execute all configuration commands
        foreach (var cmd in commands)
        {
            try
            {
                using var command = new Microsoft.Data.SqlClient.SqlCommand(cmd, connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to execute configuration command '{cmd}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets statistics messages by re-executing with message capture
    /// </summary>
    private async Task<string> GetStatisticsMessagesAsync(Microsoft.Data.SqlClient.SqlConnection connection, string query)
    {
        var messages = new StringBuilder();
        var tempConnection = new Microsoft.Data.SqlClient.SqlConnection(connection.ConnectionString);
        
        try
        {
            await tempConnection.OpenAsync();
            
            tempConnection.InfoMessage += (sender, args) =>
            {
                messages.AppendLine(args.Message);
            };

            // Enable statistics
            using var statsCmd = new Microsoft.Data.SqlClient.SqlCommand("SET STATISTICS IO ON; SET STATISTICS TIME ON;", tempConnection);
            await statsCmd.ExecuteNonQueryAsync();

            // Execute the query again to capture messages
            using var queryCmd = new Microsoft.Data.SqlClient.SqlCommand(query, tempConnection);
            queryCmd.CommandTimeout = 300;
            
            using var reader = await queryCmd.ExecuteReaderAsync();
            do
            {
                while (await reader.ReadAsync()) { }
            } while (await reader.NextResultAsync());
        }
        finally
        {
            if (tempConnection.State == System.Data.ConnectionState.Open)
            {
                tempConnection.Close();
            }
            tempConnection.Dispose();
        }

        return messages.ToString();
    }

    /// <summary>
    /// Calculates average result from multiple iterations
    /// </summary>
    private QueryExecutionResult CalculateAverageResult(List<QueryExecutionResult> results, DatabaseConfiguration config)
    {
        var successfulResults = results.Where(r => r.Success).ToList();
        if (!successfulResults.Any())
            return results.First();

        var avgResult = new QueryExecutionResult
        {
            Configuration = config,
            ExecutionTime = TimeSpan.FromMilliseconds(successfulResults.Average(r => r.ExecutionTime.TotalMilliseconds)),
            RawStatisticsOutput = successfulResults.First().RawStatisticsOutput,
            ParsedStatistics = successfulResults.First().ParsedStatistics
        };

        // Average SQL connection statistics
        var allStats = successfulResults.SelectMany(r => r.SqlConnectionStatistics).GroupBy(kvp => kvp.Key);
        foreach (var statGroup in allStats)
        {
            avgResult.SqlConnectionStatistics[statGroup.Key] = (long)statGroup.Average(kvp => kvp.Value);
        }

        return avgResult;
    }
}
