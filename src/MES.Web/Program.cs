using MES.Application.Common.Interfaces;
using MES.Application.Machines;
using MES.Infrastructure.Data;
using MES.Infrastructure.Data.Repositories;
using MES.Web.Middleware;
using MES.Application.Workstations;
using MES.Web.Prototype;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var database = new SqlDatabaseSettings
{
    ConnectionString = builder.Configuration.GetConnectionString("MES") ?? "",
    MachinesQuery = builder.Configuration["Database:MachinesQuery"] ?? "",
    CommandTimeoutSeconds = builder.Configuration.GetValue("Database:CommandTimeoutSeconds", 30)
};
if (database.CommandTimeoutSeconds <= 0) throw new InvalidOperationException("Timeout musi być większy od zera.");
builder.Services.AddSingleton(database);
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<IMachineRepository, MachineRepository>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddSingleton<IWorkstationCatalogStore, InMemoryWorkstationCatalogStore>();
builder.Services.AddScoped<ICurrentOperator, DemoOperator>();
builder.Services.AddScoped<IWorkstationCatalogService, WorkstationCatalogService>();
var app = builder.Build();
app.UseExceptionHandler("/Error");
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Workstations")
        && !app.Configuration.GetValue<bool>("WorkstationPrototype:Enabled"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();
app.Run();
