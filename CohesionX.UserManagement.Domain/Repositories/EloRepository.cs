using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Database.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="EloHistory"/> entities.
/// Inherits from the generic <see cref="Repository{T}"/> base class.
/// </summary>
public class EloRepository : Repository<EloHistory>, IEloRepository
{
	private readonly AppDbContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="EloRepository"/> class with the specified database context.
	/// </summary>
	/// <param name="context">The application database context.</param>
	public EloRepository(AppDbContext context)
            : base(context)
	{
		_context = context;
	}

	/// <summary>
	/// Retrieves all Elo history records associated with the specified user.
	/// </summary>
	/// <param name="userId">The unique identifier of the user whose Elo history is requested.</param>
	/// <returns>
	/// A task representing the asynchronous operation, containing a list of <see cref="EloHistory"/> entries.
	/// </returns>
	public async Task<List<EloHistory>> GetByUserIdAsync(Guid userId)
		=> await _context.EloHistories
			.Where(er => er.UserId == userId)
			.ToListAsync();
}
