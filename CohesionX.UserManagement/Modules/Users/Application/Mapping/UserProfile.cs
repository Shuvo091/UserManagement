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
