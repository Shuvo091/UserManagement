using System.Linq.Expressions;

namespace CohesionX.UserManagement.Persistence.Interfaces;

public interface IRepository<T> where T : class
{
	Task<T?> GetByIdAsync(Guid id, bool trackChanges = false);
	Task<List<T>> GetAllAsync(bool trackChanges = false);
	Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false);
	Task AddAsync(T entity);
	Task AddRangeAsync(IEnumerable<T> entities);
	void Update(T entity);
	void UpdateRange(IEnumerable<T> entities);
	void Remove(T entity);
	void RemoveRange(IEnumerable<T> entities);
	Task<int> SaveChangesAsync();
}