using StaffSync.Application.Features.Exceptions.AuthExceptions;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Application.Features.Sessions;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Auth;

public class AuthRulesTests
{
    private readonly AuthRules rules = new();

    private static UserSession SessionWith(string? token, int validDays = 60)
    {
        var session = new UserSession { DeviceId = "cihaz-1" };
        if (token != null) SessionTokens.Renew(session, token, validDays);
        return session;
    }

    [Fact]
    public async Task Matching_refresh_token_is_accepted() =>
        await rules.RefreshTokenShouldBeValid(SessionWith("abc123"), "abc123");

    [Theory]
    [InlineData("abc123", "baska")]   // başka/eski bir token
    [InlineData("abc123", "")]        // boş gönderilmiş
    [InlineData("abc123", null)]
    [InlineData(null, "abc123")]      // oturumun anahtarı yok
    public async Task Mismatched_or_missing_refresh_token_is_rejected(string? stored, string? sent) =>
        await Assert.ThrowsAsync<RefreshTokenShouldBeValidException>(() =>
            rules.RefreshTokenShouldBeValid(SessionWith(stored), sent));

    [Fact]
    public async Task Missing_session_is_rejected() =>
        // Bu cihaz için oturum yok (hiç giriş yapılmamış ya da oturum kapatılmış)
        await Assert.ThrowsAsync<RefreshTokenShouldBeValidException>(() =>
            rules.RefreshTokenShouldBeValid(null, "abc123"));

    [Fact]
    public async Task Expired_session_is_rejected() =>
        await Assert.ThrowsAsync<RefreshTokenShouldNotBeExpiredException>(() =>
            rules.RefreshTokenShouldNotBeExpired(SessionWith("abc123", validDays: -1)));

    [Fact]
    public async Task Valid_session_is_not_expired() =>
        await rules.RefreshTokenShouldNotBeExpired(SessionWith("abc123"));
}

public class SessionTokensTests
{
    [Fact]
    public void Refresh_token_is_stored_as_hash_not_plain_text()
    {
        var session = new UserSession();
        SessionTokens.Renew(session, "gizli-anahtar", 60);

        Assert.NotEqual("gizli-anahtar", session.RefreshTokenHash);
        Assert.Equal(64, session.RefreshTokenHash.Length);
        Assert.Equal(SessionTokens.Hash("gizli-anahtar"), session.RefreshTokenHash);
    }

    [Fact]
    public void Renew_restarts_expiry_from_now()
    {
        // Kayan süre: her yenilemede süre bugünden itibaren yeniden başlar
        var session = new UserSession { ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        SessionTokens.Renew(session, "yeni", 60);

        Assert.InRange(session.ExpiresAtUtc, DateTime.UtcNow.AddDays(59.9), DateTime.UtcNow.AddDays(60.1));
    }

    [Theory]
    [InlineData(null, "default")]
    [InlineData("", "default")]
    [InlineData("   ", "default")]
    [InlineData(" abc ", "abc")]
    public void Device_id_is_normalized(string? sent, string expected) =>
        Assert.Equal(expected, SessionTokens.NormalizeDeviceId(sent));
}
