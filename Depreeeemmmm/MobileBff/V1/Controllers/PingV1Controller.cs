using Depreeeemmmm.MobileBff.V1.Models.Requests;
using Depreeeemmmm.MobileBff.V1.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Depreeeemmmm.MobileBff.V1.Controllers;

[Route("api/v1/ping")]
[ApiController]
public class PingV1Controller : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetPingResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public IActionResult GetPing([FromQuery] GetPingRequest request, CancellationToken cancellationToken)
    {
        GetPingResponse response = new GetPingResponse
        {
            Pong = $"Pong, {request.Pinger}"
        };

        return Ok(response);
    }
}