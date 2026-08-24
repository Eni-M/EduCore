
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EduCore.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct);
    Task AddAsync(T entity);
    void Update(T entity);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<bool> SaveChangesAsync(CancellationToken ct);
}