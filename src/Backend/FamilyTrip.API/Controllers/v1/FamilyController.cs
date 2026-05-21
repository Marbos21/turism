using System.Security.Claims;
using Asp.Versioning;
using FamilyTrip.Application.DTOs.Family;
using FamilyTrip.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTrip.API.Controllers.v1;

/// <summary>
/// Manages family profiles and members.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/families")]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly IFamilyAppService _familyService;

    public FamilyController(IFamilyAppService familyService)
    {
        _familyService = familyService;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(claim!);
    }

    /// <summary>
    /// Returns all families for the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FamilyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var families = await _familyService.GetUserFamiliesAsync(userId, cancellationToken);
        return Ok(families);
    }

    /// <summary>
    /// Creates a new family.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFamilyRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var family = await _familyService.CreateFamilyAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, family);
    }

    /// <summary>
    /// Returns a family by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var family = await _familyService.GetFamilyByIdAsync(id, userId, cancellationToken);
            return Ok(family);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a family.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFamilyRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var family = await _familyService.UpdateFamilyAsync(id, userId, request, cancellationToken);
            return Ok(family);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Adds a member to a family.
    /// </summary>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(FamilyMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddFamilyMemberRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            var member = await _familyService.AddMemberAsync(id, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, member);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a member from a family.
    /// </summary>
    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        try
        {
            await _familyService.RemoveMemberAsync(id, memberId, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }
}
