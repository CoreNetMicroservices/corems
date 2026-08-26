using System.Text.Json;
using CoreMs.Common.Http;
using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;
using CoreMs.CommunicationMs.Core.Services;
using CoreMs.CommunicationMs.Core.Services.Providers;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CoreMs.CommunicationMs.Tests.Services;

public class SendMessageConsumerTests
{
    private readonly MessageRepository _repository =
        Substitute.For<MessageRepository>(Substitute.For<DbContext>());

    private readonly ServiceCallContext _serviceCallContext = new();
    private readonly IChannelProvider _emailProvider = Substitute.For<IChannelProvider>();
    private readonly IChannelProvider _smsProvider = Substitute.For<IChannelProvider>();

    public SendMessageConsumerTests()
    {
        _emailProvider.MessageType.Returns(MessageType.Email);
        _smsProvider.MessageType.Returns(MessageType.Sms);
    }

    private SendMessageConsumer CreateConsumer() => new(
        [_emailProvider, _smsProvider],
        _repository,
        _serviceCallContext,
        Substitute.For<ILogger<SendMessageConsumer>>());

    private static ConsumeContext<SendMessageCommand> MockContext(SendMessageCommand command)
    {
        var ctx = Substitute.For<ConsumeContext<SendMessageCommand>>();
        ctx.Message.Returns(command);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    private static SendMessageCommand EmailCommand(Guid messageId, string? actorUserId = null, string? actorRoles = null)
    {
        var payload = new EmailPayloadDto
        {
            Subject = "Hello",
            Recipient = "to@example.com",
            Body = "body"
        };
        return new SendMessageCommand
        {
            MessageId = messageId,
            Type = MessageType.Email,
            PayloadJson = JsonSerializer.Serialize(payload),
            ActorUserId = actorUserId,
            ActorRoles = actorRoles
        };
    }

    private static SendMessageCommand SmsCommand(Guid messageId) => new()
    {
        MessageId = messageId,
        Type = MessageType.Sms,
        PayloadJson = JsonSerializer.Serialize(new SmsPayloadDto { PhoneNumber = "+15551234567", Message = "hi" })
    };

    // ----- Success path -----

    [Fact]
    public async Task Consume_Email_RoutesToEmailProvider()
    {
        var messageId = Guid.NewGuid();
        var entity = new EmailMessageEntity { Uuid = messageId, Status = MessageStatus.Enqueued };
        _repository.GetByUuidAsync(messageId, Arg.Any<CancellationToken>()).Returns(entity);

        await CreateConsumer().Consume(MockContext(EmailCommand(messageId)));

        await _emailProvider.Received(1).SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _smsProvider.DidNotReceive().SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_Sms_RoutesToSmsProvider()
    {
        var messageId = Guid.NewGuid();
        var entity = new SmsMessageEntity { Uuid = messageId, Status = MessageStatus.Enqueued };
        _repository.GetByUuidAsync(messageId, Arg.Any<CancellationToken>()).Returns(entity);

        await CreateConsumer().Consume(MockContext(SmsCommand(messageId)));

        await _smsProvider.Received(1).SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_Success_UpdatesEntityToSent()
    {
        var messageId = Guid.NewGuid();
        var entity = new EmailMessageEntity { Uuid = messageId, Status = MessageStatus.Enqueued };
        _repository.GetByUuidAsync(messageId, Arg.Any<CancellationToken>()).Returns(entity);

        await CreateConsumer().Consume(MockContext(EmailCommand(messageId)));

        entity.Status.Should().Be(MessageStatus.Sent);
        entity.SentAt.Should().NotBeNull();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ----- Failure path -----

    [Fact]
    public async Task Consume_ProviderThrows_UpdatesEntityToFailed_AndRethrows()
    {
        var messageId = Guid.NewGuid();
        var entity = new EmailMessageEntity { Uuid = messageId, Status = MessageStatus.Enqueued };
        _repository.GetByUuidAsync(messageId, Arg.Any<CancellationToken>()).Returns(entity);
        _emailProvider.SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var act = async () => await CreateConsumer().Consume(MockContext(EmailCommand(messageId)));

        await act.Should().ThrowAsync<InvalidOperationException>();
        entity.Status.Should().Be(MessageStatus.Failed);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ----- No entity found -----

    [Fact]
    public async Task Consume_EntityNotFound_StillSendsViaProvider_NoSaveNeeded()
    {
        var messageId = Guid.NewGuid();
        _repository.GetByUuidAsync(messageId, Arg.Any<CancellationToken>()).Returns((MessageEntity?)null);

        await CreateConsumer().Consume(MockContext(EmailCommand(messageId)));

        await _emailProvider.Received(1).SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ----- Actor identity -----

    [Fact]
    public async Task Consume_WithActorIdentity_SetsServiceCallContext()
    {
        var userId = Guid.NewGuid().ToString();
        _repository.GetByUuidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MessageEntity?)null);

        await CreateConsumer().Consume(MockContext(EmailCommand(Guid.NewGuid(), userId, "ADMIN,USER")));

        _serviceCallContext.ActorUserId.Should().Be(userId);
        _serviceCallContext.ActorRoles.Should().BeEquivalentTo(["ADMIN", "USER"]);
    }

    [Fact]
    public async Task Consume_WithoutActorIdentity_LeavesContextEmpty()
    {
        _repository.GetByUuidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MessageEntity?)null);

        await CreateConsumer().Consume(MockContext(EmailCommand(Guid.NewGuid())));

        _serviceCallContext.HasIdentity.Should().BeFalse();
    }

    // ----- Unknown type -----

    [Fact]
    public async Task Consume_UnknownMessageType_ThrowsInvalidOperation()
    {
        var command = new SendMessageCommand
        {
            MessageId = Guid.NewGuid(),
            Type = (MessageType)999,
            PayloadJson = "{}"
        };
        _repository.GetByUuidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MessageEntity?)null);

        var act = async () => await CreateConsumer().Consume(MockContext(command));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
