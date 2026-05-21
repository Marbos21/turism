using System.Security.Claims;
using Asp.Versioning;
using FluentValidation;
using FamilyTrip.Application.DTOs.TripPlan;
using FamilyTrip.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTrip.API.Controllers.v1;

/// <summary>
/// Manages trip plans and itinerary generation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trip-plans")]
[Authorize]
public class TripPlanController : ControllerBase
{
    private readonly ITripPlanAppService _tripPlanService;
    private readonly IValidator<CreateTripPlanRequest> _createValidator;

    public TripPlanController(ITripPlanAppService tripPlanService, IValidator<CreateTripPlanRequest> createValidator)
    {
        _tripPlanService = tripPlanService;
        _createValidator = createValidator;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(claim!);
    }

    /// <summary>
    /// Returns all trip plans for a family.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TripPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid familyId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var plans = await _tripPlanService.GetByFamilyAsync(familyId, userId, cancellationToken);
            return Ok(plans);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new trip plan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTripPlanRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

        var userId = GetUserId();
        try
        {
            var plan = await _tripPlanService.CreateAsync(userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, plan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Returns a trip plan by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var plan = await _tripPlanService.GetByIdAsync(id, userId, cancellationToken);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a trip plan.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTripPlanRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var plan = await _tripPlanService.UpdateAsync(id, userId, request, cancellationToken);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a trip plan.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            await _tripPlanService.DeleteAsync(id, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Generates an AI-optimized itinerary for a trip plan.
    /// </summary>
    [HttpPost("{id:guid}/itinerary/generate")]
    [ProducesResponseType(typeof(List<ItineraryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateItinerary(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var itinerary = await _tripPlanService.GenerateItineraryAsync(id, userId, cancellationToken);
            return Ok(itinerary);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }
}
