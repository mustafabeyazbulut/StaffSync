using MediatR;
using StaffSync.Application.Features.Results.ContactResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactsWithPaginationQuery:IRequest<List<GetContactsWithPaginationQueryResult>>
    {
        public int PageNumber { get; set; }  // Varsayılan sayfa numarası
        public int PageSize { get; set; }    // Varsayılan sayfa boyutu

        // Sabit ID filtresi
        public int? ContactId { get; set; }   // Contact ID'ye göre filtre

        // Dinamik filtreleme alanları
        public string? DisplayName { get; set; }    // İsim filtresi
        public string? Email { get; set; }        // Email filtresi
        public string? Department { get; set; }   // Departman filtresi
        public string? Company { get; set; }      // Şirket filtresi

        public GetContactsWithPaginationQuery(int pageNumber, int pageSize, int? contactId = null,
            string? displayName = null, string? email = null,
            string? department = null, string? company = null)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            ContactId = contactId;
            DisplayName = displayName;
            Email = email;
            Department = department;
            Company = company;
        }
    }
}
