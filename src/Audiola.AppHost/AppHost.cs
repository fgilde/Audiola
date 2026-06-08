var builder = DistributedApplication.CreateBuilder(args);

// Audiola.Api hostet zugleich den Blazor-WASM-Client (gleicher Origin).
// Standard-Endpoint aus der launchSettings (http:5099) — VS/Aspire verwalten den Port selbst.
builder.AddProject("audiola", "../Audiola.Api/Audiola.Api.csproj")
    .WithExternalHttpEndpoints();

builder.Build().Run();
