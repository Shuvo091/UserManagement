using AutoMapper;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Domain.Constants;

namespace CohesionX.UserManagement.Modules.Users.Application.Mapping;

public class UserProfileMapping : Profile
{
	public UserProfileMapping()
	{

		CreateMap<User, AvailableUsersDto>()
			.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.DialectExpertise, opt => opt.MapFrom(src => src.Dialects != null ? src.Dialects.Select(d => d.Dialect).ToList() : new List<string>()))
			.ForMember(dest => dest.BypassQaComparison, opt => opt.MapFrom(src => src.Role == UserRole.PROFESSIONAL))
			.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));


		CreateMap<UserDialect, UserDialectDto>();
		CreateMap<UserStatistics, UserStatisticsDto>();
		CreateMap<EloHistory, EloHistoryDto>();
		CreateMap<JobCompletion, JobCompletionDto>();
		CreateMap<AuditLog, AuditLogDto>();
		CreateMap<VerificationRecord, VerificationRecordDto>();
		CreateMap<EloHistory, EloHistoryDto>();
	}
}


//public class RecentPerformanceResolver : IValueResolver<User, AvailableUsersDto, string>
//{
//	public string Resolve(User source, AvailableUsersDto destination, string destMember, ResolutionContext context)
//	{
//		var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
//		var recentHistories = source.EloHistories
//			.Where(h => h.ChangedAt >= sevenDaysAgo)
//			.OrderBy(h => h.ChangedAt)
//			.ToList();

//		if (!recentHistories.Any())
//			return "+0_over_7_days";

//		int eloChange = recentHistories.Last().NewElo - recentHistories.First().OldElo;
//		string prefix = eloChange >= 0 ? "+" : "";

//		return $"{prefix}{eloChange}_over_7_days";
//	}
//}
