using Connectors.Data.MSSQL;
using Connectors.Storage.AzureBlob;
using Core;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddScoped<IDataSource, MsSqlDataService>();
builder.Services.AddScoped<IStorage, AzureBlobStorageService>();
builder.Services.AddScoped<YtDlpApplication>();

builder.Logging.AddConsole();



using var host = builder.Build();
using var scope = host.Services.CreateScope();
var app = scope.ServiceProvider.GetRequiredService<YtDlpApplication>();
await app!.RunAsync();