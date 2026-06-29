using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Security;

public static class TokenProviderExtensions
{
    public static IServiceCollection AddCoreMsTokenProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TokenProviderOptions>(
            configuration.GetSection(TokenProviderOptions.SectionName));

        services.AddSingleton<TokenProvider>();

        return services;
    }
}
