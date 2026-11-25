using MediatR;

namespace StaffSync.Application.Features.Commands.ContactCommands
{
    public class RemoveContactPhotoCommand : IRequest
    {
        public RemoveContactPhotoCommand(int contactId) => ContactId = contactId;
        public int ContactId { get; }
    }
}
