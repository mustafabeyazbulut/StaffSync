using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaffSync.Application.Features.Queries.DirectoryQueries;

namespace StaffSync.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class DirectoryController : ControllerBase
    {
        private readonly IMediator mediator;

        public DirectoryController(IMediator mediator) => this.mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] string format = "vcf")
        {
            var file = await mediator.Send(new GetDirectoryExportQuery { Format = format });
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}
