using MediatR;

namespace StaffSync.Application.Features.Mediator.Commands.ContactCommands
{
    public class CreateContactCommand : IRequest<Unit>
    {
        public string CoverImageUrl { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string TelephoneNumber { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Company { get; set; }
    }
}
