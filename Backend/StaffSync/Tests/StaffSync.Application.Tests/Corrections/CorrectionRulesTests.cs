using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Exceptions.ContactExceptions;
using StaffSync.Application.Features.Exceptions.CorrectionExceptions;
using StaffSync.Application.Features.Rules.CorrectionRules;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class CorrectionRulesTests
{
    private readonly CorrectionRules rules = new();
    private static readonly Contact C = new() { Id = 1, TelephoneNumber = "111", Department = "Satış" };

    [Fact]
    public async Task Missing_contact_throws_not_found() =>
        await Assert.ThrowsAsync<ContactNotFoundException>(() => rules.ContactMustExist(null));

    [Fact]
    public async Task Same_value_is_rejected() =>
        await Assert.ThrowsAsync<CorrectionValueUnchangedException>(() => rules.ValuesMustChange(C,
            new[] { new CorrectionItemInput { FieldName = "TelephoneNumber", NewValue = " 111 " } }));

    [Fact]
    public async Task Different_value_and_other_are_accepted() =>
        await rules.ValuesMustChange(C, new[]
        {
            new CorrectionItemInput { FieldName = "TelephoneNumber", NewValue = "222" },
            new CorrectionItemInput { FieldName = "Other", NewValue = "Ofisi taşındı" }
        });

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public async Task Hourly_limit_is_five(int countInLastHour, bool shouldThrow)
    {
        Task act() => rules.MustNotExceedHourlyLimit(countInLastHour);
        if (shouldThrow) await Assert.ThrowsAsync<TooManyCorrectionsException>(act);
        else await act();
    }
}
