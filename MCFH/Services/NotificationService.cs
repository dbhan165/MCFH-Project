using MCFH.DTOs;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public interface INotificationService
{
    Task NotifyAsync(
        int userId,
        string title,
        string? message,
        string type,
        string? relatedType = null,
        int? relatedId = null);

    Task<List<NotificationDto>> GetRecentAsync(int userId, int limit = 30);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkReadAsync(int userId, int notificationId);
    Task MarkAllReadAsync(int userId);
    Task MarkRelatedReadAsync(int userId, string relatedType, int relatedId);
}

public class NotificationService : INotificationService
{
    private readonly McfhDbContext _context;

    public NotificationService(McfhDbContext context)
    {
        _context = context;
    }

    public async Task NotifyAsync(
        int userId,
        string title,
        string? message,
        string type,
        string? relatedType = null,
        int? relatedId = null)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedType = relatedType,
            RelatedId = relatedId,
            IsRead = false,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    public async Task<List<NotificationDto>> GetRecentAsync(int userId, int limit = 30)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                RelatedType = n.RelatedType,
                RelatedId = n.RelatedId,
                IsRead = n.IsRead == true,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(int userId) =>
        _context.Notifications.CountAsync(n => n.UserId == userId && n.IsRead != true);

    public async Task<bool> MarkReadAsync(int userId, int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead != true)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        if (unread.Count > 0)
            await _context.SaveChangesAsync();
    }

    public async Task MarkRelatedReadAsync(int userId, string relatedType, int relatedId)
    {
        var items = await _context.Notifications
            .Where(n => n.UserId == userId &&
                        n.RelatedType == relatedType &&
                        n.RelatedId == relatedId &&
                        n.IsRead != true)
            .ToListAsync();

        foreach (var n in items)
            n.IsRead = true;

        if (items.Count > 0)
            await _context.SaveChangesAsync();
    }
}
