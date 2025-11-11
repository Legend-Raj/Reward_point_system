using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Domain.Extensions;
using Agdata.Rewards.Infrastructure.SqlServer;
using Agdata.Rewards.Presentation.Api.Models.Requests;
using Agdata.Rewards.Presentation.Api.Models.Responses;
using Agdata.Rewards.Presentation.Api.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Use SQL Server for data persistence
builder.Services.AddRewardsSqlServer(builder.Configuration, options =>
{
	options.AddByEmail("devops@agdata.com");
});

// For development/testing: Keep InMemory available via conditional compilation or environment variable
// #if DEBUG
// builder.Services.AddRewardsInMemory(options => { options.AddByEmail("devops@agdata.com"); });
// #endif

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
		var status = ex.StatusCode;
		context.Response.StatusCode = status;
		await context.Response.WriteAsJsonAsync(new ProblemDetails
		{
				Title = status == StatusCodes.Status403Forbidden ? DomainErrors.Authorization.ForbiddenTitle : DomainErrors.Errors.DomainViolationTitle,
			Detail = ex.Message,
			Status = status,
			Type = $"https://httpstatuses.io/{status}"
		});
	}
});

app.MapGet("/", () => Results.Ok("Rewards API is running"))
   .WithName("GetStatus")
   .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Application.Json);


var users = app.MapGroup("/users").WithTags("Users");

users.MapPost(string.Empty, async ([FromBody] CreateUserRequest request, IUserService service) =>
{
	var errors = RequestValidator.Validate(request);
	if (errors.Count > 0)
	{
		return Results.ValidationProblem(errors);
	}

	var user = await service.CreateNewUserAsync(
		request.NormalizeFirstName(),
		request.NormalizeMiddleName(),
		request.NormalizeLastName(),
		request.NormalizeEmail(),
		request.NormalizeEmployeeId());
	return Results.Created($"/users/{user.Id}", UserResponse.From(user));
}).WithName("CreateUser");

users.MapPatch("/{userId:guid}", async (
	Guid userId,
	[FromBody] UpdateUserRequest request,
	IUserRepository userRepository,
	IUnitOfWork unitOfWork) =>
{
	var errors = RequestValidator.Validate(request);
	if (errors.Count > 0)
	{
		return Results.ValidationProblem(errors);
	}

	var user = await userRepository.GetUserByIdForUpdateAsync(userId) ?? throw new DomainException(DomainErrors.User.NotFound);

	var wantsNameUpdate = request.HasNameChange;
	if (wantsNameUpdate)
	{
		var first = request.NormalizeFirstName() ?? user.Name.FirstName;
		var middle = request.MiddleName is null ? user.Name.MiddleName : request.NormalizeMiddleName();
		var last = request.NormalizeLastName() ?? user.Name.LastName;
		user.Rename(PersonName.Create(first, middle, last));
	}

	if (request.Email is not null)
	{
		var desiredEmail = request.NormalizeEmail()!;
		var email = new Email(desiredEmail);
		var existing = await userRepository.GetUserByEmailAsync(email);
		if (existing is not null && existing.Id != user.Id)
		{
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				[nameof(UpdateUserRequest.Email)] = new[] { DomainErrors.User.EmailExists }
			});
		}
		user.ChangeEmail(email);
	}

	if (request.EmployeeId is not null)
	{
		var desiredEmployeeId = request.NormalizeEmployeeId()!;
		var employeeId = new EmployeeId(desiredEmployeeId);
		var existingEmployee = await userRepository.GetUserByEmployeeIdAsync(employeeId);
		if (existingEmployee is not null && existingEmployee.Id != user.Id)
		{
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				[nameof(UpdateUserRequest.EmployeeId)] = new[] { DomainErrors.User.EmployeeIdExists }
			});
		}
		user.ChangeEmployeeId(employeeId);
	}

	if (request.IsActive.HasValue)
	{
		if (request.IsActive.Value)
		{
			user.Activate();
		}
		else
		{
			user.Deactivate();
		}
	}

	await unitOfWork.SaveChangesAsync();

	return Results.Ok(UserResponse.From(user));
}).WithName("UpdateUser");

users.MapGet("/{userId:guid}", async (Guid userId, IUserService service) =>
{
	var user = await service.GetUserByIdAsync(userId);
	return user is null ? Results.NotFound() : Results.Ok(UserResponse.From(user));
}).WithName("GetUser");

