using AutoMapper;
using SharedLibrary.RequestResponseModels.UserManagement;
using SharedLibrary.AppEnums;
using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Application.Mapping;

/// <summary>
/// AutoMapper profile for mapping user domain entities to DTOs and request models.
/// </summary>
public class UserProfileMapping : Profile
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UserProfileMapping"/> class and configures mapping rules.
	/// </summary>
	public UserProfileMapping()
	{
		CreateMap<User, AvailableUsersDto>()
			.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.DialectExpertise, opt => opt.MapFrom(src => src.Dialects != null ? src.Dialects.Select(d => d.Dialect).ToList() : new List<string>()))
			.ForMember(dest => dest.BypassQaComparison, opt => opt.MapFrom(src => src.Role == UserRoleType.Professional.ToDisplayName()))
			.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

		CreateMap<UserStatistics, UserStatisticsDto>();

		CreateMap<UpdateVerificationRequirementsRequest, UserVerificationRequirement>()
			.ForMember(dest => dest.ValidationRulesJson, opt => opt.Ignore())
			.AfterMap((src, dest) =>
			{
				dest.ValidationRulesJson = System.Text.Json.JsonSerializer.Serialize(src.ValidationRules);
			})
			.ForMember(dest => dest.Id, opt => opt.Ignore());
	}
}
