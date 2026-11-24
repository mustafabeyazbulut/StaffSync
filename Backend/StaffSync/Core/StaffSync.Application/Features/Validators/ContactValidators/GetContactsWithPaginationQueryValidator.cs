using FluentValidation;
using StaffSync.Application.Features.Queries.ContactQueries;

namespace StaffSync.Application.Features.Validators.ContactValidators
{
    public class GetContactsWithPaginationQueryValidator : AbstractValidator<GetContactsWithPaginationQuery>
    {
        public GetContactsWithPaginationQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(100);
        }
    }
}
