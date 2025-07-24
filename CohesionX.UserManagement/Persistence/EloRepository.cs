using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Persistence;

public class EloRepository : Repository<EloHistory>, IEloRepository
{
	private readonly AppDbContext _context;

	public EloRepository(AppDbContext context) : base(context)
	{
		_context = context;
	}

	public async Task<List<EloHistory>> GetByUserIdAsync(Guid userId)
		=> await _context.EloHistories.Where(er => er.UserId == userId).ToListAsync();
}