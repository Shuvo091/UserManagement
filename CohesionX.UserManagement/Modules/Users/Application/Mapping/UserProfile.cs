using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;

namespace CohesionX.UserManagement.Modules.Users.Application.Mapping;

public class UserProfileMapping : Profile
{
	public UserProfileMapping()
	{
		CreateMap<User, UserProfileDto>()
			.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src =>
				src.VerificationRecords.Any(v => v.Status == "Verified")));

		CreateMap<User, AvailableUsersDto>()
			.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.EloRating, opt => opt.MapFrom(src => src.Statistics != null ? src.Statistics.CurrentElo : 0))
			.ForMember(dest => dest.PeakElo, opt => opt.MapFrom(src => src.Statistics != null ? src.Statistics.PeakElo : 0))
			.ForMember(dest => dest.DialectExpertise, opt => opt.MapFrom(src => src.Dialects != null ? src.Dialects.Select(d => d.Dialect).ToList() : new List<string>()))
			.ForMember(dest => dest.GamesPlayed, opt => opt.MapFrom(src => src.Statistics != null ? src.Statistics.GamesPlayed : 0))
			.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));


		CreateMap<UserDialect, UserDialectDto>();
		CreateMap<UserStatistics, UserStatisticsDto>();
		CreateMap<EloHistory, EloHistoryDto>();
		CreateMap<JobCompletion, JobCompletionDto>();
		CreateMap<JobClaim, JobClaimDto>();
		CreateMap<AuditLog, AuditLogDto>();
		CreateMap<VerificationRecord, VerificationRecordDto>();
		CreateMap<EloHistory, EloHistoryDto>();
	}
}
