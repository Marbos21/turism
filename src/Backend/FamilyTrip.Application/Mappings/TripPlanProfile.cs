using AutoMapper;
using FamilyTrip.Application.DTOs.TripPlan;
using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Mappings;

public class TripPlanProfile : Profile
{
    public TripPlanProfile()
    {
        CreateMap<TripPlan, TripPlanDto>()
            .ForMember(dest => dest.Destination, opt => opt.Ignore())
            .ForMember(dest => dest.ItineraryItems, opt => opt.Ignore());

        CreateMap<ItineraryItem, ItineraryItemDto>();
    }
}
