using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactChangesQueryHandler : BaseHandler, IRequestHandler<GetContactChangesQuery, ContactChangesResult>
    {
        public GetContactChangesQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<ContactChangesResult> Handle(GetContactChangesQuery request, CancellationToken cancellationToken)
        {
            // Sorgudan ÖNCE alınır: sorgu sırasında olan değişiklik bir sonraki senkronda kaçmaz.
            DateTime serverTime = DateTime.UtcNow;
            DateTime? since = request.Since?.ToUniversalTime();
            IQueryable<Contact> contacts = unitOfWork.GetReadRepository<Contact>().AsQueryable();

            return new ContactChangesResult
            {
                ServerTimeUtc = serverTime,
                Upserted = await ContactChanges.Upserted(contacts, since).Select(ContactMappings.AsDto).ToListAsync(cancellationToken),
                DeletedIds = since is null ? new() : await ContactChanges.DeletedIds(contacts, since).ToListAsync(cancellationToken)
            };
        }
    }
}
