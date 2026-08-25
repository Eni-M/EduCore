using EduCore.Data;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : class
{
    protected readonly AppDbContext _context = context;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _context.Set<T>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, ct);

    public async Task AddAsync(T entity) => await _context.Set<T>().AddAsync(entity);

    public void Update(T entity) => _context.Set<T>().Update(entity);

    public void Remove(T entity) => _context.Set<T>().Remove(entity);

    public async Task<bool> SaveChangesAsync(CancellationToken ct) =>
        await _context.SaveChangesAsync(ct) > 0;
}
