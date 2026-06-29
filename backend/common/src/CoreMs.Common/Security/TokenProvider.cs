using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CoreMs.Common.Security;

public class TokenProvider
{
    public const string TokenTypeAccess = "access_token";
    public const string TokenTypeRefresh = "refresh_token";
    public const string TokenTypeId = "id_token";

    public const string ClaimEmail = "email";
    public const string ClaimFirstName = "first_name";
    public const string ClaimLastName = "last_name";
    public const string ClaimRoles = "roles";
    public const string ClaimTokenId = "token_id";
    public const string ClaimActionType = "action_type";

    private readonly TokenProviderOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly SecurityKey _validationKey;

    public TokenProvider(IOptions<TokenProviderOptions> options)
    {
        _options = options.Value;

        switch (_options.Algorithm)
        {
            case SigningAlgorithm.HS256:
                InitializeHmac(out _signingCredentials, out _validationKey);
                break;
            case SigningAlgorithm.RS256:
                InitializeRsa(out _signingCredentials, out _validationKey);
                break;
            case SigningAlgorithm.ES256:
                InitializeEcdsa(out _signingCredentials, out _validationKey);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported signing algorithm: {_options.Algorithm}");
        }
    }

    private void InitializeHmac(out SigningCredentials signingCredentials, out SecurityKey validationKey)
    {
        if (string.IsNullOrEmpty(_options.SecretKey))
            throw new InvalidOperationException(
                "TokenProvider: SecretKey must not be empty when Algorithm is HS256.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        validationKey = key;
    }

    private void InitializeRsa(out SigningCredentials signingCredentials, out SecurityKey validationKey)
    {
        if (string.IsNullOrEmpty(_options.PrivateKeyBase64) && string.IsNullOrEmpty(_options.PrivateKeyPath))
            throw new InvalidOperationException(
                "TokenProvider: Either PrivateKeyBase64 or PrivateKeyPath must be provided when Algorithm is RS256.");

        if (string.IsNullOrEmpty(_options.PublicKeyBase64) && string.IsNullOrEmpty(_options.PublicKeyPath))
            throw new InvalidOperationException(
                "TokenProvider: Either PublicKeyBase64 or PublicKeyPath must be provided when Algorithm is RS256.");

        var privateRsa = LoadRsaKey(_options.PrivateKeyBase64, _options.PrivateKeyPath);
        var publicRsa = LoadRsaKey(_options.PublicKeyBase64, _options.PublicKeyPath);

        var privateKey = new RsaSecurityKey(privateRsa);
        var publicKey = new RsaSecurityKey(publicRsa);

        signingCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
        validationKey = publicKey;
    }

    private void InitializeEcdsa(out SigningCredentials signingCredentials, out SecurityKey validationKey)
    {
        if (string.IsNullOrEmpty(_options.PrivateKeyBase64) && string.IsNullOrEmpty(_options.PrivateKeyPath))
            throw new InvalidOperationException(
                "TokenProvider: Either PrivateKeyBase64 or PrivateKeyPath must be provided when Algorithm is ES256.");

        if (string.IsNullOrEmpty(_options.PublicKeyBase64) && string.IsNullOrEmpty(_options.PublicKeyPath))
            throw new InvalidOperationException(
                "TokenProvider: Either PublicKeyBase64 or PublicKeyPath must be provided when Algorithm is ES256.");

        var privateEcdsa = LoadEcKey(_options.PrivateKeyBase64, _options.PrivateKeyPath);
        var publicEcdsa = LoadEcKey(_options.PublicKeyBase64, _options.PublicKeyPath);

        var privateKey = new ECDsaSecurityKey(privateEcdsa);
        var publicKey = new ECDsaSecurityKey(publicEcdsa);

        signingCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.EcdsaSha256);
        validationKey = publicKey;
    }

    private static RSA LoadRsaKey(string base64Content, string filePath)
    {
        var pem = !string.IsNullOrEmpty(base64Content)
            ? Encoding.UTF8.GetString(Convert.FromBase64String(base64Content))
            : File.ReadAllText(filePath);

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }

    private static ECDsa LoadEcKey(string base64Content, string filePath)
    {
        var pem = !string.IsNullOrEmpty(base64Content)
            ? Encoding.UTF8.GetString(Convert.FromBase64String(base64Content))
            : File.ReadAllText(filePath);

        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(pem);
        return ecdsa;
    }

    public string CreateAccessToken(string subject, IDictionary<string, object> claims)
        => CreateCustomToken(TokenTypeAccess, subject, claims, _options.AccessTokenExpirationMinutes);

    public string CreateRefreshToken(string subject, IDictionary<string, object> claims)
        => CreateCustomToken(TokenTypeRefresh, subject, claims, _options.RefreshTokenExpirationMinutes);

    public string CreateIdToken(string subject, IDictionary<string, object> claims)
        => CreateCustomToken(TokenTypeId, subject, claims, _options.IdTokenExpirationMinutes);

    public string CreateActionToken(string subject, string actionType, IDictionary<string, object>? claims = null)
    {
        var allClaims = claims is not null
            ? new Dictionary<string, object>(claims)
            : new Dictionary<string, object>();
        allClaims[ClaimActionType] = actionType;

        return CreateCustomToken(actionType, subject, allClaims, _options.ActionTokenExpirationMinutes);
    }

    public string CreateCustomToken(
        string tokenType,
        string subject,
        IDictionary<string, object>? claims,
        int expirationMinutes)
    {
        var now = DateTime.UtcNow;
        var jti = Guid.NewGuid().ToString();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = BuildClaimsIdentity(subject, jti, now, claims),
            Expires = now.AddMinutes(expirationMinutes),
            Issuer = _options.Issuer,
            IssuedAt = now,
            NotBefore = now,
            SigningCredentials = _signingCredentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    private static ClaimsIdentity BuildClaimsIdentity(
        string subject,
        string jti,
        DateTime issuedAt,
        IDictionary<string, object>? claims)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("sub", subject));
        identity.AddClaim(new Claim("jti", jti));
        identity.AddClaim(new Claim("iat",
            new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(),
            ClaimValueTypes.Integer64));

        if (claims is not null)
        {
            foreach (var (key, value) in claims)
            {
                identity.AddClaim(new Claim(key, value?.ToString() ?? ""));
            }
        }

        return identity;
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JsonWebTokenHandler();
        var result = handler.ValidateTokenAsync(token, GetValidationParameters()).GetAwaiter().GetResult();

        if (!result.IsValid)
        {
            if (result.Exception is SecurityTokenExpiredException expiredException)
                throw expiredException;

            throw new SecurityTokenException(
                result.Exception?.Message ?? "Token validation failed.");
        }

        return new ClaimsPrincipal(result.ClaimsIdentity);
    }

    public TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _validationKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    }
}
