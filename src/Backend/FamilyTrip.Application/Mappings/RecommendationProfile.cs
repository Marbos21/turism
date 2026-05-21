using AutoMapper;
using FamilyTrip.Application.DTOs.Recommendation;
using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Mappings;

public class RecommendationProfile : Profile
{
    public RecommendationProfile()
    {
        CreateMap<Recommendation, RecommendationDto>()
            .ForMember(dest => dest.Destination, opt => opt.Ignore());
    }
}
