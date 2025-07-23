using SharedLibrary.RequestResponseModels.UserManagement;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

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
