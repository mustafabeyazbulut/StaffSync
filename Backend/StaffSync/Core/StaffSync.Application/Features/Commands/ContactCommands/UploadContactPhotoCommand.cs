using MediatR;

namespace StaffSync.Application.Features.Commands.ContactCommands
{
    public class UploadContactPhotoCommand : IRequest
    {
        public int ContactId { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
