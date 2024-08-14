using MediatR;

namespace StaffSync.Application.Features.Mediator.Commands
{
    public class RemoveContactCommand:IRequest
    {
        public int Id { get; set; }

        public RemoveContactCommand(int id)
        {
            Id = id;
        }
    }
}
