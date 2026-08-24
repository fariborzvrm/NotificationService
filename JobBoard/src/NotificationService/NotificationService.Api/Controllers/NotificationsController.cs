using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Models;
using NotificationService.Domain;

namespace NotificationService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> Search(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NotificationType? type = null,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await notificationService.SearchAsync(userId, type, isRead, pageNumber, pageSize, cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await notificationService.GetUnreadCountAsync(userId, cancellationToken));
    }

    [HttpPost("{id:guid}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return await notificationService.MarkAsReadAsync(id, userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { updated });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        userId = Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
        return userId != Guid.Empty;
    }
}
