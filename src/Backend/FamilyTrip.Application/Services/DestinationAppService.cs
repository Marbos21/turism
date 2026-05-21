using AutoMapper;
using FamilyTrip.Application.DTOs.Destination;
using FamilyTrip.Domain.Interfaces;

namespace FamilyTrip.Application.Services;

public class DestinationAppService : IDestinationAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DestinationAppService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<DestinationDto>> SearchAsync(DestinationSearchRequest request, CancellationToken cancellationToken = default)
    {
        var all = await _unitOfWork.Destinations.GetAllAsync(cancellationToken);

        var filtered = all.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            filtered = filtered.Where(d =>
                d.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                d.Country.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                d.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Country))
            filtered = filtered.Where(d => d.Country.Equals(request.Country, StringComparison.OrdinalIgnoreCase));

        if (request.Climate.HasValue)
            filtered = filtered.Where(d => d.Climate == request.Climate.Value);

        if (request.MinChildFriendlyScore.HasValue)
            filtered = filtered.Where(d => d.ChildFriendlyScore >= request.MinChildFriendlyScore.Value);

        if (request.MaxCostPerDay.HasValue)
            filtered = filtered.Where(d => d.AvgCostPerDay <= request.MaxCostPerDay.Value);

        if (!string.IsNullOrWhiteSpace(request.Category))
            filtered = filtered.Where(d => d.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));

        return _mapper.Map<List<DestinationDto>>(filtered.ToList());
    }

    public async Task<DestinationDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var destination = await _unitOfWork.Destinations.GetByIdAsync(id, cancellationToken);
        if (destination is null)
            throw new KeyNotFoundException("Destination not found.");

        return _mapper.Map<DestinationDto>(destination);
    }

    public async Task<List<DestinationDto>> GetPopularAsync(CancellationToken cancellationToken = default)
    {
        var all = await _unitOfWork.Destinations.GetAllAsync(cancellationToken);
        var popular = all
            .OrderByDescending(d => d.ChildFriendlyScore)
            .ThenBy(d => d.AvgCostPerDay)
            .Take(6)
            .ToList();

        return _mapper.Map<List<DestinationDto>>(popular);
    }
}