users.MapGet("/top-3", async (IUserRepository userRepository) =>
{
	var topEmployees = await userRepository.GetTop3EmployeesWithHighestRewardsAsync();
	var response = topEmployees.Select(UserResponse.From).ToList();
	return Results.Ok(response);
}).WithName("GetTop3Employees");

// ============================================================================
// ADMIN - EVENT MANAGEMENT ENDPOINTS (Admin-only)
// ============================================================================
var adminEvents = app.MapGroup("/admin/events").WithTags("Admin - Events");

adminEvents.MapPost(string.Empty, async ([FromBody] CreateEventRequest request, IEventService eventService) =>
{
	var errors = RequestValidator.Validate(request);
	if (errors.Count > 0)
	{
		return Results.ValidationProblem(errors);
	}

	var created = await eventService.CreateEventAsync(request.Admin!.ToAdmin(), request.NormalizeName(), request.NormalizeOccursAt(), request.IsActive);
	return Results.Created($"/admin/events/{created.Id}", created);
}).WithName("CreateEvent");

adminEvents.MapPut("/{eventId:guid}", async (Guid eventId, [FromBody] UpdateEventRequest request, IEventService eventService) =>
{
	var errors = RequestValidator.Validate(request);
	if (errors.Count > 0)
	{
		return Results.ValidationProblem(errors);
	}

	var updated = await eventService.UpdateEventAsync(request.Admin!.ToAdmin(), eventId, request.NormalizeName(), request.NormalizeOccursAt());
	return Results.Ok(updated);
}).WithName("UpdateEvent");

adminEvents.MapPost("/{eventId:guid}/active/{isActive:bool}", async (Guid eventId, bool isActive, [FromBody] AdminActionDto admin, IEventService eventService) =>
{
	var evt = await eventService.SetEventActiveStateAsync(admin.ToAdmin(), eventId, isActive);
	return Results.Ok(evt);
}).WithName("SetEventActive");

// ============================================================================
// PUBLIC - EVENT BROWSING (Read-only access for users)
// ============================================================================
var events = app.MapGroup("/events").WithTags("Events");

events.MapGet(string.Empty, async ([FromQuery] bool? onlyActive, IEventService eventService) =>
{
	if (onlyActive.GetValueOrDefault(true))
	{
		return Results.Ok(await eventService.GetActiveEventsAsync());
	}

	return Results.Ok(await eventService.GetAllEventsAsync());
}).WithName("ListEvents").Produces(StatusCodes.Status200OK);

// ============================================================================
// ADMIN - PRODUCT MANAGEMENT ENDPOINTS (Admin-only)
// ============================================================================
var adminProducts = app.MapGroup("/admin/products").WithTags("Admin - Products");

adminProducts.MapPost(string.Empty, async ([FromBody] CreateProductDto dto, IProductCatalogService service) =>
{
	var product = await service.CreateProductAsync(
		dto.Admin.ToAdmin(),
		dto.NormalizeName(),
		dto.NormalizeDescription(),
		dto.PointsCost,
		dto.NormalizeImageUrl(),
		dto.Stock,
		dto.IsActive);
	return Results.Created($"/admin/products/{product.Id}", product);
}).WithName("CreateProduct");

adminProducts.MapPatch("/{productId:guid}", async (
	Guid productId,
	[FromBody] UpdateProductDto dto,
	IProductCatalogService service) =>
{
	var product = await service.UpdateProductAsync(
		dto.Admin.ToAdmin(),
		productId,
		dto.Name,
		dto.Description,
		dto.PointsCost,
		dto.ImageUrl,
		dto.Stock,
		dto.IsActive);
	return Results.Ok(product);
}).WithName("UpdateProduct");

adminProducts.MapDelete("/{productId:guid}", async (Guid productId, [FromBody] AdminActionDto admin, IProductCatalogService service) =>
{
	await service.DeleteProductAsync(admin.ToAdmin(), productId);
	return Results.NoContent();
}).WithName("DeleteProduct");

// ============================================================================
// PUBLIC - PRODUCT CATALOG (Read-only access for users)
// ============================================================================
var products = app.MapGroup("/products").WithTags("Products");

