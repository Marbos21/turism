using FamilyTrip.Application.DTOs.Destination;

namespace FamilyTrip.Application.Services;

public interface IDestinationAppService
{
    Task<List<DestinationDto>> SearchAsync(DestinationSearchRequest request, CancellationToken cancellationToken = default);
    Task<DestinationDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DestinationDto>> GetPopularAsync(CancellationToken cancellationToken = default);
}
