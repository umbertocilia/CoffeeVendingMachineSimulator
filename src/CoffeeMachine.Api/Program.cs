using CoffeeMachine.Infrastructure;
using CoffeeMachine.Infrastructure.Realtime;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddCoffeeMachineApplication();
builder.Services.AddCoffeeMachineInfrastructure(builder.Configuration);

builder.Host.UseSerilog((context, _, configuration) =>
{
    var path = context.Configuration["Logging:File:Path"] ?? "logs/coffee-machine-.log";
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File(path, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapControllers();
app.MapHub<MachineHub>("/hubs/machine");
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
