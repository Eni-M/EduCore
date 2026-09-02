using EduCore.Data;

namespace EduCore.Models;

// A post on the school's notice/news board. School-wide by nature: everyone in the school sees
// it. AuthorUserId is a plain column, not a navigation -- the author's name is resolved by a join
// when listing, and a notice must outlive the account that wrote it.
public class Notice : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool Pinned { get; set; }
    public Guid AuthorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
