using System.Net;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.CommunicationMs.Core.Configuration;
using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;
using CoreMs.CommunicationMs.Core.Services;
using CoreMs.CommunicationMs.Core.Services.Providers;
using CoreMs.TemplateMs.Client;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace CoreMs.CommunicationMs.Tests.Services;

public class EmailServiceTests
{
    private readonly MessageRepository _repository =
        Substitute.For<MessageRepository>(Substitute.For<DbContext>());

    private const string DefaultFrom = "noreply@test.local";

    private static MessageDispatcher DirectDispatcher(out IChannelProvider provider)
    {
        provider = Substitute.For<IChannelProvider>();
        provider.MessageType.Returns(MessageType.Email);
        return new MessageDispatcher(
            [provider],
            Substitute.For<IPublishEndpoint>(),
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new QueueOptions { Enabled = false }),
            Substitute.For<ILogger<MessageDispatcher>>());
    }

    private EmailService CreateService(MessageDispatcher dispatcher, string renderedContent = "rendered")
    {
        var client = new TemplateMsClient(
            new HttpClient(new StubHandler(renderedContent)) { BaseAddress = new Uri("http://template") });
        var mailOptions = Options.Create(new EmailProviderOptions { DefaultFrom = DefaultFrom });
        return new EmailService(_repository, dispatcher, client, mailOptions, Substitute.For<ILogger<EmailService>>());
    }

    // ----- Body resolution -----

    [Fact]
    public async Task SendMessage_ExplicitBody_UsesItVerbatim()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        var response = await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = "Direct body"
        }, senderUserId: null);

        ((EmailPayloadDto)response.Payload!).Body.Should().Be("Direct body");
    }

    [Fact]
    public async Task SendMessage_TemplateOnly_ResolvesViaClient_AndSetsTypeHtml()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher, "Rendered HTML");

        var response = await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Welcome",
            Recipient = "to@example.com",
            Template = new TemplateRequest { TemplateId = "welcome-email" }
        }, senderUserId: null);

        var payload = (EmailPayloadDto)response.Payload!;
        payload.Body.Should().Be("Rendered HTML");
        payload.EmailType.Should().Be("html");
    }

    [Fact]
    public async Task SendMessage_NeitherBodyNorTemplate_ThrowsServiceException()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        var act = async () => await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = null,
            Template = null
        }, senderUserId: null);

        await act.Should().ThrowAsync<ServiceException>();
    }

    // ----- Sender fallback -----

    [Fact]
    public async Task SendMessage_NoSender_FallsBackToDefaultFrom()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        var response = await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = "body",
            Sender = null
        }, senderUserId: null);

        ((EmailPayloadDto)response.Payload!).Sender.Should().Be(DefaultFrom);
    }

    [Fact]
    public async Task SendMessage_ExplicitSender_OverridesDefault()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        var response = await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = "body",
            Sender = "custom@app.com"
        }, senderUserId: null);

        ((EmailPayloadDto)response.Payload!).Sender.Should().Be("custom@app.com");
    }

    // ----- SentBy logic -----

    [Fact]
    public async Task SendMessage_WithSenderUserId_MarksSentByUser()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);
        var sender = Guid.NewGuid();

        var response = await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = "body"
        }, senderUserId: sender);

        response.SentById.Should().Be(sender);
        response.SentByType.Should().Be("user");
    }

    [Fact]
    public async Task SendMessage_NullSenderUserId_MarksSentBySystem()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        var response = await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = "body"
        }, senderUserId: null);

        response.SentByType.Should().Be("system");
    }

    // ----- Persistence -----

    [Fact]
    public async Task SendMessage_PersistsEntityAndCallsSaveChanges()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = "body"
        }, senderUserId: null);

        _repository.Received(1).Add(Arg.Any<EmailMessageEntity>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ----- CC/BCC payload -----

    [Fact]
    public async Task SendMessage_CcAndBcc_IncludedInPayload()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        var response = await service.SendMessageAsync(new EmailMessageRequest
        {
            UserId = Guid.NewGuid(),
            Subject = "Hi",
            Recipient = "to@example.com",
            Body = "body",
            Cc = ["cc1@test.com", "cc2@test.com"],
            Bcc = ["bcc@test.com"]
        }, senderUserId: null);

        var payload = (EmailPayloadDto)response.Payload!;
        payload.Cc.Should().BeEquivalentTo(["cc1@test.com", "cc2@test.com"]);
        payload.Bcc.Should().BeEquivalentTo(["bcc@test.com"]);
    }

    // ----- Notification (lighter path, no persistence) -----

    [Fact]
    public async Task SendNotification_ValidBody_ReturnsSentStatus()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher);

        var response = await service.SendNotificationAsync(new EmailNotificationRequest
        {
            Subject = "Alert",
            Recipient = "admin@example.com",
            Body = "Something happened"
        });

        response.Status.Should().Be("Sent");
    }

    private sealed class StubHandler(string renderedContent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = $"{{\"renderedContent\":\"{renderedContent}\"}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
