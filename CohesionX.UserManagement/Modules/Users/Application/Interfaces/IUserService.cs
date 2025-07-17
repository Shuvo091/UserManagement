using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Services;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces
{
	public interface IUserService
	{
		Task<RegistrationResult> RegisterUserAsync(UserRegisterDto dto, string? idPhotoPath);
		Task<UserProfileDto> GetProfileAsync(Guid userId);
	}
}
