using Asp.Versioning;
using MailKit;
using MailKitSimplified.Receiver.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MimeKit;

namespace WebApiExample.Controllers
{
    //[Authorize]
    [ApiVersion("1.0")]
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class EmailController : ControllerBase
    {
        private readonly ILogger<EmailController> _logger;
        private readonly IMailReader _mailReader;

        public EmailController(IMailReader mailReader, ILogger<EmailController> logger)
        {
            _logger = logger;
            _mailReader = mailReader;
        }

        /// <summary>
        /// Get MIME message detail from the server for the given unique ID.
        /// </summary>
        /// <param name="uniqueId">Unique ID to look up</param>
        /// <param name="cancellationToken">Automatically bound cancellation token</param>
        /// <returns><see cref="MimeMessage"/> MIME message details</returns>
        /// <remarks>Sample request:  
        ///     GET /email/123456  
        ///     ```curl --header accept: application/json -u username:password http://localhost:80/api/v1/email/123456```
        /// </remarks>
        [HttpGet("{uniqueId}", Name = nameof(GetMimeMessageAsync))]
        [ProducesResponseType(typeof(MimeMessage), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MimeMessage>> GetMimeMessageAsync([FromRoute] MailKit.UniqueId uniqueId, CancellationToken cancellationToken = default)
        {
            var ids = await _mailReader.Items(MessageSummaryItems.UniqueId)
                .GetMessageSummariesAsync().ConfigureAwait(false);
            var matches = ids.Where(m => m.UniqueId == uniqueId);
            var skip = matches.Min(m => m.Index);
            var take = matches.Max(m => m.Index);
            var result = await _mailReader.Skip(skip).Take(take).GetMimeMessagesAsync(cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return BadRequest($"Unique ID parameter ({uniqueId}) not found.");
            }
            //var mappedResult = result.ToDto();
            return Ok(result);
        }

        /// <summary>
        /// Exception handling middleware redirects here,
        /// <seealso href="https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-6.0#exception-handler-1"/>
        /// </summary>
        [Route("/error")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult HandleError() => Problem();
    }
}