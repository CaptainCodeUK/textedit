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

// Bootstrap Electron window
if (HybridSupport.IsElectronActive)
{
    await ElectronHost.InitializeAsync(app);
}

await app.RunAsync();
