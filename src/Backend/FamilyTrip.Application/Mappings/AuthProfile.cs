using AutoMapper;
using FamilyTrip.Application.DTOs.Auth;
using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Mappings;

public class AuthProfile : Profile
{
    public AuthProfile()
    {
        CreateMap<User, UserInfoDto>();
    }
}
