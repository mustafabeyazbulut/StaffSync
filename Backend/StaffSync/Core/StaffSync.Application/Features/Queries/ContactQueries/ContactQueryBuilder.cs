using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    // Saf sorgu kuralları: hem EF (SQL'e çevrilir) hem testlerdeki List.AsQueryable() ile çalışır.
    public static class ContactQueryBuilder
    {
        public static IQueryable<Contact> ApplyFilters(this IQueryable<Contact> query, string? search,
            IReadOnlyCollection<string>? departments, string? company)
        {
            query = query.Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                // ?? "" null alanlarda LINQ-to-objects'in patlamasını önler; EF bunu COALESCE'e çevirir.
                query = query.Where(c =>
                    (c.DisplayName ?? "").Contains(s) ||
                    (c.JobTitle ?? "").Contains(s) ||
                    (c.Department ?? "").Contains(s) ||
                    (c.Email ?? "").Contains(s) ||
                    (c.TelephoneNumber ?? "").Contains(s) ||
                    (c.Home ?? "").Contains(s));
            }

            if (departments is { Count: > 0 })
                query = query.Where(c => departments.Contains(c.Department));

            if (!string.IsNullOrWhiteSpace(company))
                query = query.Where(c => c.Company == company);

            return query;
        }

        public static IOrderedQueryable<Contact> ApplySort(this IQueryable<Contact> query, ContactSortBy sortBy) => sortBy switch
        {
            ContactSortBy.Department => query.OrderBy(c => c.Department).ThenBy(c => c.DisplayName).ThenBy(c => c.Id),
            ContactSortBy.JobTitle => query.OrderBy(c => c.JobTitle).ThenBy(c => c.DisplayName).ThenBy(c => c.Id),
            _ => query.OrderBy(c => c.DisplayName).ThenBy(c => c.Id)
        };

        public static IQueryable<T> Page<T>(this IOrderedQueryable<T> query, int pageNumber, int pageSize)
            => query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}
