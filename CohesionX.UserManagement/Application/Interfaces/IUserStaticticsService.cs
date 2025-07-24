using SharedLibrary.RequestResponseModels.UserManagement;
using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Application.Interfaces;

public interface IUserStaticticsService
{
	Task<UserStatistics> GetUserStatisticsAsync(Guid userId);
	Task<UserStatistics> UpdateUserStatisticsAsync(Guid userId, RecommendedEloChangeDto userStatistics);
}
