using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Services;
using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces
{
	public interface IUserService
	{
		Task<RegistrationResult> RegisterUserAsync(UserRegisterDto dto);
		Task<UserProfileDto> GetProfileAsync(Guid userId);
		Task<List<User>> GetFilteredUser(string? dialect, int? minElo, int? maxElo, int? maxWorkload, int? limit);
		Task UpdateAvailabilityAuditAsync(Guid userId, UserAvailabilityRedisDto existingAvailability, string? ipAddress, string? userAgent);
	}
}
