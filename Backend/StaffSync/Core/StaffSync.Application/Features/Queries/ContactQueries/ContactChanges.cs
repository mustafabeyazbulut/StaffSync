using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public static class ContactChanges
    {
        public static IQueryable<Contact> Upserted(IQueryable<Contact> contacts, DateTime? sinceUtc) =>
            sinceUtc is null
                ? contacts.Where(c => !c.IsDeleted)
                : contacts.Where(c => !c.IsDeleted && c.UpdatedDate > sinceUtc);

        // since yoksa istemci tam liste alır, silinen listesine ihtiyacı yoktur.
        public static IQueryable<int> DeletedIds(IQueryable<Contact> contacts, DateTime? sinceUtc) =>
            sinceUtc is null
                ? Enumerable.Empty<int>().AsQueryable()
                : contacts.Where(c => c.IsDeleted && c.UpdatedDate > sinceUtc).Select(c => c.Id);
    }
}
