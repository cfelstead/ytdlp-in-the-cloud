using System.Text;
using CliWrap;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core;

public sealed class YtDlpApplication(
    IDataSource data,
    IStorage storage,
    ILogger<YtDlpApplication> logger,
    IConfiguration configuration)
{
    public async Task RunAsync()
    {
        var noWorkPauseTime = GetNoWorkPauseTime();
        await data.DataSourceSetup();
        DateTime? lastUpdatedBinary = null; // used to know when the binary was last attempted to be updated. Attempts happen daily and on restart
        
        
        await UpdateYtdlp(lastUpdatedBinary);
        
        
        logger.LogInformation("Starting YtDlpWrapperApp ...");
        string[] initialFilesInApp = Directory.GetFiles(Directory.GetCurrentDirectory()); // user to store the initial file system so that we can detect what videos have been downloaded

        
        while (true)
        {
            var nextTask = await data.GetNextTask();
            
            if (nextTask is not null)
            {
                await ProcessDownload(nextTask, initialFilesInApp);
            }
            else
            {
                await UpdateYtdlp(lastUpdatedBinary);
                logger.LogInformation("Pausing for {time} as there is no work.", noWorkPauseTime);
                await Task.Delay(noWorkPauseTime);
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private TimeSpan GetNoWorkPauseTime()
    {
        var noWorkPauseTimeInMinutes = configuration.GetValue<int>("YtDlpApplication:NoWorkPauseTimeInMinutes");
        if (noWorkPauseTimeInMinutes == default)
        {
            noWorkPauseTimeInMinutes = 5;
        }
        logger.LogInformation("No work pause time set to {time} minutes", noWorkPauseTimeInMinutes);
        return TimeSpan.FromMinutes(noWorkPauseTimeInMinutes);
    }

    
    
    private async Task UpdateYtdlp(DateTime? lastUpdatedBinary)
    {
        if (lastUpdatedBinary is null
            || lastUpdatedBinary.Value.Date < DateTime.UtcNow.Date)
        {
            logger.LogInformation("Updating yt-dlp binary");

            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            await Cli.Wrap("yt-dlp")
                .WithArguments(["-U"])
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            if (!string.IsNullOrWhiteSpace(stdOutBuffer.ToString()))
            {
                logger.LogInformation("{output}", stdOutBuffer.ToString());
            }

            if (!string.IsNullOrWhiteSpace(stdErrBuffer.ToString()))
            {
                logger.LogInformation("{output}", stdErrBuffer.ToString());   
            }
            
            lastUpdatedBinary = DateTime.Now;
        }
    }

    private async Task ProcessDownload(DownloadRequest task, string[] initialFilesInApp)
    {
        await data.LogTaskAsStarted(task.DownloadId);



        try
        {
            logger.LogInformation("Booting the CLI for {url}", task.Url);

            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();


            var result = await Cli.Wrap("yt-dlp")
                .WithArguments([
                    "-f", "bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4] / bv*+ba/b", "--ffmpeg-location", "/app", task.Url
                ])
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            // var stdOut = stdOutBuffer.ToString();
            // var stdErr = stdErrBuffer.ToString();
            // Console.WriteLine(stdOut);
            // Console.WriteLine(stdErr);

            if (result.IsSuccess)
            {
                await data.LogTaskAsCompletedSuccessfully(task.DownloadId);
                await HandleVideoFiles(initialFilesInApp);
            }
            else
            {
                await data.LogTaskAsCompletedWithError(task.DownloadId, "YT-DLP returned with a failed result but no exception.");    
            }
        }
        catch (Exception ex)
        {
            await data.LogTaskAsCompletedWithError(task.DownloadId, ex.Message);
        }
    }

    private async Task HandleVideoFiles(string[] initialFilesInApp)
    {
        var filesInApp = Directory.GetFiles(Directory.GetCurrentDirectory());
        var newFiles = filesInApp.Except(initialFilesInApp).ToArray();
        foreach (var file in newFiles)
        {
            // Transfer file to long term storage and delete from the application path
            logger.LogInformation("Working on {file}", file);
            MemoryStream ms = new(await File.ReadAllBytesAsync(file));
            try
            {
                await storage.SaveFileToStorage(file, ms);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error transferring file {file} to long term storage", file);
            }
            finally
            {
                File.Delete(file);
                logger.LogInformation("Deleted {file}", file);
            }
        }
    }
}