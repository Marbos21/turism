using AutoMapper;
using FamilyTrip.Application.DTOs.Family;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Domain.Interfaces;

namespace FamilyTrip.Application.Services;

public class FamilyAppService : IFamilyAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FamilyAppService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<FamilyDto>> GetUserFamiliesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var families = await _unitOfWork.Families.FindAsync(f => f.UserId == userId, cancellationToken);
        var result = new List<FamilyDto>();
        foreach (var family in families)
        {
            var dto = _mapper.Map<FamilyDto>(family);
            var members = await _unitOfWork.FamilyMembers.FindAsync(m => m.FamilyId == family.Id, cancellationToken);
            dto.Members = _mapper.Map<List<FamilyMemberDto>>(members);
            result.Add(dto);
        }
        return result;
    }

    public async Task<FamilyDto> CreateFamilyAsync(Guid userId, CreateFamilyRequest request, CancellationToken cancellationToken = default)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            UserId = userId,
            BudgetLevel = request.BudgetLevel,
            PreferredClimate = request.PreferredClimate,
            TravelStyle = request.TravelStyle,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Families.AddAsync(family, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<FamilyDto>(family);
        dto.Members = new List<FamilyMemberDto>();
        return dto;
    }

    public async Task<FamilyDto> GetFamilyByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(id, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        var dto = _mapper.Map<FamilyDto>(family);
        var members = await _unitOfWork.FamilyMembers.FindAsync(m => m.FamilyId == family.Id, cancellationToken);
        dto.Members = _mapper.Map<List<FamilyMemberDto>>(members);
        return dto;
    }

    public async Task<FamilyDto> UpdateFamilyAsync(Guid id, Guid userId, UpdateFamilyRequest request, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(id, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        family.Name = request.Name;
        family.BudgetLevel = request.BudgetLevel;
        family.PreferredClimate = request.PreferredClimate;
        family.TravelStyle = request.TravelStyle;

        await _unitOfWork.Families.UpdateAsync(family, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<FamilyDto>(family);
        var members = await _unitOfWork.FamilyMembers.FindAsync(m => m.FamilyId == family.Id, cancellationToken);
        dto.Members = _mapper.Map<List<FamilyMemberDto>>(members);
        return dto;
    }

    public async Task<FamilyMemberDto> AddMemberAsync(Guid familyId, Guid userId, AddFamilyMemberRequest request, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        var member = new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = request.Name,
            BirthDate = request.BirthDate,
            Interests = request.Interests,
            SpecialNeeds = request.SpecialNeeds,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.FamilyMembers.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FamilyMemberDto>(member);
    }

    public async Task RemoveMemberAsync(Guid familyId, Guid memberId, Guid userId, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        var member = await _unitOfWork.FamilyMembers.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.FamilyId != familyId)
            throw new KeyNotFoundException("Member not found.");

        await _unitOfWork.FamilyMembers.DeleteAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
