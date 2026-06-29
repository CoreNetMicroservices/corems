using System.Text;
using CoreMs.Common.Security;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CoreMs.Common.Tests;

public class TokenProviderPropertyTests
{
    private const string DefaultTestKey = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!";
    private const string DefaultIssuer = "test-issuer";

    private static TokenProvider CreateTestProvider(
        string secretKey = DefaultTestKey,
        int accessExpMin = 10,
        int refreshExpMin = 1440,
        int idExpMin = 60,
        int actionExpMin = 1440)
    {
        var options = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.HS256,
            SecretKey = secretKey,
            Issuer = DefaultIssuer,
            AccessTokenExpirationMinutes = accessExpMin,
            RefreshTokenExpirationMinutes = refreshExpMin,
            IdTokenExpirationMinutes = idExpMin,
            ActionTokenExpirationMinutes = actionExpMin
        });
        return new TokenProvider(options);
    }

    private static Gen<string> SubjectGen =>
        Gen.Choose(3, 20)
            .SelectMany(len =>
                Gen.ArrayOf(
                    Gen.OneOf(
                        Gen.Choose('a', 'z').Select(i => (char)i),
                        Gen.Choose('A', 'Z').Select(i => (char)i),
                        Gen.Choose('0', '9').Select(i => (char)i)),
                    len)
                .Select(chars => new string(chars)));

    private static Gen<Dictionary<string, object>> ClaimsDictGen =>
        Gen.Choose(0, 4).SelectMany(count =>
        {
            var keys = new[] { "email", "role", "scope", "tenant", "org_id" };
            var pairGen = Gen.Choose(0, keys.Length - 1).SelectMany(ki =>
                Gen.Choose(3, 10).SelectMany(vLen =>
                    Gen.ArrayOf(
                        Gen.Choose('a', 'z').Select(i => (char)i),
                        vLen)
                        .Select(chars => new KeyValuePair<string, object>(keys[ki], new string(chars)))));

            return Gen.ArrayOf(pairGen, count)
                .Select(pairs =>
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var pair in pairs)
                        dict[pair.Key] = pair.Value;
                    return dict;
                });
        });

    private static Gen<int> ExpirationMinutesGen =>
        Gen.Choose(1, 10000);

    private static Gen<string> ValidHmacKeyGen =>
        Gen.Choose(32, 64).SelectMany(len =>
            Gen.ArrayOf(Gen.Choose('A', 'z').Select(i => (char)i), len)
                .Select(chars => new string(chars)));

    /// <summary>
    /// Property 1: Token creation round-trip preserves identity.
    /// For any valid subject and claims, CreateCustomToken followed by ValidateToken
    /// returns a ClaimsPrincipal with matching sub claim.
    /// Validates: Requirements 4.4, 7.4, 9.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TokenCreationRoundTrip_PreservesIdentity()
    {
        return Prop.ForAll(
            SubjectGen.ToArbitrary(),
            ClaimsDictGen.ToArbitrary(),
            (subject, claims) =>
            {
                var provider = CreateTestProvider();
                var token = provider.CreateCustomToken("access_token", subject, claims, 10);
                var principal = provider.ValidateToken(token);

                var subClaim = principal.FindFirst("sub")?.Value;
                return (subClaim == subject)
                    .Label($"Expected sub='{subject}', got sub='{subClaim}'");
            });
    }

    /// <summary>
    /// Property 2: All standard claims are present in created tokens.
    /// For any subject and claims dictionary, the resulting JWT contains sub, jti, iat, iss,
    /// and all custom claims.
    /// Validates: Requirements 4.3, 5.3, 6.3, 7.3, 8.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllStandardClaims_ArePresentInCreatedTokens()
    {
        return Prop.ForAll(
            SubjectGen.ToArbitrary(),
            ClaimsDictGen.ToArbitrary(),
            (subject, claims) =>
            {
                var provider = CreateTestProvider();
                var token = provider.CreateCustomToken("access_token", subject, claims, 10);

                var handler = new JsonWebTokenHandler();
                var jwt = handler.ReadJsonWebToken(token);

                var hasSub = jwt.Claims.Any(c => c.Type == "sub" && c.Value == subject);
                var hasJti = jwt.Claims.Any(c => c.Type == "jti" && Guid.TryParse(c.Value, out _));
                var hasIat = jwt.Claims.Any(c => c.Type == "iat");
                var hasIss = jwt.Claims.Any(c => c.Type == "iss" && c.Value == DefaultIssuer);

                var hasAllCustomClaims = claims.All(kvp =>
                    jwt.Claims.Any(c => c.Type == kvp.Key && c.Value == kvp.Value.ToString()));

                return (hasSub && hasJti && hasIat && hasIss && hasAllCustomClaims)
                    .Label($"sub={hasSub}, jti={hasJti}, iat={hasIat}, iss={hasIss}, customClaims={hasAllCustomClaims}");
            });
    }

    /// <summary>
    /// Property 3: Token expiration matches configured minutes.
    /// For any positive expirationMinutes, the token's exp claim equals iat + expirationMinutes * 60
    /// (within 2-second tolerance).
    /// Validates: Requirements 4.2, 5.2, 6.2, 7.2, 8.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TokenExpiration_MatchesConfiguredMinutes()
    {
        return Prop.ForAll(
            ExpirationMinutesGen.ToArbitrary(),
            expirationMinutes =>
            {
                var provider = CreateTestProvider();
                var token = provider.CreateCustomToken("access_token", "testuser", null, expirationMinutes);

                var handler = new JsonWebTokenHandler();
                var jwt = handler.ReadJsonWebToken(token);

                var iat = long.Parse(jwt.Claims.First(c => c.Type == "iat").Value);
                var exp = new DateTimeOffset(jwt.ValidTo).ToUnixTimeSeconds();
                var expectedExp = iat + (long)expirationMinutes * 60;
                var diff = Math.Abs(exp - expectedExp);

                return (diff <= 2)
                    .Label($"exp={exp}, expected={expectedExp}, diff={diff}s (tolerance=2s)");
            });
    }

    /// <summary>
    /// Property 4: Action token includes action_type claim.
    /// For any subject and actionType string, CreateActionToken produces a token with
    /// action_type claim matching the parameter.
    /// Validates: Requirements 7.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ActionToken_IncludesActionTypeClaim()
    {
        var actionTypeGen = Gen.OneOf(
            Gen.Constant("email_verification"),
            Gen.Constant("phone_verification"),
            Gen.Constant("password_reset"),
            Gen.Constant("account_activation"),
            Gen.Constant("mfa_setup"));

        return Prop.ForAll(
            SubjectGen.ToArbitrary(),
            actionTypeGen.ToArbitrary(),
            (subject, actionType) =>
            {
                var provider = CreateTestProvider();
                var token = provider.CreateActionToken(subject, actionType);

                var handler = new JsonWebTokenHandler();
                var jwt = handler.ReadJsonWebToken(token);

                var actionTypeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "action_type")?.Value;
                return (actionTypeClaim == actionType)
                    .Label($"Expected action_type='{actionType}', got='{actionTypeClaim}'");
            });
    }

    /// <summary>
    /// Property 5: HS256 signing with valid key produces verifiable tokens.
    /// For any non-empty key (≥32 bytes UTF-8), tokens created can be validated,
    /// and tampered tokens fail validation.
    /// Validates: Requirements 3.1, 4.4
    /// </summary>
    [Property(MaxTest = 50)]
    public Property HS256Signing_WithValidKey_ProducesVerifiableTokens()
    {
        return Prop.ForAll(
            ValidHmacKeyGen.ToArbitrary(),
            key =>
            {
                var provider = CreateTestProvider(secretKey: key);
                var token = provider.CreateCustomToken("access_token", "testuser", null, 10);

                var principal = provider.ValidateToken(token);
                var validationSucceeded = principal.FindFirst("sub")?.Value == "testuser";

                var tamperedToken = token[..^5] + "XXXXX";
                var tamperDetected = false;
                try
                {
                    provider.ValidateToken(tamperedToken);
                }
                catch (SecurityTokenException)
                {
                    tamperDetected = true;
                }

                return (validationSucceeded && tamperDetected)
                    .Label($"validationSucceeded={validationSucceeded}, tamperDetected={tamperDetected}");
            });
    }
}
