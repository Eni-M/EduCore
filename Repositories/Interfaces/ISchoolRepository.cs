using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISchoolRepository : IRepository<School>
{
    // activeOnly is what a picker offering "create something in this school" asks for. The
    // management screen and every historical report ask for all of them: a switched-off school
    // keeps its past, and hiding it from statistics would make that past unreachable.
    Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct);
}
