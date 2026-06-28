using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.CommunicationMs.Api.Controllers;

/// <summary>
/// System notifications (Slack, SMS, Email) — NOT stored in database.
/// Admin-only endpoints for fire-and-forget notifications.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "COMMUNICATION_MS_ADMIN,SUPER_ADMIN")]
[Produces("application/json")]
public class NotificationsController(
    EmailService emailService,
    SmsService smsService,
    SlackService slackService) : ControllerBase
{
    /// <summary>
    /// Send an email notification (not stored in DB).
    /// </summary>
    [HttpPost("email")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<NotificationResponse>> SendEmailNotification(
        [FromBody] EmailNotificationRequest request, CancellationToken ct)
    {
        var response = await emailService.SendNotificationAsync(request, ct);
        return StatusCode(StatusCodes.Status202Accepted, response);
    }

    /// <summary>
    /// Send an SMS notification (not stored in DB).
    /// </summary>
    [HttpPost("sms")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<NotificationResponse>> SendSmsNotification(
        [FromBody] SmsNotificationRequest request, CancellationToken ct)
    {
        var response = await smsService.SendNotificationAsync(request, ct);
        return StatusCode(StatusCodes.Status202Accepted, response);
    }

    /// <summary>
    /// Send a Slack notification (not stored in DB).
    /// </summary>
    [HttpPost("slack")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<NotificationResponse>> SendSlackNotification(
        [FromBody] SlackNotificationRequest request, CancellationToken ct)
    {
        var response = await slackService.SendNotificationAsync(request, ct);
        return StatusCode(StatusCodes.Status202Accepted, response);
    }
}
