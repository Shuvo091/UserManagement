using System.Linq.Expressions;

namespace CohesionX.UserManagement.Persistence.Interfaces;

/// <summary>
/// Generic repository interface defining common data access methods for entities of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : class
{
	/// <summary>
	/// Retrieves an entity by its unique identifier.
	/// </summary>
	/// <param name="id">The unique identifier of the entity.</param>
	/// <param name="trackChanges">
	/// Indicates whether to track changes on the retrieved entity.
	/// Defaults to <c>false</c>.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation, containing the entity if found; otherwise, <c>null</c>.
	/// </returns>
	Task<T?> GetByIdAsync(Guid id, bool trackChanges = false);

	/// <summary>
	/// Retrieves all entities.
	/// </summary>
	/// <param name="trackChanges">
	/// Indicates whether to track changes on the retrieved entities.
	/// Defaults to <c>false</c>.
	/// </param>
	/// <returns>A task representing the asynchronous operation, containing a list of entities.</returns>
	Task<List<T>> GetAllAsync(bool trackChanges = false);

	/// <summary>
	/// Finds entities that match the specified predicate.
	/// </summary>
	/// <param name="predicate">The expression to filter entities.</param>
	/// <param name="trackChanges">
	/// Indicates whether to track changes on the retrieved entities.
	/// Defaults to <c>false</c>.
	/// </param>
	/// <returns>A task representing the asynchronous operation, containing a list of matching entities.</returns>
	Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false);

	/// <summary>
	/// Adds a new entity asynchronously.
	/// </summary>
	/// <param name="entity">The entity to add.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task AddAsync(T entity);

	/// <summary>
	/// Adds a range of entities asynchronously.
	/// </summary>
	/// <param name="entities">The collection of entities to add.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task AddRangeAsync(IEnumerable<T> entities);

	/// <summary>
	/// Updates an existing entity.
	/// </summary>
	/// <param name="entity">The entity to update.</param>
	void Update(T entity);

	/// <summary>
	/// Updates a range of entities.
	/// </summary>
	/// <param name="entities">The collection of entities to update.</param>
	void UpdateRange(IEnumerable<T> entities);

	/// <summary>
	/// Removes an entity.
	/// </summary>
	/// <param name="entity">The entity to remove.</param>
	void Remove(T entity);

	/// <summary>
	/// Removes a range of entities.
	/// </summary>
	/// <param name="entities">The collection of entities to remove.</param>
	void RemoveRange(IEnumerable<T> entities);

	/// <summary>
	/// Saves all pending changes to the data store asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous operation, returning the number of state entries written to the data store.</returns>
	Task<int> SaveChangesAsync();
}
