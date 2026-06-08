using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Audiola.Web;
using MudBlazor.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Der Client wird von der API gehostet → gleicher Origin (BaseAddress = Host).
// Großzügiges Timeout, weil die Stem-Trennung (Demucs) mehrere Minuten dauern kann.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromMinutes(15)
});

// MudBlazor + MudBlazor.Extensions (inkl. MudExAudioPlayer / AuralizeBlazor).
builder.Services.AddMudServicesWithExtensions();

// Gemeinsamer App-Zustand (geöffnete Datei über Seiten hinweg).
builder.Services.AddSingleton<Audiola.Web.Services.AppState>();

await builder.Build().RunAsync();
