using System.Net.Mime;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRewardsInMemory(options =>
{
	options.AddByEmail("devops@agdata.com");
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (context, next) =>
{
	try
	{
		await next();
	}
	catch (DomainException ex)
	{
		context.Response.StatusCode = StatusCodes.Status400BadRequest;
		await context.Response.WriteAsJsonAsync(new ProblemDetails
		{
			Title = "Domain rule violated",
			Detail = ex.Message,
			Status = StatusCodes.Status400BadRequest,
			Type = "https://httpstatuses.io/400"
		});
	}
});

app.MapGet("/", () => Results.Ok("Rewards API is running ✅"))
   .WithName("GetStatus")
   .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Application.Json);

app.MapPost("/users", async ([FromBody] CreateUserDto dto, IUserService service) =>
{
	var user = await service.CreateNewUserAsync(dto.Name, dto.Email, dto.EmployeeId);
	return Results.Created($"/users/{user.Id}", user);
}).WithName("CreateUser");

app.MapPatch("/users/{id:guid}", async (Guid id, [FromBody] UpdateUserDto dto, IUserService service) =>
{
	var user = await service.UpdateUserAsync(id, dto.Name, dto.Email, dto.EmployeeId, dto.IsActive);
	return Results.Ok(user);
}).WithName("UpdateUser");

app.MapGet("/users/{id:guid}", async (Guid id, IUserService service) =>
{
	var user = await service.GetByIdAsync(id);
	return user is null ? Results.NotFound() : Results.Ok(user);
}).WithName("GetUser");

app.MapPost("/events", async ([FromBody] CreateEventDto dto, IPointsLedgerService service) =>
{
	var evt = await service.CreateEventAsync(dto.Name, dto.IsActive);
	return Results.Created($"/events/{evt.Id}", evt);
}).WithName("CreateEvent");

app.MapGet("/events", async ([FromQuery] bool? onlyActive, IPointsLedgerService service) =>
{
	var events = await service.ListEventsAsync(onlyActive ?? true);
	return Results.Ok(events);
}).WithName("ListEvents").Produces(StatusCodes.Status200OK);

app.MapPost("/events/{id:guid}/active/{isActive:bool}", async (Guid id, bool isActive, IPointsLedgerService service) =>
{
	var evt = await service.SetEventActiveAsync(id, isActive);
	return Results.Ok(evt);
}).WithName("SetEventActive");

app.MapPost("/products", async ([FromBody] CreateProductDto dto, IProductCatalogService service) =>
{
	var product = await service.CreateProductAsync(dto.Name, dto.CostPoints, dto.Stock, dto.IsActive);
	return Results.Created($"/products/{product.Id}", product);
}).WithName("CreateProduct");

app.MapGet("/products", async ([FromQuery] bool? onlyActive, IProductCatalogService service) =>
{
	var products = await service.GetCatalogAsync(onlyActive ?? true);
	return Results.Ok(products);
}).WithName("ListProducts");

app.MapPatch("/products/{id:guid}", async (Guid id, [FromBody] UpdateProductDto dto, IProductCatalogService service) =>
{
	var product = await service.UpdateProductAsync(id, dto.Name, dto.CostPoints, dto.Stock, dto.IsActive);
	return Results.Ok(product);
}).WithName("UpdateProduct");

app.MapDelete("/products/{id:guid}", async (Guid id, IProductCatalogService service) =>
{
	await service.DeleteProductAsync(id);
	return Results.NoContent();
}).WithName("DeleteProduct");

app.MapPost("/points/earn", async ([FromBody] EarnPointsDto dto, IPointsLedgerService service) =>
{
	var transaction = await service.EarnAsync(dto.UserId, dto.EventId, dto.Points);
	return Results.Ok(transaction);
}).WithName("EarnPoints");

app.MapGet("/points/history/{userId:guid}", async (Guid userId, IPointsLedgerService service) =>
{
	var history = await service.GetUserTransactionHistoryAsync(userId);
	return Results.Ok(history);
}).WithName("GetPointsHistory");

app.MapPost("/redemptions", async ([FromBody] RedemptionRequestDto dto, IRedemptionService service) =>
{
	var redemptionId = await service.RequestRedemptionAsync(dto.UserId, dto.ProductId);
	return Results.Accepted($"/redemptions/{redemptionId}", new { Id = redemptionId });
}).WithName("RequestRedemption");

app.MapPost("/redemptions/{id:guid}/approve", async (Guid id, [FromBody] AdminActionDto dto, IRedemptionService service) =>
{
	await service.ApproveRedemptionAsync(dto.ToAdmin(), id);
	return Results.NoContent();
}).WithName("ApproveRedemption");

app.MapPost("/redemptions/{id:guid}/deliver", async (Guid id, [FromBody] AdminActionDto dto, IRedemptionService service) =>
{
	await service.DeliverRedemptionAsync(dto.ToAdmin(), id);
	return Results.NoContent();
}).WithName("DeliverRedemption");

app.MapPost("/redemptions/{id:guid}/reject", async (Guid id, [FromBody] AdminActionDto dto, IRedemptionService service) =>
{
	await service.RejectRedemptionAsync(dto.ToAdmin(), id);
	return Results.NoContent();
}).WithName("RejectRedemption");

app.MapPost("/redemptions/{id:guid}/cancel", async (Guid id, [FromBody] AdminActionDto dto, IRedemptionService service) =>
{
	await service.CancelRedemptionAsync(dto.ToAdmin(), id);
	return Results.NoContent();
}).WithName("CancelRedemption");

app.Run();

public sealed record CreateUserDto(string Name, string Email, string EmployeeId);

public sealed record UpdateUserDto(string? Name, string? Email, string? EmployeeId, bool? IsActive);

public sealed record CreateEventDto(string Name, bool IsActive = true);

public sealed record CreateProductDto(string Name, int CostPoints, int? Stock, bool IsActive = true);

public sealed record UpdateProductDto(string? Name, int? CostPoints, int? Stock, bool? IsActive);

public sealed record EarnPointsDto(Guid UserId, Guid EventId, int Points);

public sealed record RedemptionRequestDto(Guid UserId, Guid ProductId);

public sealed record AdminActionDto(string Name, string Email, string EmployeeId)
{
	public Admin ToAdmin() => Admin.CreateNew(Name, Email, EmployeeId);
}
