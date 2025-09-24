var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("AGDATA Rewards API placeholder"));

app.Run();
