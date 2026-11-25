using StaffSync.Application.Features.Exceptions.AuthExceptions;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Auth;

public class AuthRulesTests
{
    private readonly AuthRules rules = new();

    [Fact]
    public async Task Matching_refresh_token_is_accepted() =>
        await rules.RefreshTokenShouldBeValid(new User { RefreshToken = "abc123" }, "abc123");

    [Theory]
    [InlineData("abc123", "baska")]   // başka/eski bir token
    [InlineData("abc123", "")]        // boş gönderilmiş
    [InlineData("abc123", null)]
    [InlineData(null, "abc123")]      // kullanıcının token'ı iptal edilmiş (revoke)
    public async Task Mismatched_or_missing_refresh_token_is_rejected(string? stored, string? sent) =>
        await Assert.ThrowsAsync<RefreshTokenShouldBeValidException>(() =>
            rules.RefreshTokenShouldBeValid(new User { RefreshToken = stored }, sent));

    [Fact]
    public async Task Unknown_user_is_rejected() =>
        await Assert.ThrowsAsync<RefreshTokenShouldBeValidException>(() =>
            rules.RefreshTokenShouldBeValid(null, "abc123"));
}
