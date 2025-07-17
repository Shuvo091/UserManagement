using CohesionX.UserManagement.Modules.Users.Domain.Entities;
using CohesionX.UserManagement.Modules.Users.Domain.Interfaces;
using CohesionX.UserManagement.Shared.Persistence;

namespace CohesionX.UserManagement.Modules.Users.Persistence;

public class UserRepository : IUserRepository
{
	private readonly AppDbContext _context;

	public UserRepository(AppDbContext context)
	{
		_context = context;
	}

	public Task AddAsync(User user) =>
		_context.Users.AddAsync(user).AsTask();

	public Task SaveChangesAsync() =>
		_context.SaveChangesAsync();
}
