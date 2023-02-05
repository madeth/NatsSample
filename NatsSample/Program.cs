using AlterNats;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvcCore();
builder.Host.UseSerilog();

var poolSize = 1;
builder.Services.AddNats(
    poolSize: poolSize,
    configureOptions: (options) => options with
    {
        LoggerFactory = new MinimumConsoleLoggerFactory(LogLevel.Debug),
        Url = "nats1:4222",
        ConnectTimeout  = TimeSpan.FromSeconds(30),
        RequestTimeout  = TimeSpan.FromSeconds(10),
        ReconnectWait   = TimeSpan.FromSeconds(5),
        ConnectOptions  = ConnectOptions.Default with { Verbose = true, Echo = true }
    },
    configureConnection: (connection) =>
    {
        connection.ReconnectFailed += (sender, e) => {
            var date = DateTime.Now;
            Log.Logger.Error($"ReconnectFailed: {e}");
        };
    }
);

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRouting();
app.MapDefaultControllerRoute();
app.Run();