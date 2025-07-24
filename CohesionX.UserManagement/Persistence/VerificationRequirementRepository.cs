using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CohesionX.UserManagement.Persistence;

public class VerificationRequirementRepository : Repository<UserVerificationRequirement>, IVerificationRequirementRepository
{
	private readonly AppDbContext _context;

	public VerificationRequirementRepository(AppDbContext context) : base(context)
	{
		_context = context;
	}

	public async Task<UserVerificationRequirement?> GetVerificationRequirement()
	{
		return await _context.UserVerificationRequirements.FirstOrDefaultAsync();
	}
}
