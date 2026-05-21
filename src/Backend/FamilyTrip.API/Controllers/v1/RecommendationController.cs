using System.Security.Claims;
using Asp.Versioning;
using FamilyTrip.Application.DTOs.Recommendation;
using FamilyTrip.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTrip.API.Controllers.v1;

/// <summary>
/// Generates and retrieves AI-powered destination recommendations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recommendations")]
[Authorize]
public class RecommendationController : ControllerBase
{
    private readonly IRecommendationAppService _recommendationService;

    public RecommendationController(IRecommendationAppService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(claim!);
    }

    /// <summary>
    /// Generates personalized destination recommendations for a family.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateRecommendationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate([FromBody] GenerateRecommendationsRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var recommendations = await _recommendationService.GenerateAsync(request, userId, cancellationToken);
            return Ok(recommendations);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Returns existing recommendations for a family.
    /// </summary>
    [HttpGet("family/{familyId:guid}")]
    [ProducesResponseType(typeof(List<RecommendationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByFamily(Guid familyId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var recommendations = await _recommendationService.GetByFamilyAsync(familyId, userId, cancellationToken);
            return Ok(recommendations);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }
}
