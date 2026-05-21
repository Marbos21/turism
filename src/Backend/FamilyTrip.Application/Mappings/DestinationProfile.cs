using AutoMapper;
using FamilyTrip.Application.DTOs.Destination;
using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Mappings;

public class DestinationProfile : Profile
{
    public DestinationProfile()
    {
        CreateMap<Destination, DestinationDto>();
    }
}
