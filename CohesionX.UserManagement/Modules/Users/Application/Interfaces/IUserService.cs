using CohesionX.UserManagement.Modules.Users.Application.DTOs;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces
{
	public interface IUserService
	{
		Task<Guid> RegisterUserAsync(UserRegisterDto dto);
	}
}
