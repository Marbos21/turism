using AutoMapper;
using FamilyTrip.Application.DTOs.Family;
using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Mappings;

public class FamilyProfile : Profile
{
    public FamilyProfile()
    {
        CreateMap<Family, FamilyDto>()
            .ForMember(dest => dest.Members, opt => opt.Ignore());

        CreateMap<FamilyMember, FamilyMemberDto>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src =>
                DateTime.UtcNow.Year - src.BirthDate.Year -
                (DateTime.UtcNow.DayOfYear < src.BirthDate.DayOfYear ? 1 : 0)));
    }
}
