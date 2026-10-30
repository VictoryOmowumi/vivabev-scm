using VivaBev.Api.Host;
using VivaBev.Catalogue;
using VivaBev.Infrastructure.Persistence;
using VivaBev.SharedKernel.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting services
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddVivaBevPersistence();

// Modules
builder.Services.AddCatalogueModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.Services.SeedCatalogueAsync();
}

app.UseHttpsRedirection();

app.MapCatalogueEndpoints();

app.Run();