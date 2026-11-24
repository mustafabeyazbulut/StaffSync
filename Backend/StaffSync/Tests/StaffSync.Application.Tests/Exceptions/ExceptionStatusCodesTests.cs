using FluentValidation;
using FluentValidation.Results;
using StaffSync.Application.Bases;
using StaffSync.Application.Exceptions;

namespace StaffSync.Application.Tests.Exceptions;

public class ExceptionStatusCodesTests
{
    private class SomeRuleException : BaseException { public SomeRuleException() : base("kural") { } }
    private class SomeNotFoundException : NotFoundException { public SomeNotFoundException() : base("yok") { } }

    [Fact]
    public void Business_rule_exception_is_400() =>
        Assert.Equal(400, ExceptionStatusCodes.For(new SomeRuleException()));

    [Fact]
    public void Not_found_exception_is_404() =>
        Assert.Equal(404, ExceptionStatusCodes.For(new SomeNotFoundException()));

    [Fact]
    public void Validation_exception_is_422() =>
        Assert.Equal(422, ExceptionStatusCodes.For(new ValidationException(new[] { new ValidationFailure("A", "hata") })));

    [Fact]
    public void Unknown_exception_is_500() =>
        Assert.Equal(500, ExceptionStatusCodes.For(new InvalidOperationException()));
}
