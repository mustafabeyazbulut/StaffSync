using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactByIdQueryHandler : BaseHandler, IRequestHandler<GetContactByIdQuery, GetContactByIdQueryResult>
    {
        public GetContactByIdQueryHandler(IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(mapper, unitOfWork, httpContextAccessor)
        {
        }
        public async Task<GetContactByIdQueryResult> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            var value = await unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.ContactId);
            var map = mapper.Map<GetContactByIdQueryResult, Contact>(value);
            return map;
        }
    }
}
