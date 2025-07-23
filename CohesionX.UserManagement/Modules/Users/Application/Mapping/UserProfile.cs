using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using SharedLibrary.RequestResponseModels.UserManagement;
using SharedLibrary.AppEnums;

namespace CohesionX.UserManagement.Modules.Users.Application.Mapping;

public class UserProfileMapping : Profile
{
	public UserProfileMapping()
	{

		CreateMap<User, AvailableUsersDto>()
			.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.DialectExpertise, opt => opt.MapFrom(src => src.Dialects != null ? src.Dialects.Select(d => d.Dialect).ToList() : new List<string>()))
			.ForMember(dest => dest.BypassQaComparison, opt => opt.MapFrom(src => src.Role == UserRoleType.Professional.ToDisplayName()))
			.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

		CreateMap<UserStatistics, UserStatisticsDto>();
	}
}