using final_pro_c.Components;
//using final_pro_c.Data;
using final_pro_c.Services;
using final_pro_c.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
//----------------------
// SQL Database Layer Mock Context Registration Engine
//builder.Services.AddDbContext<AppDbContext>(options =>
    //options.UseInMemoryDatabase("IronCrusadeSqlDatabase"));

// Core Object Oriented Processing Realtime Service Engines
builder.Services.AddScoped<GameEngineService>();
//builder.Services.AddScoped<Services>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
