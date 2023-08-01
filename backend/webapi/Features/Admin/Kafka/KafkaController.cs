namespace Pidp.Features.Admin.Kafka;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.Admin.Kafka.Models;
using Pidp.Features.Admin.Kafka.Topics;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;

[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminAuthentication)]
public class KafkaController : PidpControllerBase
{
    public KafkaController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpGet("topics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TopicModel>>> GetTopics([FromServices] IQueryHandler<TopicQuery, List<TopicModel>> handler,
                                                                   [FromQuery] TopicQuery query) => await handler.HandleAsync(query);

}
