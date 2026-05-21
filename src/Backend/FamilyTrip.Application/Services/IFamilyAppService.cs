using FamilyTrip.Application.DTOs.Family;

namespace FamilyTrip.Application.Services;

public interface IFamilyAppService
{
    Task<List<FamilyDto>> GetUserFamiliesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FamilyDto> CreateFamilyAsync(Guid userId, CreateFamilyRequest request, CancellationToken cancellationToken = default);
    Task<FamilyDto> GetFamilyByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<FamilyDto> UpdateFamilyAsync(Guid id, Guid userId, UpdateFamilyRequest request, CancellationToken cancellationToken = default);
    Task<FamilyMemberDto> AddMemberAsync(Guid familyId, Guid userId, AddFamilyMemberRequest request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid familyId, Guid memberId, Guid userId, CancellationToken cancellationToken = default);
}
