using EddnLookup.DTO;
using EddnLookup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EddnLookup.Controllers
{
    /// <inheritdoc/>
    [Route("")]
    [ApiController]
    public class HomeController(APIService service) : ControllerBase
    {
        private readonly APIService Service = service;

        /// <summary>
        /// Site entry point
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        public ActionResult Index()
        {
            return Redirect("~/swagger");
        }

        /// <summary>Lookup systems</summary>
        /// <remarks>
        /// Returns systems matching all of the given parameters.
        /// 
        /// At least one of the following parameters must be provided:
        /// * `systemName`
        /// * `systemAddress`
        /// 
        /// The following parameters filter systems:
        /// * `systemName`
        /// * `systemAddress`
        /// * `includeRejected`
        /// 
        /// The following parameters filter matches:
        /// * `brief`
        /// * `limitMatches`
        /// * `minDate`
        /// * `maxDate`
        /// </remarks>
        /// <param name="systemName">Name of system to search for</param>
        /// <param name="systemAddress">System Address (id64) of system to search for</param>
        /// <param name="includeRejected">Set to include items marked as rejected</param>
        /// <param name="brief">Set to only return system and body information without matches</param>
        /// <param name="limitMatches">Limit number of matches returned</param>
        /// <param name="minDate">Start of date range for matches</param>
        /// <param name="maxDate">End of date range for matches</param>
        /// <returns>Matched system entries</returns>
        [HttpGet("systems")]
        [ProducesResponseType<List<SystemData>>(StatusCodes.Status200OK)]
        public ActionResult<List<SystemData>> GetSystems(
            [FromQuery] string? systemName,
            [FromQuery] long? systemAddress,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = null,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null)
        {
            systemAddress ??= long.TryParse(Request.Query["systemId64"], out var systemId64) ? systemId64 : null;

            return Ok(Service.GetSystems(systemName, systemAddress, includeRejected, brief, limitMatches, minDate, maxDate));
        }

        /// <summary>Lookup bodies</summary>
        /// <remarks>
        /// Returns bodies matching all of the given parameters
        /// 
        /// At least one of the following combinations of parameters must be provided:
        /// * `bodyName`
        /// * `bodyId` and `systemName`
        /// * `bodyId` and `systemAddress`
        /// 
        /// The following parameters filter bodies:
        /// * `bodyName`
        /// * `systemName`
        /// * `systemAddress`
        /// * `bodyId`
        /// * `includeRejected`
        /// 
        /// The following parameters filter matches:
        /// * `brief`
        /// * `limitMatches`
        /// * `minDate`
        /// * `maxDate`
        /// </remarks>
        /// <param name="bodyName">Name of the body to search for</param>
        /// <param name="systemName">Used with `bodyId`; Name of the system to search for the body</param>
        /// <param name="systemAddress">Used with `bodyId`; System Address (id64) of the system to search for the body</param>
        /// <param name="bodyId">Used with `systemName` or `systemAddress`; Body ID of the body to search for</param>
        /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
        /// <param name="brief">Set brief to only return body and system information without matches</param>
        /// <param name="limitMatches">Limit number of matches returned</param>
        /// <param name="minDate">Start of date range for matches</param>
        /// <param name="maxDate">End of date range for matches</param>
        /// <returns>Matched body entries</returns>
        [HttpGet("bodies")]
        [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
        public ActionResult<List<BodyData>> GetBodies(
            [FromQuery] string? bodyName,
            [FromQuery] string? systemName,
            [FromQuery] long? systemAddress,
            [FromQuery] int? bodyId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = null,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null)
        {
            systemAddress ??= long.TryParse(Request.Query["systemId64"], out var systemId64) ? systemId64 : null;

            return Ok(Service.GetBodies(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate));
        }

        /// <summary>Lookup stations</summary>
        /// <remarks>
        /// Returns stations matching all of the given parameters.
        /// 
        /// At least one of the following parameters must be provided:
        /// * `stationName`
        /// * `marketId`
        /// 
        /// The following parameters filter stations:
        /// * `stationName`
        /// * `marketId`
        /// * `includeRejected`
        /// 
        /// The following parameters filter matches:
        /// * `brief`
        /// * `limitMatches`
        /// * `minDate`
        /// * `maxDate`
        /// </remarks>
        /// <param name="stationName">Name of the station to search for</param>
        /// <param name="marketId">Market ID of the station to search for</param>
        /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
        /// <param name="brief">Set brief to only return station and system information</param>
        /// <param name="limitMatches">Limit number of matches returned</param>
        /// <param name="minDate">Start of date range for matches</param>
        /// <param name="maxDate">End of date range for matches</param>
        /// <returns>Matched station entries</returns>
        [HttpGet("stations")]
        [ProducesResponseType<List<StationData>>(StatusCodes.Status200OK)]
        public ActionResult<List<StationData>> GetStations(
            [FromQuery] string? stationName,
            [FromQuery] long? marketId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = null,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null)
        {
            return Ok(Service.GetStations(stationName, marketId, includeRejected, brief, limitMatches, minDate, maxDate));
        }

        /// <summary>Extract EDDN event</summary>
        /// <remarks>
        /// Extract line from indexed EDDN capture
        /// </remarks>
        /// <param name="filename">EDDN capture filename without path</param>
        /// <param name="lineno">1-based Line number</param>
        /// <returns>EDDN Event JSON</returns>
        [HttpGet("extract")]
        [ProducesResponseType<EDDNEvent>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<EDDNEvent> ExtractLine([FromQuery] string filename, [FromQuery] int lineno)
        {
            if (Service.ExtractLine(filename, lineno) is not { } line)
            {
                return NotFound();
            }

            return Content(line, "application/json");
        }
    }
}
