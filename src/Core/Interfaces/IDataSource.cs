using Core.Models;

namespace Core.Interfaces;

public interface IDataSource
{
    public Task DataSourceSetup();
    public Task<DownloadRequest?> GetNextTask();
    public Task LogTaskAsStarted(Guid downloadId);
    public Task LogTaskAsCompletedSuccessfully(Guid downloadId);
    public Task LogTaskAsCompletedWithError(Guid downloadId, string errorMessage);
}