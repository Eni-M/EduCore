using System.Linq.Expressions;
using EduCore.Data;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : class
{
    protected readonly AppDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _dbSet.FindAsync([id], ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct) =>
        await _dbSet.AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

    public async Task AddAsync(T entity) =>
        await _dbSet.AddAsync(entity);

    public void Update(T entity) =>
        _dbSet.Update(entity);

    public async Task<bool> SaveChangesAsync(CancellationToken ct) =>
        await _context.SaveChangesAsync(ct) > 0;
}