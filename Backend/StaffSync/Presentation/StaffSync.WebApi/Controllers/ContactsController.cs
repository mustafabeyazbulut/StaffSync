using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Queries.ContactQueries;

namespace StaffSync.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ContactsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ContactList()
        {
            var values = await _mediator.Send(new GetContactQuery());
            return Ok(values);
        }
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetContact(int id)
        {
            var value = await _mediator.Send(new GetContactByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetContactsWithPagination(GetContactsWithPaginationQuery query)
        {
            var value = await _mediator.Send(query);
            return Ok(value);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateContact(CreateContactCommand command)
        {
            await _mediator.Send(command);
            return Ok("Contact added");
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RemoveContact(int id)
        {
            await _mediator.Send(new RemoveContactCommand(id));
            return Ok("Contact removed");
        }
        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateContact(UpdateContactCommand command)
        {
            await _mediator.Send(command);
            return Ok("Contact updated");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FilterOptions()
            => Ok(await _mediator.Send(new GetContactFilterOptionsQuery()));

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Changes([FromQuery] DateTime? since)
            => Ok(await _mediator.Send(new GetContactChangesQuery { Since = since }));

        /// <summary>Kişinin fotoğrafı (mobil uygulama indirip önbelleğe alır).</summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Photo(int id)
        {
            var photo = await _mediator.Send(new GetContactPhotoQuery(id));
            return File(photo.Content, photo.ContentType);
        }

        /// <summary>Kişiye fotoğraf yükler (JPEG/PNG, en fazla 2 MB); varsa eskisinin yerine geçer.</summary>
        [HttpPost("{id}")]
        [Authorize(Roles = "admin")]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
        {
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);
            await _mediator.Send(new UploadContactPhotoCommand { ContactId = id, Content = buffer.ToArray() });
            return Ok("Photo uploaded");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RemovePhoto(int id)
        {
            await _mediator.Send(new RemoveContactPhotoCommand(id));
            return Ok("Photo removed");
        }

        [HttpPost("{id}")]
        [Authorize]
        public async Task<IActionResult> ReportCorrection(int id, CreateContactCorrectionCommand command)
        {
            command.ContactId = id;
            await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created);
        }
    }
}
