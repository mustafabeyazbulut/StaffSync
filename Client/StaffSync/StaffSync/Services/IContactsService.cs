using StaffSync.Models;
using System.Web;

namespace StaffSync.Services
{
    public interface IContactsService
    {
        Task<PagedResult<ContactDto>?> GetContactsAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            List<string>? departments = null,
            string? company = null,
            string? sortBy = null);

        Task<ContactFilterOptionsResult?> GetFilterOptionsAsync();

        Task<ContactChangesResult?> GetChangesAsync(DateTime? sinceUtc = null);

        Task<bool> ReportCorrectionAsync(int contactId, string? note, List<CorrectionItemInput> items);
    }

    public class ContactsService : IContactsService
    {
        private readonly IApiClient _apiClient;

        public ContactsService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<PagedResult<ContactDto>?> GetContactsAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            List<string>? departments = null,
            string? company = null,
            string? sortBy = null)
        {
            var request = new ContactsPageRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = string.IsNullOrWhiteSpace(search) ? null : search,
                Departments = (departments != null && departments.Count > 0) ? departments : null,
                Company = string.IsNullOrWhiteSpace(company) ? null : company,
                SortBy = string.IsNullOrWhiteSpace(sortBy) ? "Name" : sortBy
            };

            return await _apiClient.PostAsync<ContactsPageRequest, PagedResult<ContactDto>>(
                "Contacts/GetContactsWithPagination",
                request);
        }

        public async Task<ContactFilterOptionsResult?> GetFilterOptionsAsync()
        {
            return await _apiClient.GetAsync<ContactFilterOptionsResult>("Contacts/FilterOptions");
        }

        public async Task<ContactChangesResult?> GetChangesAsync(DateTime? sinceUtc = null)
        {
            var endpoint = "Contacts/Changes";
            if (sinceUtc.HasValue)
            {
                var formatted = HttpUtility.UrlEncode(sinceUtc.Value.ToString("O"));
                endpoint += $"?since={formatted}";
            }

            return await _apiClient.GetAsync<ContactChangesResult>(endpoint);
        }

        public async Task<bool> ReportCorrectionAsync(int contactId, string? note, List<CorrectionItemInput> items)
        {
            var request = new CreateContactCorrectionRequest
            {
                Note = note,
                Items = items
            };

            return await _apiClient.PostAsync($"Contacts/ReportCorrection/{contactId}", request);
        }
    }
}
