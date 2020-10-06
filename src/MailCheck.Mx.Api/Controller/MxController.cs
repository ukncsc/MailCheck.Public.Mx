using System.Threading.Tasks;
using MailCheck.Common.Api.Authorisation.Filter;
using MailCheck.Common.Api.Authorisation.Service.Domain;
using MailCheck.Common.Api.Domain;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Api.Service;
using MailCheck.Mx.Api.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.Api.Controller
{
    [Route("api/mx")]
    public class MxController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IMxService _mxService;
        private readonly ILogger<MxController> _log;

        public MxController(IMxService mxService, ILogger<MxController> log)
        {
            _log = log;
            _mxService = mxService;
        }

        [HttpGet]
        [Route("domain/{domain}/tls")]
        [MailCheckAuthoriseRole(Role.Standard)]
        public async Task<IActionResult> GetDomainTlsEvaluatorResults(DomainRequest domainRequest)
        {
            if (!ModelState.IsValid)
            {
                _log.LogWarning($"Bad request: {ModelState.GetErrorString()}");
                return BadRequest(new ErrorResponse(ModelState.GetErrorString()));
            }

            DomainTlsEvaluatorResults result = await _mxService.GetDomainTlsEvaluatorResults(domainRequest.Domain);

            return result == null 
                ? NotFound(new ErrorResponse($"Domain {domainRequest.Domain} does not exist in Mail Check.")) 
                : new ObjectResult(result);
        }
    }
}
