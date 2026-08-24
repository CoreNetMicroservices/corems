namespace CoreMs.UserMs.Api.Configuration;

using System.ComponentModel.DataAnnotations;
using CoreMs.Common.Extensions;

[Options]
public class RabbitMqOptions
{
    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    [Required]
    public string Username { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";

    [Required]
    public string VirtualHost { get; set; } = "/";
}
