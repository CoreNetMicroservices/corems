using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;

namespace CoreMs.CommunicationMs.Core.Services;

[Service]
public class MessagingService
{
    private readonly MessageRepository _messageRepository;

    public MessagingService(MessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<PagedResult<MessageResponse>> ListMessagesAsync(
        Guid? scopeUserId,
        QueryParameters parameters,
        CancellationToken ct = default)
    {
        // Scope to user if not admin
        if (scopeUserId.HasValue)
        {
            parameters.Filters ??= [];
            parameters.Filters.Add($"UserId:eq:{scopeUserId.Value}");
        }

        if (string.IsNullOrEmpty(parameters.Sort))
            parameters.Sort = "CreatedAt:desc";

        var result = await _messageRepository.GetPagedAsync(parameters, ct);

        var items = result.Items.Select(MapToResponse).ToList();
        return new PagedResult<MessageResponse>(items, result.TotalCount, result.Page, result.PageSize);
    }

    private static MessageResponse MapToResponse(MessageEntity entity)
    {
        object? payload = entity switch
        {
            EmailMessageEntity email => new EmailPayloadDto
            {
                EmailType = email.EmailType,
                Subject = email.Subject,
                Recipient = email.Recipient,
                Sender = email.Sender,
                SenderName = email.SenderName,
                Body = email.Body,
                Cc = string.IsNullOrEmpty(email.Cc) ? null : email.Cc.Split(',').Select(s => s.Trim()).ToList(),
                Bcc = string.IsNullOrEmpty(email.Bcc) ? null : email.Bcc.Split(',').Select(s => s.Trim()).ToList()
            },
            SmsMessageEntity sms => new SmsPayloadDto
            {
                PhoneNumber = sms.PhoneNumber,
                Message = sms.Message
            },
            _ => null
        };

        return new MessageResponse
        {
            Uuid = entity.Uuid,
            Type = entity.Type.ToString().ToLowerInvariant(),
            Status = entity.Status.ToString().ToLowerInvariant(),
            UserId = entity.UserId,
            CreatedAt = entity.CreatedAt,
            SentById = entity.SentById,
            SentByType = entity.SentByType?.ToString().ToLowerInvariant(),
            Payload = payload
        };
    }
}
