using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IUserStaticticsService
{
	Task<UserStatistics> GetUserStatisticsAsync(Guid userId);
	Task<UserStatistics> UpdateUserStatisticsAsync(Guid userId, RecommendedEloChangeDto userStatistics);
}
