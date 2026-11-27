//using BIOHub.AnalysisWorker;
//using BIOHub.Data;
//using Microsoft.EntityFrameworkCore;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();


using BIOHub.AnalysisWorker;
using BIOHub.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile(
    "worker.settings.json",
    optional: false,
    reloadOnChange: true);

// 讓 Docker 環境變數能覆蓋 worker.settings.json
builder.Configuration.AddEnvironmentVariables();
string connectionString =
    builder.Configuration.GetConnectionString("BIOHubConnection")
    ?? throw new InvalidOperationException(
        "找不到 BIOHubConnection 資料庫連線設定");

builder.Services.AddDbContext<BIOHubDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
