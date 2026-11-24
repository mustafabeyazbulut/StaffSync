using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public static class ContactFilterOptions
    {
        public static IQueryable<FilterOptionDto> Departments(IQueryable<Contact> contacts) =>
            contacts.Where(c => !c.IsDeleted && c.Department != null && c.Department != "")
                .GroupBy(c => c.Department)
                .Select(g => new FilterOptionDto { Name = g.Key, Count = g.Count() })
                .OrderBy(x => x.Name);

        public static IQueryable<FilterOptionDto> Companies(IQueryable<Contact> contacts) =>
            contacts.Where(c => !c.IsDeleted && c.Company != null && c.Company != "")
                .GroupBy(c => c.Company)
                .Select(g => new FilterOptionDto { Name = g.Key, Count = g.Count() })
                .OrderBy(x => x.Name);
    }
}
