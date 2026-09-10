using EduCore.Dtos.Messaging;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class MessagingService(
    IConversationRepository conversations,
    IMessagingRepository messages,
    IGuardianRepository guardians,
    IStudentRepository students,
    ITeacherRepository teachers,
    IClassSubjectTeacherRepository assignments,
    IEnrollmentRepository enrollments,
    ICurrentUser currentUser) : IMessagingService
{
    public async Task<IEnumerable<ConversationListDto>> GetMyConversationsAsync(
        CancellationToken ct)
    {
        if (currentUser.IsGuardian)
            return await conversations.GetForGuardianAsync(
                currentUser.RequireGuardianId(), ct);

        if (currentUser.IsTeacher)
            return await conversations.GetForTeacherAsync(
                currentUser.RequireTeacherId(), ct);

        throw new ForbiddenException(
            "Only guardians and teachers can access messages.");
    }

    public async Task<ConversationDetailsDto> GetConversationAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        var conversation = await conversations.GetDetailsAsync(
            conversationId, ct)
            ?? throw new NotFoundException(
                "Conversation", conversationId);

        EnsureParticipant(
            conversation.GuardianId,
            conversation.TeacherId);

        return conversation;
    }

    public async Task<ConversationDetailsDto> StartConversationAsync(
        StartConversationDto dto,
        CancellationToken ct)
    {
        if (!currentUser.IsGuardian && !currentUser.IsTeacher)
            throw new ForbiddenException(
                "Only guardians and teachers can start conversations.");

        await ValidateParticipantsAsync(dto, ct);

        var existing = await conversations.GetByParticipantsAsync(
            dto.StudentId,
            dto.GuardianId,
            dto.TeacherId,
            ct);

        if (existing is not null)
            return await GetConversationAsync(existing.Id, ct);

        var schoolId = await GetStudentSchoolIdAsync(
            dto.StudentId, ct);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentId = dto.StudentId,
            GuardianId = dto.GuardianId,
            TeacherId = dto.TeacherId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await conversations.AddAsync(conversation);
        await conversations.SaveChangesAsync(ct);

        return await GetConversationAsync(conversation.Id, ct);
    }

    public async Task<ConversationDetailsDto> SendMessageAsync(
        Guid conversationId,
        SendMessageDto dto,
        CancellationToken ct)
    {
        var content = dto.Content?.Trim();

        if (string.IsNullOrWhiteSpace(content))
            throw new BusinessException("Message cannot be empty.");

        if (content.Length > 4000)
            throw new BusinessException(
                "Message cannot exceed 4000 characters.");

        var conversation = await conversations.GetByIdAsync(
            conversationId, ct)
            ?? throw new NotFoundException(
                "Conversation", conversationId);

        EnsureParticipant(
            conversation.GuardianId,
            conversation.TeacherId);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = currentUser.UserId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        await messages.AddMessageAsync(message);

        conversation.UpdatedAt = DateTime.UtcNow;
        conversations.Update(conversation);

        await conversations.SaveChangesAsync(ct);

        return await GetConversationAsync(conversationId, ct);
    }

    public async Task MarkAsReadAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        var conversation = await conversations.GetByIdAsync(
            conversationId, ct)
            ?? throw new NotFoundException(
                "Conversation", conversationId);

        EnsureParticipant(
            conversation.GuardianId,
            conversation.TeacherId);

        await messages.MarkConversationAsReadAsync(
            conversationId,
            currentUser.UserId,
            ct);
    }

    private void EnsureParticipant(
        Guid guardianId,
        Guid teacherId)
    {
        if (currentUser.IsGuardian)
        {
            if (currentUser.GuardianId != guardianId)
                throw new ForbiddenException(
                    "You do not have access to this conversation.");

            return;
        }

        if (currentUser.IsTeacher)
        {
            if (currentUser.TeacherId != teacherId)
                throw new ForbiddenException(
                    "You do not have access to this conversation.");

            return;
        }

        throw new ForbiddenException(
            "You do not have access to this conversation.");
    }

    private async Task ValidateParticipantsAsync(
        StartConversationDto dto,
        CancellationToken ct)
    {
        if (currentUser.IsGuardian &&
            currentUser.RequireGuardianId() != dto.GuardianId)
        {
            throw new ForbiddenException(
                "You can only start conversations as yourself.");
        }

        if (currentUser.IsTeacher &&
            currentUser.RequireTeacherId() != dto.TeacherId)
        {
            throw new ForbiddenException(
                "You can only start conversations as yourself.");
        }

        var link = await guardians.GetLinkAsync(
            dto.GuardianId,
            dto.StudentId,
            ct);

        if (link is null)
            throw new ForbiddenException(
                "The guardian is not linked to this student.");

        var enrollment = await enrollments.GetActiveForStudentAsync(
            dto.StudentId, ct);

        if (enrollment is null)
            throw new ForbiddenException(
                "The student does not have an active enrollment.");

        if (!await assignments.TeachesClassAsync(
                dto.TeacherId,
                enrollment.ClassId,
                ct))
        {
            throw new ForbiddenException(
                "The teacher does not teach this student's class.");
        }
    }

    private async Task<Guid> GetStudentSchoolIdAsync(
        Guid studentId,
        CancellationToken ct)
    {
        var student = await students.GetByIdAsync(
            studentId, ct)
            ?? throw new NotFoundException(
                "Student", studentId);

        return student.SchoolId;
    }
}