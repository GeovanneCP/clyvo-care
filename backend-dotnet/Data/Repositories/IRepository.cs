using System.Linq.Expressions;
using ClyvoCare.API.DTOs;

namespace ClyvoCare.API.Data.Repositories;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(PaginationFilter filter, Expression<Func<T, bool>>? predicate = null);
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}