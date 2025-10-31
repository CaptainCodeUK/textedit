using ElectronNET.API;
using TextEdit.App;
using Serilog;
using Serilog.Events;

// Determine OS-specific log directory
var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var logDir = Path.Combine(appDataDir, "Scrappy", "Logs");
Directory.CreateDirectory(logDir);

// Configure Serilog with rolling file sink
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: Path.Combine(logDir, "textedit-.log"),
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10_485_760, // 10MB
        retainedFileCountLimit: 5,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Capture CLI args for passing to Electron (T041)
var cliArgs = args;

// Add Electron support
builder.WebHost.UseElectron(args);

// Configure services via Startup
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure middleware
startup.Configure(app, app.Environment);

// Bootstrap Electron window after the web host is fully started
if (HybridSupport.IsElectronActive)
{
    // Initialize Electron after ASP.NET starts listening
    _ = Task.Run(async () =>
    {
        // Give ASP.NET Core a moment to start listening
        await Task.Delay(1000);
        ElectronHost.Initialize(app, cliArgs);
    });
}

try
{
    Log.Information("Starting Scrappy Text Editor");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
