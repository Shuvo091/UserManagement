using SharedLibrary.RequestResponseModels.UserManagement;
using CohesionX.UserManagement.Application.Interfaces;
using CohesionX.UserManagement.Domain.Entities;

namespace CohesionX.UserManagement.Application.Services;

public class UserStatisticsService : IUserStaticticsService
{
	public Task<UserStatistics> GetUserStatisticsAsync(Guid userId)
	{
		throw new NotImplementedException();
	}

	public Task<UserStatistics> UpdateUserStatisticsAsync(Guid userId, RecommendedEloChangeDto userStatistics)
	{
		throw new NotImplementedException();
	}
}
