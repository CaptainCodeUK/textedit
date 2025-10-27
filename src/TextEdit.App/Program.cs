using ElectronNET.API;
using TextEdit.App;

var builder = WebApplication.CreateBuilder(args);

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
        ElectronHost.Initialize(app);
    });
}

await app.RunAsync();
