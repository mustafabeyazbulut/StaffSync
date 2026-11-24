using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Queries.CorrectionQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class CorrectionsController : ControllerBase
    {
        private readonly IMediator mediator;

        public CorrectionsController(IMediator mediator) => this.mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] CorrectionStatus? status)
            => Ok(await mediator.Send(new GetCorrectionsQuery { Status = status }));

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateCorrectionStatusCommand command)
        {
            command.Id = id;
            await mediator.Send(command);
            return Ok();
        }
    }
}
