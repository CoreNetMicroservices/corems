using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.CommunicationMs.Api.Controllers;

/// <summary>
/// Client messages (SMS, Email) — stored in database, linked to userId.
/// </summary>
[ApiController]
[Route("api/messages")]
[Authorize]
[Produces("application/json")]
public class MessagesController(
    MessagingService messagingService,
    EmailService emailService,
    SmsService smsService,
    ICurrentUserService currentUserService) : ControllerBase
{
    /// <summary>
    /// Get paginated list of messages. Clients see their own; admins see all.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<MessageResponse>>> GetMessages(
        [FromQuery] QueryParameters parameters, CancellationToken ct)
    {
        Guid? scope = User.IsInRole("COMMUNICATION_MS_ADMIN") || User.IsInRole("SUPER_ADMIN")
            ? null
            : currentUserService.GetCurrentUserUuid();

        var result = await messagingService.ListMessagesAsync(scope, parameters, ct);
        return Ok(result);
    }

    /// <summary>
    /// Send an email message to a user (admin only).
    /// </summary>
    [HttpPost("email")]
    [Authorize(Roles = "COMMUNICATION_MS_ADMIN,SUPER_ADMIN")]
    public async Task<ActionResult<MessageResponse>> SendEmail([FromBody] EmailMessageRequest request, CancellationToken ct)
    {
        var senderUuid = currentUserService.GetCurrentUserUuid();
        var response = await emailService.SendMessageAsync(request, senderUuid, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Send an SMS message to a user (admin only).
    /// </summary>
    [HttpPost("sms")]
    [Authorize(Roles = "COMMUNICATION_MS_ADMIN,SUPER_ADMIN")]
    public async Task<ActionResult<MessageResponse>> SendSms([FromBody] SmsMessageRequest request, CancellationToken ct)
    {
        var senderUuid = currentUserService.GetCurrentUserUuid();
        var response = await smsService.SendMessageAsync(request, senderUuid, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
