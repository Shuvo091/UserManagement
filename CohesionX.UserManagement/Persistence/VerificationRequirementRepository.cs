using CohesionX.UserManagement.Domain.Entities;
using CohesionX.UserManagement.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Persistence
{
	/// <summary>
	/// Repository implementation for managing <see cref="UserVerificationRequirement"/> entities.
	/// Provides methods to retrieve verification requirements configuration.
	/// </summary>
	public class VerificationRequirementRepository : Repository<UserVerificationRequirement>, IVerificationRequirementRepository
	{
		private readonly AppDbContext _context;

		/// <summary>
		/// Initializes a new instance of the <see cref="VerificationRequirementRepository"/> class
		/// with the specified database context.
		/// </summary>
		/// <param name="context">The application database context.</param>
		public VerificationRequirementRepository(AppDbContext context) : base(context)
		{
			_context = context;
		}

		/// <summary>
		/// Retrieves the first (and presumably global) user verification requirement configuration asynchronously.
		/// </summary>
		/// <returns>
		/// A task representing the asynchronous operation, containing the <see cref="UserVerificationRequirement"/> if found; otherwise, <c>null</c>.
		/// </returns>
		public async Task<UserVerificationRequirement?> GetVerificationRequirement()
		{
			return await _context.UserVerificationRequirements.FirstOrDefaultAsync();
		}
	}
}
