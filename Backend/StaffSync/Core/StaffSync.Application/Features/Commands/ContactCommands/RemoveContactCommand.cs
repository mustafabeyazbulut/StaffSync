using MediatR;

namespace StaffSync.Application.Features.Commands.ContactCommands
{
    public class RemoveContactCommand : IRequest
    {
        public int Id { get; set; }

        public RemoveContactCommand(int id)
        {
            Id = id;
        }
    }
}
