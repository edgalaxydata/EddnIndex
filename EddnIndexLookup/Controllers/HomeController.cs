using EddnIndexLookup.DTO;
using EddnIndexLookup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EddnIndexLookup.Controllers;

/// <inheritdoc/>
[Route("")]
[ApiController]
public class HomeController(EddnLookupService service) : ControllerBase
{
    private readonly EddnLookupService Service = service;

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
    public async Task<ActionResult<List<SystemData>>> GetSystemsAsync(
        [FromQuery] string? systemName,
        [FromQuery] long? systemAddress,
        [FromQuery] bool includeRejected = false,
        [FromQuery] bool brief = false,
        [FromQuery] int? limitMatches = null,
        [FromQuery] DateTimeOffset? minDate = null,
        [FromQuery] DateTimeOffset? maxDate = null)
    {
        systemAddress ??= long.TryParse(Request.Query["systemId64"], out var systemId64) ? systemId64 : null;

        var systems = await Service.GetSystemsAsync(systemName, systemAddress, includeRejected, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        systems = [.. systems.Select(e => e with
        {
            Matches = e.Matches is null ? null : [.. e.Matches.Select(m => m with
            {
                Extract = GetExtractUrl(m.FileName, m.LineNo)
            })],
            Bodies = e.Bodies is null ? null : [.. e.Bodies.Select(b => b with
            {
                Matches = e.Matches is null ? null : [.. e.Matches.Select(m => m with
                {
                    Extract = GetExtractUrl(m.FileName, m.LineNo)
                })]
            })]
        })];

        return Ok(systems);
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
    public async Task<ActionResult<List<BodyData>>> GetBodiesAsync(
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

        var bodies = await Service.GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        bodies = [.. bodies.Select(e => e with
        {
            Matches = e.Matches is null ? null : [.. e.Matches.Select(m => m with
            {
                Extract = GetExtractUrl(m.FileName, m.LineNo)
            })]
        })];

        return Ok(bodies);
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
    public async Task<ActionResult<List<StationData>>> GetStationsAsync(
        [FromQuery] string? stationName,
        [FromQuery] long? marketId,
        [FromQuery] bool includeRejected = false,
        [FromQuery] bool brief = false,
        [FromQuery] int? limitMatches = null,
        [FromQuery] DateTimeOffset? minDate = null,
        [FromQuery] DateTimeOffset? maxDate = null)
    {
        var stations = await Service.GetStationsAsync(stationName, marketId, includeRejected, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        stations = [..
            stations.Select(e => e with
            {
                Matches = e.Matches is null ? null : [.. e.Matches.Select(m => m with
                {
                    Extract = GetExtractUrl(m.FileName, m.LineNo)
                })]
            })
        ];            

        return Ok(stations);
    }

    private string? GetExtractUrl(string filename, int lineno)
    {
        return Url.Action("ExtractLine", "Home", new { filename, lineno }, Request.Scheme);
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
    public async Task<ActionResult<EDDNEvent>> ExtractLineAsync([FromQuery] string filename, [FromQuery] int lineno)
    {
        if (await Service.ExtractLineAsync(filename, lineno, HttpContext.RequestAborted) is not { } line)
        {
            return NotFound();
        }

        return Content(line, "application/json");
    }
}
