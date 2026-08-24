using CoreMs.Common.Extensions;

namespace CoreMs.CommunicationMs.Core.Configuration;

[Options(Validate = false)]
public class QueueOptions
{
    public bool Enabled { get; set; }
}
