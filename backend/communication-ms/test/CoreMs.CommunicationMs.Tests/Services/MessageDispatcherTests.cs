using System.Security.Claims;
using CoreMs.CommunicationMs.Core.Configuration;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Services;
using CoreMs.CommunicationMs.Core.Services.Providers;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace CoreMs.CommunicationMs.Tests.Services;

public class MessageDispatcherTests
{
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    private MessageDispatcher CreateDispatcher(bool queueEnabled, params IChannelProvider[] providers)
    {
        var options = Options.Create(new QueueOptions { Enabled = queueEnabled });
        return new MessageDispatcher(
            providers,
            _publishEndpoint,
            _httpContextAccessor,
            options,
            Substitute.For<ILogger<MessageDispatcher>>());
    }

    private static IChannelProvider ProviderFor(MessageType type)
    {
        var provider = Substitute.For<IChannelProvider>();
        provider.MessageType.Returns(type);
        return provider;
    }

    [Fact]
    public async Task QueueEnabled_PublishesCommand_AndReturnsEnqueued()
    {
        var dispatcher = CreateDispatcher(queueEnabled: true);
        var messageId = Guid.NewGuid();
        var payload = new SmsPayloadDto { PhoneNumber = "+15551234567", Message = "hi" };

        var status = await dispatcher.DispatchAsync(MessageType.Sms, messageId, payload);

        status.Should().Be(MessageStatus.Enqueued);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<SendMessageCommand>(c => c.MessageId == messageId && c.Type == MessageType.Sms),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueEnabled_CapturesAuthenticatedActorIdentity()
    {
        var userId = Guid.NewGuid().ToString();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId), new Claim("role", "ADMIN"), new Claim("role", "USER")],
            authenticationType: "test"));
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = claims });

        var dispatcher = CreateDispatcher(queueEnabled: true);

        await dispatcher.DispatchAsync(MessageType.Email, Guid.NewGuid(), new EmailPayloadDto());

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<SendMessageCommand>(c => c.ActorUserId == userId && c.ActorRoles == "ADMIN,USER"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueEnabled_Unauthenticated_LeavesActorNull()
    {
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext()); // no authenticated user

        var dispatcher = CreateDispatcher(queueEnabled: true);

        await dispatcher.DispatchAsync(MessageType.Email, Guid.NewGuid(), new EmailPayloadDto());

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<SendMessageCommand>(c => c.ActorUserId == null && c.ActorRoles == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueDisabled_SendsViaMatchingProvider_AndReturnsSent()
    {
        var provider = ProviderFor(MessageType.Sms);
        var dispatcher = CreateDispatcher(queueEnabled: false, provider);
        var payload = new SmsPayloadDto { PhoneNumber = "+15551234567", Message = "hi" };

        var status = await dispatcher.DispatchAsync(MessageType.Sms, Guid.NewGuid(), payload);

        status.Should().Be(MessageStatus.Sent);
        await provider.Received(1).SendAsync(payload, Arg.Any<CancellationToken>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<SendMessageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueDisabled_ProviderThrows_ReturnsFailed()
    {
        var provider = ProviderFor(MessageType.Email);
        provider.SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("smtp down"));

        var dispatcher = CreateDispatcher(queueEnabled: false, provider);

        var status = await dispatcher.DispatchAsync(MessageType.Email, Guid.NewGuid(), new EmailPayloadDto());

        status.Should().Be(MessageStatus.Failed);
    }

    [Fact]
    public async Task QueueDisabled_NoMatchingProvider_Throws()
    {
        var dispatcher = CreateDispatcher(queueEnabled: false, ProviderFor(MessageType.Email));

        var act = async () => await dispatcher.DispatchAsync(MessageType.Slack, Guid.NewGuid(), new SlackNotificationRequest
        {
            Channel = "#c",
            Message = "m"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
