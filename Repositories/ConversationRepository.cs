using EduCore.Data;
using EduCore.Dtos.Messaging;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class ConversationRepository(AppDbContext context)
    : Repository<Conversation>(context), IConversationRepository
{
    public async Task<Conversation?> GetByParticipantsAsync(
        Guid studentId,
        Guid guardianId,
        Guid teacherId,
        CancellationToken ct)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(
                c =>
                    c.StudentId == studentId &&
                    c.GuardianId == guardianId &&
                    c.TeacherId == teacherId,
                ct);
    }

    public async Task<bool> ExistsAsync(
        Guid studentId,
        Guid guardianId,
        Guid teacherId,
        CancellationToken ct)
    {
        return await _context.Conversations.AnyAsync(
            c =>
                c.StudentId == studentId &&
                c.GuardianId == guardianId &&
                c.TeacherId == teacherId,
            ct);
    }

    public async Task<IEnumerable<ConversationListDto>> GetForGuardianAsync(
        Guid guardianId,
        CancellationToken ct)
    {
        return await (
            from c in _context.Conversations.AsNoTracking()

            join s in _context.Students
                on new { c.StudentId, c.SchoolId }
                equals new { StudentId = s.Id, s.SchoolId }

            join su in _context.Users
                on s.UserId equals su.Id

            join t in _context.Teachers
                on new { c.TeacherId, c.SchoolId }
                equals new { TeacherId = t.Id, t.SchoolId }

            join tu in _context.Users
                on t.UserId equals tu.Id

            where c.GuardianId == guardianId

            orderby c.UpdatedAt descending

            select new ConversationListDto(
                c.Id,
                c.StudentId,
                su.FirstName + " " + su.LastName,
                tu.Id,
                tu.FirstName + " " + tu.LastName,
                c.UpdatedAt,
                // Unread, from the guardian's side, is the teacher's messages they have not
                // opened -- not their own. == tu.Id, not !=.
                _context.Messages.Count(m =>
                    m.ConversationId == c.Id &&
                    m.SenderUserId == tu.Id &&
                    m.ReadAt == null))
        )
        .ToListAsync(ct);
    }

    public async Task<IEnumerable<ConversationListDto>> GetForTeacherAsync(
        Guid teacherId,
        CancellationToken ct)
    {
        // sort in SQL, then project to DTO
        return await (
            from c in _context.Conversations.AsNoTracking()
            join s in _context.Students on new { c.StudentId, c.SchoolId } equals new { StudentId = s.Id, s.SchoolId }
            join su in _context.Users on s.UserId equals su.Id
            join g in _context.Guardians on c.GuardianId equals g.Id
            join gu in _context.Users on g.UserId equals gu.Id
            where c.TeacherId == teacherId
            orderby c.UpdatedAt descending
            select new ConversationListDto(
                c.Id,
                c.StudentId,
                su.FirstName + " " + su.LastName,
                gu.Id,
                gu.FirstName + " " + gu.LastName,
                c.UpdatedAt,
                // Likewise the teacher's unread is the guardian's messages, not the teacher's.
                _context.Messages.Count(m =>
                    m.ConversationId == c.Id &&
                    m.SenderUserId == gu.Id &&
                    m.ReadAt == null))
        )
        .ToListAsync(ct);
    }

    public async Task<ConversationDetailsDto?> GetDetailsAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        var conversation = await (
            from c in _context.Conversations.AsNoTracking()

            join s in _context.Students
                on new { c.StudentId, c.SchoolId }
                equals new { StudentId = s.Id, s.SchoolId }

            join su in _context.Users
                on s.UserId equals su.Id

            join g in _context.Guardians
                on c.GuardianId equals g.Id

            join gu in _context.Users
                on g.UserId equals gu.Id

            join t in _context.Teachers
                on new { c.TeacherId, c.SchoolId }
                equals new { TeacherId = t.Id, t.SchoolId }

            join tu in _context.Users
                on t.UserId equals tu.Id

            where c.Id == conversationId

            select new
            {
                c.Id,
                c.StudentId,
                StudentName = su.FirstName + " " + su.LastName,
                c.GuardianId,
                GuardianUserId = gu.Id,
                GuardianName = gu.FirstName + " " + gu.LastName,
                c.TeacherId,
                TeacherUserId = tu.Id,
                TeacherName = tu.FirstName + " " + tu.LastName,
                c.CreatedAt,
                c.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
            return null;

        var messages = await (
            from m in _context.Messages.AsNoTracking()

            join u in _context.Users
                on m.SenderUserId equals u.Id

            where m.ConversationId == conversationId

            orderby m.SentAt

            select new MessageDto(
                m.Id,
                m.ConversationId,
                m.SenderUserId,
                u.FirstName + " " + u.LastName,
                m.Content,
                m.SentAt,
                m.ReadAt))
            .ToListAsync(ct);

        return new ConversationDetailsDto(
            conversation.Id,
            conversation.StudentId,
            conversation.StudentName,
            conversation.GuardianId,
            conversation.GuardianUserId,
            conversation.GuardianName,
            conversation.TeacherId,
            conversation.TeacherUserId,
            conversation.TeacherName,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            messages);
    }
}