// <copyright file="Repository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CohesionX.UserManagement.Database.Repositories;

/// <summary>
/// Generic repository implementation providing basic CRUD operations for entities of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class Repository<T> : IRepository<T>
    where T : class
{
    /// <summary>
    /// The Entity Framework database context.
    /// </summary>
    private readonly DbContext context;

    /// <summary>
    /// The <see cref="DbSet{T}"/> representing the collection of entities in the context.
    /// </summary>
    private readonly DbSet<T> dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{T}"/> class with the specified database context.
    /// </summary>
    /// <param name="context">The Entity Framework database context.</param>
    public Repository(DbContext context)
    {
        this.context = context;
        this.dbSet = context.Set<T>();
    }

    /// <summary>
    /// Retrieves an entity by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="trackChanges">Indicates whether to track changes on the retrieved entity. (Note: <see cref="FindAsync"/> does not support tracking toggle.)</param>
    /// <returns>A task representing the asynchronous operation, containing the entity if found; otherwise, <c>null</c>.</returns>
    public async Task<T?> GetByIdAsync(Guid id, bool trackChanges = false)
        => await this.dbSet.FindAsync(id);

    /// <summary>
    /// Retrieves all entities asynchronously.
    /// </summary>
    /// <param name="trackChanges">If <c>true</c>, entities are tracked; otherwise, query uses no-tracking for better performance.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of all entities.</returns>
    public async Task<List<T>> GetAllAsync(bool trackChanges = false)
        => trackChanges ? await this.dbSet.ToListAsync() : await this.dbSet.AsNoTracking().ToListAsync();

    /// <summary>
    /// Finds entities matching the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="trackChanges">If <c>true</c>, entities are tracked; otherwise, query uses no-tracking.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of matching entities.</returns>
    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false)
        => trackChanges ? await this.dbSet.Where(predicate).ToListAsync() : await this.dbSet.Where(predicate).AsNoTracking().ToListAsync();

    /// <summary>
    /// Adds a new entity asynchronously to the context.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>A task representing the asynchronous add operation.</returns>
    public async Task AddAsync(T entity) => await this.dbSet.AddAsync(entity);

    /// <summary>
    /// Adds a range of entities asynchronously to the context.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    /// <returns>A task representing the asynchronous add operation.</returns>
    public async Task AddRangeAsync(IEnumerable<T> entities) => await this.dbSet.AddRangeAsync(entities);

    /// <summary>
    /// Updates an existing entity in the context.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public void Update(T entity) => this.dbSet.Update(entity);

    /// <summary>
    /// Updates a range of existing entities in the context.
    /// </summary>
    /// <param name="entities">The collection of entities to update.</param>
    public void UpdateRange(IEnumerable<T> entities) => this.dbSet.UpdateRange(entities);

    /// <summary>
    /// Removes an entity from the context.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(T entity) => this.dbSet.Remove(entity);

    /// <summary>
    /// Removes a range of entities from the context.
    /// </summary>
    /// <param name="entities">The collection of entities to remove.</param>
    public void RemoveRange(IEnumerable<T> entities) => this.dbSet.RemoveRange(entities);

    /// <summary>
    /// Saves all changes made in this context to the database asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation, returning the number of state entries written to the database.</returns>
    public async Task<int> SaveChangesAsync() => await this.context.SaveChangesAsync();
}
