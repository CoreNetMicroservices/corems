using CoreMs.CommunicationMs.Core.Enums;

namespace CoreMs.CommunicationMs.Core.Services.Providers;

public interface IChannelProvider
{
    MessageType MessageType { get; }
    Task SendAsync(object payload, CancellationToken ct = default);
}
