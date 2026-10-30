using VivaBev.Api.Host;
using VivaBev.Catalogue;
using VivaBev.Identity;
using VivaBev.Infrastructure.Persistence;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.Suppliers;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting services
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddVivaBevPersistence();

// Modules
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCatalogueModule(builder.Configuration);
builder.Services.AddSuppliersModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.Services.SeedOrganisationAsync();
    await app.Services.SeedCatalogueAsync();
    await app.Services.SeedSuppliersAsync(); // after Catalogue: it looks up categories
}

app.UseHttpsRedirection();

app.MapOrganisationEndpoints();
app.MapCatalogueEndpoints();
app.MapSuppliersEndpoints();

app.Run();
