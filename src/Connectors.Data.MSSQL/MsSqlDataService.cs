using System.Reflection;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DbUp;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Connectors.Data.MSSQL;

public class MsSqlDataService : IDataSource
{
    private readonly ILogger<MsSqlDataService> _logger;
    private readonly string _connectionString;
    private const string YTDLP_REQUESTS_TABLE = "YtDlp_DownloadRequests";

    public MsSqlDataService(IConfiguration configuration,
        ILogger<MsSqlDataService> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("YtDlpDatabase")
            ?? throw new NullReferenceException("YtDlpDatabase");
    }

    
    
    public Task DataSourceSetup()
    {
        EnsureDatabase.For.SqlDatabase(_connectionString);
        
        var upgrader =
            DeployChanges.To
                .SqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            _logger.LogError(result.Error, "An error occurred while migrating the database.");
            return Task.CompletedTask;
        }
        
        _logger.LogInformation("Database migration completed.");
        return Task.CompletedTask;
    }

    
    
    public async Task<DownloadRequest?> GetNextTask()
    {
        await using var connection = new SqlConnection(_connectionString);
        string query = $"""
                        SELECT TOP 1 *
                        FROM {YTDLP_REQUESTS_TABLE}
                        WHERE DownloadStarted IS NULL
                        ORDER BY DownloadRequested
                        """;
        var task = await connection.QueryFirstOrDefaultAsync<DownloadRequest>(query);
        return task;
    }

    public async Task LogTaskAsStarted(Guid downloadId)
    {
        await using var connection = new SqlConnection(_connectionString);
        string query = $"""
                        UPDATE {YTDLP_REQUESTS_TABLE}
                        SET DownloadStarted = GETUTCDATE()
                        WHERE DownloadId = @downloadId
                        """;
        await connection.QueryFirstOrDefaultAsync<DownloadRequest>(query, new { downloadId });
    }

    public async Task LogTaskAsCompletedSuccessfully(Guid downloadId)
    {
        await using var connection = new SqlConnection(_connectionString);
        string query = $"""
                        UPDATE {YTDLP_REQUESTS_TABLE}
                        SET DownloadCompleted = GETUTCDATE()
                        WHERE DownloadId = @downloadId
                        """;
        await connection.QueryFirstOrDefaultAsync<DownloadRequest>(query, new { downloadId });
    }

    public async Task LogTaskAsCompletedWithError(Guid downloadId, string errorMessage)
    {
        await using var connection = new SqlConnection(_connectionString);
        string query = $"""
                        UPDATE {YTDLP_REQUESTS_TABLE}
                        SET DownloadCompleted = GETUTCDATE(),
                            DownloadError = @errorMessage
                        WHERE DownloadId = @downloadId
                        """;
        await connection.QueryFirstOrDefaultAsync<DownloadRequest>(query, new { downloadId, errorMessage });
    }
}