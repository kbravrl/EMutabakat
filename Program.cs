using EMutabakat.Components;
using EMutabakat.Data;
using EMutabakat.Services;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFirmaService, FirmaService>();
builder.Services.AddScoped<ICariGrupService, CariGrupService>();
builder.Services.AddScoped<ICariService, CariService>();
builder.Services.AddScoped<IMutabakatService, MutabakatService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Try a few times until PostgreSQL in Docker is ready.
for (int i = 0; i < 10; i++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        break;
    }
    catch
    {
        if (i == 9) throw;
        Thread.Sleep(3000);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();