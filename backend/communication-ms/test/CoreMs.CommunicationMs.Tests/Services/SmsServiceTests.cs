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

public class SmsServiceTests
{
    private readonly MessageRepository _repository =
        Substitute.For<MessageRepository>(Substitute.For<DbContext>());

    private static MessageDispatcher DirectDispatcher(out IChannelProvider provider)
    {
        provider = Substitute.For<IChannelProvider>();
        provider.MessageType.Returns(MessageType.Sms);
        return new MessageDispatcher(
            [provider],
            Substitute.For<IPublishEndpoint>(),
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new QueueOptions { Enabled = false }),
            Substitute.For<ILogger<MessageDispatcher>>());
    }

    private static TemplateMsClient TemplateClientReturning(string rendered) =>
        new(new HttpClient(new StubHandler(rendered)) { BaseAddress = new Uri("http://template") });

    private SmsService CreateService(MessageDispatcher dispatcher, TemplateMsClient templateClient) =>
        new(_repository, dispatcher, templateClient, Substitute.For<ILogger<SmsService>>());

    [Fact]
    public async Task SendMessage_WithExplicitMessage_UsesItVerbatim_AndSaves()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher, TemplateClientReturning("SHOULD NOT BE USED"));

        var request = new SmsMessageRequest
        {
            UserId = Guid.NewGuid(),
            PhoneNumber = "+15551234567",
            Message = "Direct message"
        };

        var response = await service.SendMessageAsync(request, senderUserId: null);

        response.Type.Should().Be("sms");
        response.Status.Should().Be("sent");
        ((SmsPayloadDto)response.Payload!).Message.Should().Be("Direct message");
        _repository.Received(1).Add(Arg.Any<SmsMessageEntity>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessage_WithTemplateOnly_ResolvesBodyFromTemplateClient()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher, TemplateClientReturning("Rendered OTP 123456"));

        var request = new SmsMessageRequest
        {
            UserId = Guid.NewGuid(),
            PhoneNumber = "+15551234567",
            Template = new TemplateRequest { TemplateId = "otp" }
        };

        var response = await service.SendMessageAsync(request, senderUserId: null);

        ((SmsPayloadDto)response.Payload!).Message.Should().Be("Rendered OTP 123456");
    }

    [Fact]
    public async Task SendMessage_SenderUserId_MarksSentByUser()
    {
        var dispatcher = DirectDispatcher(out _);
        var service = CreateService(dispatcher, TemplateClientReturning(""));
        var sender = Guid.NewGuid();

        var request = new SmsMessageRequest
        {
            UserId = Guid.NewGuid(),
            PhoneNumber = "+15551234567",
            Message = "Hello"
        };

        var response = await service.SendMessageAsync(request, senderUserId: sender);

        response.SentById.Should().Be(sender);
        response.SentByType.Should().Be("user");
    }

    [Fact]
    public async Task SendNotification_ProviderThrows_StatusIsFailed()
    {
        var dispatcher = DirectDispatcher(out var provider);
        provider.SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("carrier down"));
        var service = CreateService(dispatcher, TemplateClientReturning(""));

        var response = await service.SendNotificationAsync(new SmsNotificationRequest
        {
            PhoneNumber = "+15551234567",
            Message = "ping"
        });

        response.Status.Should().Be(MessageStatus.Failed.ToString());
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
