// Repository.cs
using CohesionX.UserManagement.Modules.Users.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CohesionX.UserManagement.Shared.Persistence;

public class Repository<T> : IRepository<T> where T : class
{
	protected readonly DbContext _context;
	private readonly DbSet<T> _dbSet;

	public Repository(DbContext context)
	{
		_context = context;
		_dbSet = context.Set<T>();
	}

	public async Task<T?> GetByIdAsync(Guid id, bool trackChanges = false)
		=> await _dbSet.FindAsync(id);

	public async Task<List<T>> GetAllAsync(bool trackChanges = false)
		=> trackChanges ? await _dbSet.ToListAsync() : await _dbSet.AsNoTracking().ToListAsync();

	public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false)
		=> trackChanges ? await _dbSet.Where(predicate).ToListAsync() : await _dbSet.Where(predicate).AsNoTracking().ToListAsync();

	public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

	public async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);

	public void Update(T entity) => _dbSet.Update(entity);

	public void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

	public void Remove(T entity) => _dbSet.Remove(entity);

	public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

	public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}