using StaffSync.Models;
using System.Web;

namespace StaffSync.Services
{
    public interface IContactsService
    {
        Task<PagedResult<ContactDto>?> GetContactsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? search = null,
            List<string>? departments = null,
            List<string>? companies = null,
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
            int pageSize = 20,
            string? search = null,
            List<string>? departments = null,
            List<string>? companies = null,
            string? sortBy = null)
        {
            var queryParams = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={HttpUtility.UrlEncode(search)}");

            if (departments != null && departments.Count > 0)
            {
                foreach (var dept in departments)
                    queryParams.Add($"departments={HttpUtility.UrlEncode(dept)}");
            }

            if (companies != null && companies.Count > 0)
            {
                foreach (var comp in companies)
                    queryParams.Add($"companies={HttpUtility.UrlEncode(comp)}");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"sortBy={HttpUtility.UrlEncode(sortBy)}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"Contacts?{queryString}";

            return await _apiClient.GetAsync<PagedResult<ContactDto>>(endpoint);
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
