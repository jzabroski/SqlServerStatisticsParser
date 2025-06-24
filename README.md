# SqlServerStatisticsParser

## 🚀 Key Features:
### Smart SQL Execution

* **Live Statistics Collection:** Uses SqlConnection.RetrieveStatistics() to gather real execution metrics
* **InfoMessage Capture:** Captures SQL Server's STATISTICS IO and TIME output through event handlers
* **Timeout Management:** 5-minute query timeout for complex operations
* **Error Handling:** Robust exception handling with detailed error reporting

### A/B Testing Framework

* **Multiple Configuration Testing:** Compare different database settings side-by-side
* **Iterative Testing:** Run multiple iterations for statistical accuracy
* **Automatic Averaging:** Calculate average performance across iterations
* **Real-time Progress:** Live feedback during test execution

### Database Configuration Options

* **Compatibility Level Testing:** Test SQL Server 2016-2022 compatibility levels (130-160)
* **MAXDOP Variations:** Compare parallel execution settings
* **Query Hints:** Apply optimizer hints like index forcing
* **Custom Settings:** Flexible system for additional SET options

### Three Execution Modes

1. **Single Mode:** Basic query analysis with statistics
    ```bash
    StatisticsParser.exe "Server=.;Database=MyDB;Integrated Security=true" "SELECT * FROM Users" single
    ```

2. **A/B Test Mode:** Compare different configurations
    ```bash
    StatisticsParser.exe "Server=.;Database=MyDB;Integrated Security=true" "SELECT * FROM Users" abtest
    ```

3. **Compatibility Test Mode:** Test across SQL Server versions
    ```bash
    StatisticsParser.exe "Server=.;Database=MyDB;Integrated Security=true" "SELECT * FROM Users" compatibility
    ```

### Rich Output Format

* **Execution Timing:** Precise millisecond timing
* **Parsed Statistics:** Clean, formatted IO statistics
* **Connection Metrics:** Low-level SqlConnection statistics
* **Comparative Tables:** Side-by-side performance comparison
* **JSON Export:** Machine-readable output option

## Example A/B Test Output

```
=== A/B TEST RESULTS ===
Configuration        Avg Time (ms)   Logical Reads   Physical Reads
================================================================================
Default              1,250.45        15,000          150
MAXDOP 1            2,100.23        15,000          150
MAXDOP 4            890.67          15,000          150
Force Index Scan    3,450.12        45,000          2,100
```

This creates a powerful tool for database performance analysis, allowing developers and DBAs to scientifically compare query performance under different conditions, identify optimal configurations, and understand the impact of various SQL Server settings on query execution.

The tool leverages both raw statistics parsing capabilities and native .NET SQL Server integration for comprehensive performance analysis.
