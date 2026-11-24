using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactByIdQueryHandler : BaseHandler, IRequestHandler<GetContactByIdQuery, ContactDto>
    {
        private readonly ContactRules contactRules;

        public GetContactByIdQueryHandler(ContactRules contactRules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
        }

        public async Task<ContactDto> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.ContactId && !x.IsDeleted);
            await contactRules.ContactMustExist(contact);
            return contact!.ToDto();
        }
    }
}