products.MapGet(string.Empty, async ([FromQuery] bool? onlyActive, IProductCatalogService service) =>
{
	var productList = await service.GetCatalogAsync(onlyActive ?? true);
	return Results.Ok(productList);
}).WithName("ListProducts");

// ============================================================================
// ADMIN - POINTS ALLOCATION (Admin-only)
// ============================================================================
var adminPoints = app.MapGroup("/admin/points").WithTags("Admin - Points");

adminPoints.MapPost("/earn", async ([FromBody] EarnPointsWithAdminDto dto, IPointsLedgerService service) =>
{
	var actor = dto.ToAdmin();
	var transaction = await service.EarnAsync(actor, dto.UserId, dto.EventId, dto.Points);
	return Results.Ok(transaction);
}).WithName("EarnPoints");

// ============================================================================
// PUBLIC - POINTS HISTORY (User can view their own transaction history)
// ============================================================================
var points = app.MapGroup("/points").WithTags("Points");

points.MapGet("/history/{userId:guid}", async (Guid userId, [FromQuery] int? skip, [FromQuery] int? take, IPointsLedgerService service) =>
{
	const int defaultTake = 50;
	var page = await service.GetUserTransactionHistoryAsync(userId, skip.GetValueOrDefault(0), take.GetValueOrDefault(defaultTake));
	var response = new PagedResponse<LedgerEntryResponse>(
		page.Items.Select(LedgerEntryResponse.From).ToList(),
		page.TotalCount,
		page.Skip,
		page.Take);
	return Results.Ok(response);
}).WithName("GetPointsHistory");

// ============================================================================
// REDEMPTION REQUESTS (Mixed - submit is user, actions are admin)
// ============================================================================
var redemptionRequests = app.MapGroup("/redemption-requests").WithTags("Redemptions");

// User action: Submit redemption request
redemptionRequests.MapPost(string.Empty, async ([FromBody] SubmitRedemptionRequestDto dto, IRedemptionRequestService service) =>
{
	var requestId = await service.RequestRedemptionAsync(dto.UserId, dto.ProductId);
	return Results.Accepted($"/redemption-requests/{requestId}", new { Id = requestId });
}).WithName("SubmitRedemptionRequest");

// Admin action: Approve redemption
redemptionRequests.MapPost("/{requestId:guid}/approve", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.ApproveRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("ApproveRedemptionRequest");

// Admin action: Deliver redemption
redemptionRequests.MapPost("/{requestId:guid}/deliver", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.DeliverRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("DeliverRedemptionRequest");

// Admin action: Reject redemption
redemptionRequests.MapPost("/{requestId:guid}/reject", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.RejectRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("RejectRedemptionRequest");

// Admin action: Cancel redemption
redemptionRequests.MapPost("/{requestId:guid}/cancel", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.CancelRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("CancelRedemptionRequest");

app.Run();

public sealed record CreateProductDto(AdminActionDto Admin, string Name, string? Description, int PointsCost, string? ImageUrl, int? Stock, bool IsActive = true)
{
	public string NormalizeName() => Name.NormalizeRequired();
	public string? NormalizeDescription() => Description.NormalizeOptional();
	public string? NormalizeImageUrl() => ImageUrl.NormalizeOptional();
}

public sealed record UpdateProductDto(AdminActionDto Admin, string? Name, string? Description, int? PointsCost, string? ImageUrl, int? Stock, bool? IsActive)
{
	public string? NormalizeName() => Name?.NormalizeRequired();
	public string? TryGetDescription(string? current) => Description is null ? current : Description.NormalizeOptional();
	public string? TryGetImageUrl(string? current) => ImageUrl is null ? current : ImageUrl.NormalizeOptional();
}

public sealed record AdminActionDto(string FirstName, string? MiddleName, string LastName, string Email, string EmployeeId)
{
	public Admin ToAdmin() => Admin.CreateNew(FirstName, MiddleName, LastName, Email, EmployeeId);
}

public sealed record EarnPointsWithAdminDto(
	string FirstName,
	string? MiddleName,
	string LastName,
	string Email,
	string EmployeeId,
	Guid UserId,
	Guid EventId,
	int Points)
{
	public Admin ToAdmin() => Admin.CreateNew(FirstName, MiddleName, LastName, Email, EmployeeId);
}
