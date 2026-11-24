using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactFilterOptionsQueryHandler : BaseHandler, IRequestHandler<GetContactFilterOptionsQuery, ContactFilterOptionsResult>
    {
        public GetContactFilterOptionsQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<ContactFilterOptionsResult> Handle(GetContactFilterOptionsQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Contact> contacts = unitOfWork.GetReadRepository<Contact>().AsQueryable();
            return new ContactFilterOptionsResult
            {
                Departments = await ContactFilterOptions.Departments(contacts).ToListAsync(cancellationToken),
                Companies = await ContactFilterOptions.Companies(contacts).ToListAsync(cancellationToken)
            };
        }
    }
}
