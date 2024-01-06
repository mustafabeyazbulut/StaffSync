using MediatR;
using StaffSync.Application.Features.Mediator.Queries.ContactQueries;
using StaffSync.Application.Features.Mediator.Results.ContactResults;
using StaffSync.Application.Interfaces;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers
{
    public class GetContactQueryHandler:IRequestHandler<GetContactQuery,List<GetContactQueryResult>>
    {
        private readonly IRepository<Contact> _repository;
        public GetContactQueryHandler(IRepository<Contact> repository)
        {
            _repository = repository;
        }
        public async Task<List<GetContactQueryResult>> Handle(GetContactQuery request, CancellationToken cancellationToken)
        {
            var values = await _repository.GetAllAsync();
            return values.Select(x=> new GetContactQueryResult
            {
                ContactId = x.ContactId,
                CoverImageUrl = x.CoverImageUrl,
                FirstName = x.FirstName,
                LastName = x.LastName,
                DisplayName = x.DisplayName,
                Email = x.Email,
                TelephoneNumber = x.TelephoneNumber,
                JobTitle = x.JobTitle,
                Department = x.Department,
                Company = x.Company,
            }).ToList();
        }
    }
}
