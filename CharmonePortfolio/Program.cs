using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CharmonePortfolio;
using CharmonePortfolio.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Default HttpClient for Blazor
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Register SupabaseService as a singleton so auth state persists
// across page navigations within the same session
builder.Services.AddSingleton<SupabaseService>(sp =>
{
    var http = new HttpClient();
    return new SupabaseService(http);
});

await builder.Build().RunAsync();