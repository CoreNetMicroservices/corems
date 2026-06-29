using System.Security.Claims;
using System.Text;
using CoreMs.Common.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CoreMs.Common.Tests;

public class TokenProviderErrorTests
{
    [Fact]
    public void Constructor_HS256_EmptySecretKey_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.HS256,
            SecretKey = ""
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new TokenProvider(options));
        Assert.Contains("SecretKey", ex.Message);
    }

    [Fact]
    public void Constructor_RS256_NoPrivateKey_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.RS256,
            PrivateKeyPath = "",
            PrivateKeyBase64 = "",
            PublicKeyBase64 = "dummy"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new TokenProvider(options));
        Assert.Contains("PrivateKey", ex.Message);
    }

    [Fact]
    public void Constructor_RS256_NoPublicKey_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.RS256,
            PrivateKeyBase64 = "dummy",
            PublicKeyPath = "",
            PublicKeyBase64 = ""
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new TokenProvider(options));
        Assert.Contains("PublicKey", ex.Message);
    }

    [Fact]
    public void Constructor_ES256_NoPrivateKey_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.ES256,
            PrivateKeyPath = "",
            PrivateKeyBase64 = "",
            PublicKeyBase64 = "dummy"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new TokenProvider(options));
        Assert.Contains("PrivateKey", ex.Message);
    }

    [Fact]
    public void Constructor_ES256_NoPublicKey_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.ES256,
            PrivateKeyBase64 = "dummy",
            PublicKeyPath = "",
            PublicKeyBase64 = ""
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new TokenProvider(options));
        Assert.Contains("PublicKey", ex.Message);
    }

    [Fact]
    public void ValidateToken_InvalidSignature_ThrowsSecurityTokenException()
    {
        var provider1 = CreateHmacProvider("ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!");
        var token = provider1.CreateAccessToken("user1", new Dictionary<string, object> { ["email"] = "test@test.com" });

        var provider2 = CreateHmacProvider("ADifferentSecretKeyThatIsAlsoLongEnough123456!");

        Assert.Throws<SecurityTokenException>(() => provider2.ValidateToken(token));
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ThrowsSecurityTokenExpiredException()
    {
        const string secretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!";
        var provider = CreateHmacProvider(secretKey);

        // Manually create a token that is expired (NotBefore in the past, Expires in the past)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var past = DateTime.UtcNow.AddMinutes(-10);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", "user1")]),
            Issuer = "test-issuer",
            IssuedAt = past,
            NotBefore = past,
            Expires = past.AddMinutes(1), // expired 9 minutes ago
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        Assert.Throws<SecurityTokenExpiredException>(() => provider.ValidateToken(token));
    }

    private static TokenProvider CreateHmacProvider(string secretKey)
    {
        var options = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.HS256,
            SecretKey = secretKey,
            Issuer = "test-issuer"
        });
        return new TokenProvider(options);
    }
}
