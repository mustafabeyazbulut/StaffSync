using MediatR;
using StaffSync.Application.Features.Mediator.Queries.ContactQueries;
using StaffSync.Application.Features.Mediator.Results.ContactResults;
using StaffSync.Application.Interfaces;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers
{
    public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, GetContactByIdQueryResult>
    {
        private readonly IRepository<Contact> _repository;

        public GetContactByIdQueryHandler(IRepository<Contact> repository)
        {
            _repository = repository;
        }
        public async Task<GetContactByIdQueryResult> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            var value = await _repository.GetByIdAsync(request.ContactId);
            return new GetContactByIdQueryResult
            {
                ContactId = value.ContactId,
                CoverImageUrl= value.CoverImageUrl,
                FirstName= value.FirstName,
                LastName= value.LastName,
                DisplayName= value.DisplayName,
                Email= value.Email,
                TelephoneNumber= value.TelephoneNumber,
                JobTitle= value.JobTitle,
                Department= value.Department,
                Company= value.Company,
            };
        }
    }
}
