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
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Presentation.Api.Models.Requests;
using Agdata.Rewards.Presentation.Api.Models.Responses;
using Agdata.Rewards.Presentation.Api.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

app.MapPost("/users", async ([FromBody] CreateUserRequest request, IUserService service) =>
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

app.MapPatch("/users/{userId:guid}", async (
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

	var user = await userRepository.GetUserByIdAsync(userId) ?? throw new DomainException(DomainErrors.User.NotFound);

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

	userRepository.UpdateUser(user);
	await unitOfWork.SaveChangesAsync();

	return Results.Ok(UserResponse.From(user));
}).WithName("UpdateUser");

app.MapGet("/users/{userId:guid}", async (Guid userId, IUserService service) =>
{
	var user = await service.GetUserByIdAsync(userId);
	return user is null ? Results.NotFound() : Results.Ok(UserResponse.From(user));
}).WithName("GetUser");

app.MapPost("/events", async ([FromBody] CreateEventRequest request, IEventService eventService) =>
{
	var errors = RequestValidator.Validate(request);
	if (errors.Count > 0)
	{
		return Results.ValidationProblem(errors);
	}

	var created = await eventService.CreateEventAsync(request.Admin!.ToAdmin(), request.NormalizeName(), request.NormalizeOccursAt(), request.IsActive);
	return Results.Created($"/events/{created.Id}", created);
}).WithName("CreateEvent");

app.MapPut("/events/{eventId:guid}", async (Guid eventId, [FromBody] UpdateEventRequest request, IEventService eventService) =>
{
	var errors = RequestValidator.Validate(request);
	if (errors.Count > 0)
	{
		return Results.ValidationProblem(errors);
	}

	var updated = await eventService.UpdateEventAsync(request.Admin!.ToAdmin(), eventId, request.NormalizeName(), request.NormalizeOccursAt());
	return Results.Ok(updated);
}).WithName("UpdateEvent");

app.MapGet("/events", async ([FromQuery] bool? onlyActive, IEventService eventService) =>
{
	if (onlyActive.GetValueOrDefault(true))
	{
		return Results.Ok(await eventService.GetActiveEventsAsync());
	}

	return Results.Ok(await eventService.GetAllEventsAsync());
}).WithName("ListEvents").Produces(StatusCodes.Status200OK);

app.MapPost("/events/{eventId:guid}/active/{isActive:bool}", async (Guid eventId, bool isActive, [FromBody] AdminActionDto admin, IEventService eventService) =>
{
	var evt = await eventService.SetEventActiveStateAsync(admin.ToAdmin(), eventId, isActive);
	return Results.Ok(evt);
}).WithName("SetEventActive");

app.MapPost("/products", async ([FromBody] CreateProductDto dto, IProductCatalogService service) =>
{
	var product = await service.CreateProductAsync(
		dto.Admin.ToAdmin(),
		dto.NormalizeName(),
		dto.NormalizeDescription(),
		dto.PointsCost,
		dto.NormalizeImageUrl(),
		dto.Stock,
		dto.IsActive);
	return Results.Created($"/products/{product.Id}", product);
}).WithName("CreateProduct");

app.MapGet("/products", async ([FromQuery] bool? onlyActive, IProductCatalogService service) =>
{
	var products = await service.GetCatalogAsync(onlyActive ?? true);
	return Results.Ok(products);
}).WithName("ListProducts");

app.MapPatch("/products/{productId:guid}", async (
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

app.MapDelete("/products/{productId:guid}", async (Guid productId, [FromBody] AdminActionDto admin, IProductCatalogService service) =>
{
	await service.DeleteProductAsync(admin.ToAdmin(), productId);
	return Results.NoContent();
}).WithName("DeleteProduct");

app.MapPost("/points/earn", async ([FromBody] EarnPointsWithAdminDto dto, IPointsLedgerService service) =>
{
	var actor = dto.ToAdmin();
	var transaction = await service.EarnAsync(actor, dto.UserId, dto.EventId, dto.Points);
	return Results.Ok(transaction);
}).WithName("EarnPoints");

app.MapGet("/points/history/{userId:guid}", async (Guid userId, [FromQuery] int? skip, [FromQuery] int? take, IPointsLedgerService service) =>
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

var redemptionRequests = app.MapGroup("/redemption-requests");

redemptionRequests.MapPost(string.Empty, async ([FromBody] SubmitRedemptionRequestDto dto, IRedemptionRequestService service) =>
{
	var requestId = await service.RequestRedemptionAsync(dto.UserId, dto.ProductId);
	return Results.Accepted($"/redemption-requests/{requestId}", new { Id = requestId });
}).WithName("SubmitRedemptionRequest");

redemptionRequests.MapPost("/{requestId:guid}/approve", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.ApproveRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("ApproveRedemptionRequest");

redemptionRequests.MapPost("/{requestId:guid}/deliver", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.DeliverRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("DeliverRedemptionRequest");

redemptionRequests.MapPost("/{requestId:guid}/reject", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.RejectRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("RejectRedemptionRequest");

redemptionRequests.MapPost("/{requestId:guid}/cancel", async (Guid requestId, [FromBody] AdminActionDto dto, IRedemptionRequestService service) =>
{
	await service.CancelRedemptionAsync(dto.ToAdmin(), requestId);
	return Results.NoContent();
}).WithName("CancelRedemptionRequest");

app.Run();

public sealed record CreateProductDto(AdminActionDto Admin, string Name, string? Description, int PointsCost, string? ImageUrl, int? Stock, bool IsActive = true)
{
	public string NormalizeName() => Name.Trim();
	public string? NormalizeDescription() => string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
	public string? NormalizeImageUrl() => string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim();
}

public sealed record UpdateProductDto(AdminActionDto Admin, string? Name, string? Description, int? PointsCost, string? ImageUrl, int? Stock, bool? IsActive)
{
	public string? NormalizeName() => Name?.Trim();
	public string? TryGetDescription(string? current) => Description is null ? current : (string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());
	public string? TryGetImageUrl(string? current) => ImageUrl is null ? current : (string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim());
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
