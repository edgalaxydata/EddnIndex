using EddnIndexLookup.DTO;
using EddnIndexLookup.Filters;
using EddnIndexLookup.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EddnIndexLookup.Controllers;

/// <inheritdoc/>
[EnableCors]
[Route("")]
[ApiController]
public class LookupController(EddnLookupService service) : ControllerBase
{
    private readonly EddnLookupService _service = service;

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
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("systems")]
    [HttpHead("systems")]
    [ProducesResponseType<List<SystemData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SystemData>>> GetSystemsAsync(
            [FromQuery] string? systemName,
            [FromQuery] long? systemAddress,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
    {
        systemAddress ??= long.TryParse(Request.Query["systemId64"], out long systemId64) ? systemId64 : null;
        systemAddress ??= long.TryParse(Request.Query["systemAddress"], out systemId64) ? systemId64 : null;

        if (Request.Method == "HEAD")
        {
            brief = true;
        }

        var systems = await _service.GetSystemsAsync(systemName, systemAddress, includeRejected, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        systems = [..
            systems.Select(e => e with
            {
                Matches = e.Matches is null
                        ? null
                        : [..
                            e.Matches.Select(m => m with
                            {
                                Extract = GetExtractUrl(m.FileName, m.LineNo)
                            })
                        ],
                Bodies  = e.Bodies is null
                        ? null
                        : [..
                            e.Bodies.Select(b => b with
                            {
                                Matches = e.Matches is null
                                        ? null
                                        : [..
                                            e.Matches.Select(m => m with
                                            {
                                                Extract = GetExtractUrl(m.FileName, m.LineNo)
                                            })
                                        ]
                            })
                            .OrderBy(b => b.BodyId ?? int.MaxValue)
                            .ThenBy(b => b, new BodyDesignationComparer(e.Name))
                            .ThenBy(b => b.Name)
                        ]
            })
            .OrderByDescending(e => e.LastSeen)
        ];

        return Ok(systems);
    }

    /// <summary>Lookup systems by SystemAddress</summary>
    /// <remarks>
    /// Returns systems matching all of the given parameters.
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
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("systems/{systemAddress:long}")]
    [HttpHead("systems/{systemAddress:long}")]
    [ProducesResponseType<List<SystemData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SystemData>>> GetSystemsSysAddrAsync(
            [FromQuery] string? systemName,
            [FromRoute] long? systemAddress,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetSystemsAsync(systemName, systemAddress, includeRejected, brief, limitMatches, minDate, maxDate);

    /// <summary>Lookup systems by name</summary>
    /// <remarks>
    /// Returns systems matching all of the given parameters.
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
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("systems/{systemName}")]
    [HttpHead("systems/{systemName}")]
    [ProducesResponseType<List<SystemData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SystemData>>> GetSystemsSysNameAsync(
            [FromRoute] string? systemName,
            [FromQuery] long? systemAddress,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetSystemsAsync(systemName, systemAddress, includeRejected, brief, limitMatches, minDate, maxDate);

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
    [ApiExplorerSettings(GroupName = "v1")]
    [HttpGet("systems.php")]
    [HttpHead("systems.php")]
    [ProducesResponseType<List<SystemData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SystemData>>> GetSystemsV1Async(
            [FromQuery] string? systemName,
            [FromQuery(Name = "systemId64")] long? systemAddress,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetSystemsAsync(systemName, systemAddress, includeRejected, brief, limitMatches, minDate, maxDate);

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
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("bodies")]
    [HttpHead("bodies")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesAsync(
            [FromQuery] string? bodyName,
            [FromQuery] string? systemName,
            [FromQuery] long? systemAddress,
            [FromQuery] int? bodyId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
    {
        systemAddress ??= long.TryParse(Request.Query["systemId64"], out long systemId64) ? systemId64 : null;
        systemAddress ??= long.TryParse(Request.Query["systemAddress"], out systemId64) ? systemId64 : null;

        if (systemAddress >= (1 << 55))
        {
            bodyId = (int)(systemAddress >> 55);
            systemAddress &= (1 << 55) - 1;
        }

        if (Request.Method == "HEAD")
        {
            brief = true;
        }

        var bodies = await _service.GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        bodies = [..
            bodies.Select(e => e with
            {
                Matches = e.Matches is null
                        ? null
                        : [..
                            e.Matches.Select(m => m with
                            {
                                Extract = GetExtractUrl(m.FileName, m.LineNo)
                            })
                        ]
            })
            .OrderByDescending(e => e.LastSeen)
        ];

        return Ok(bodies);
    }

    /// <summary>Lookup bodies by id64</summary>
    /// <remarks>
    /// Returns bodies matching all of the given parameters
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
    /// <param name="systemName">Name of the system to search for the body</param>
    /// <param name="systemAddress">System Address (id64) of the body</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return body and system information without matches</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <returns>Matched body entries</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("bodies/{systemAddress:long}")]
    [HttpHead("bodies/{systemAddress:long}")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesSysAddrAsync(
            [FromQuery] string? bodyName,
            [FromQuery] string? systemName,
            [FromRoute] long? systemAddress,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetBodiesAsync(bodyName, systemName, systemAddress, 0, includeRejected, brief, limitMatches, minDate, maxDate);

    /// <summary>Lookup bodies by SystemAddress and BodyId</summary>
    /// <remarks>
    /// Returns bodies matching all of the given parameters
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
    /// <param name="systemName">Name of the system to search for the body</param>
    /// <param name="systemAddress">System Address (id64) of the system to search for the body</param>
    /// <param name="bodyId">Body ID of the body to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return body and system information without matches</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <returns>Matched body entries</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("bodies/{systemAddress:long}/{bodyId:int}")]
    [HttpHead("bodies/{systemAddress:long}/{bodyId:int}")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesSysAddrBodyIdAsync(
            [FromQuery] string? bodyName,
            [FromQuery] string? systemName,
            [FromRoute] long? systemAddress,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null,
            [FromRoute] int bodyId = 0
        )
        => await GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate);

    /// <summary>Lookup bodies by SystemAddress and body name</summary>
    /// <remarks>
    /// Returns bodies matching all of the given parameters
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
    /// <param name="systemName">Name of the system to search for the body</param>
    /// <param name="systemAddress">System Address (id64) of the system to search for the body</param>
    /// <param name="bodyId">Body ID of the body to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return body and system information without matches</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <returns>Matched body entries</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("bodies/{systemAddress:long}/{bodyName}")]
    [HttpHead("bodies/{systemAddress:long}/{bodyName}")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesSysAddrBodyNameAsync(
            [FromRoute] string? bodyName,
            [FromQuery] string? systemName,
            [FromRoute] long? systemAddress,
            [FromQuery] int? bodyId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate);

    /// <summary>Lookup bodies by system name and BodyId</summary>
    /// <remarks>
    /// Returns bodies matching all of the given parameters
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
    /// <param name="systemName">Name of the system to search for the body</param>
    /// <param name="systemAddress">System Address (id64) of the system to search for the body</param>
    /// <param name="bodyId">Body ID of the body to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return body and system information without matches</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <returns>Matched body entries</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("bodies/{systemName}/{bodyId:int}")]
    [HttpHead("bodies/{systemName}/{bodyId:int}")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesSysNameBodyIdAsync(
            [FromQuery] string? bodyName,
            [FromRoute] string? systemName,
            [FromQuery] long? systemAddress,
            [FromRoute] int? bodyId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate);

    /// <summary>Lookup bodies by system and body name</summary>
    /// <remarks>
    /// Returns bodies matching all of the given parameters
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
    /// <param name="systemName">Name of the system to search for the body</param>
    /// <param name="systemAddress">System Address (id64) of the system to search for the body</param>
    /// <param name="bodyId">Body ID of the body to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return body and system information without matches</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <returns>Matched body entries</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("bodies/{systemName}/{bodyName}")]
    [HttpHead("bodies/{systemName}/{bodyName}")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesSysNameBodyNameAsync(
            [FromRoute] string? bodyName,
            [FromRoute] string? systemName,
            [FromQuery] long? systemAddress,
            [FromQuery] int? bodyId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate);

    /// <summary>Lookup bodies by name</summary>
    /// <remarks>
    /// Returns bodies matching all of the given parameters
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
    /// <param name="systemName">Name of the system to search for the body</param>
    /// <param name="systemAddress">System Address (id64) of the system to search for the body</param>
    /// <param name="bodyId">Body ID of the body to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return body and system information without matches</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <returns>Matched body entries</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("bodies/{bodyName}")]
    [HttpHead("bodies/{bodyName}")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesBodyNameAsync(
            [FromRoute] string? bodyName,
            [FromQuery] string? systemName,
            [FromQuery] long? systemAddress,
            [FromQuery] int? bodyId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate);

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
    [ApiExplorerSettings(GroupName = "v1")]
    [HttpGet("bodies.php")]
    [HttpHead("bodies.php")]
    [ProducesResponseType<List<BodyData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BodyData>>> GetBodiesV1Async(
            [FromQuery] string? bodyName,
            [FromQuery] string? systemName,
            [FromQuery(Name = "systemId64")] long? systemAddress,
            [FromQuery] int? bodyId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetBodiesAsync(bodyName, systemName, systemAddress, bodyId, includeRejected, brief, limitMatches, minDate, maxDate);

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
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("stations")]
    [HttpHead("stations")]
    [ProducesResponseType<List<StationData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StationData>>> GetStationsAsync(
            [FromQuery] string? stationName,
            [FromQuery] long? marketId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
    {
        if (Request.Method == "HEAD")
        {
            brief = true;
        }

        var stations = await _service.GetStationsAsync(stationName, marketId, includeRejected, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        stations = [..
            stations.Select(e => e with
            {
                Matches = e.Matches is null
                        ? null
                        : [..
                            e.Matches.Select(m => m with
                            {
                                Extract = GetExtractUrl(m.FileName, m.LineNo)
                            })
                        ]
            })
            .OrderByDescending(e => e.LastSeen)
        ];

        return Ok(stations);
    }

    /// <summary>Lookup stations by MarketId</summary>
    /// <remarks>
    /// Returns stations matching all of the given parameters.
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
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("stations/{marketId:long}")]
    [HttpHead("stations/{marketId:long}")]
    [ProducesResponseType<List<StationData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StationData>>> GetStationsMarketIdAsync(
            [FromQuery] string? stationName,
            [FromRoute] long? marketId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetStationsAsync(stationName, marketId, includeRejected, brief, limitMatches, minDate, maxDate);

    /// <summary>Lookup stations by name</summary>
    /// <remarks>
    /// Returns stations matching all of the given parameters.
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
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("stations/{stationName}")]
    [HttpHead("stations/{stationName}")]
    [ProducesResponseType<List<StationData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StationData>>> GetStationsStationNameAsync(
            [FromRoute] string? stationName,
            [FromQuery] long? marketId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetStationsAsync(stationName, marketId, includeRejected, brief, limitMatches, minDate, maxDate);

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
    [ApiExplorerSettings(GroupName = "v1")]
    [HttpGet("marketstations.php")]
    [HttpHead("marketstations.php")]
    [ProducesResponseType<List<StationData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StationData>>> GetMarketStationsV1Async(
            [FromQuery] string? stationName,
            [FromQuery] long? marketId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
        => await GetStationsAsync(stationName, marketId, includeRejected, brief, limitMatches, minDate, maxDate);

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
    [ApiExplorerSettings(GroupName = "v1")]
    [HttpGet("stations.php")]
    [HttpHead("stations.php")]
    [ProducesResponseType<List<OldStationData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OldStationData>>> GetStationsV1Async(
            [FromQuery] string? stationName,
            [FromQuery] long? marketId,
            [FromQuery] bool includeRejected = false,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
    {
        if (Request.Method == "HEAD")
        {
            brief = true;
        }

        var stations = await _service.GetStationsAsync(stationName, marketId, includeRejected, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        return Ok(
            stations.Select(e => OldStationData.From(e with
            {
                Matches = e.Matches is null
                        ? null
                        : [..
                            e.Matches.Select(m => m with
                            {
                                Extract = GetExtractUrl(m.FileName, m.LineNo)
                            })
                        ]
            }))
            .OrderByDescending(e => e.LastSeen)
            .ToList()
        );
    }

    /// <summary>Lookup signals</summary>
    /// <param name="signalName">Name of the signal</param>
    /// <param name="systemName">Limit to events with given system name</param>
    /// <param name="systemAddress">Limit to events with given system address</param>
    /// <param name="brief">Set brief to only return signal information</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <returns>List of signals</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("signals/{signalName}")]
    [HttpHead("signals/{signalName}")]
    [ProducesResponseType<List<SignalData>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SignalData>>> GetSignalsAsync(
            string signalName,
            [FromQuery] string? systemName = null,
            [FromQuery] long? systemAddress = null,
            [FromQuery] bool brief = false,
            [FromQuery] int? limitMatches = 100,
            [FromQuery] DateTimeOffset? minDate = null,
            [FromQuery] DateTimeOffset? maxDate = null
        )
    {
        if (Request.Method == "HEAD")
        {
            brief = true;
        }

        var signals = await _service.GetSignalsAsync(signalName, systemName, systemAddress, brief, limitMatches, minDate, maxDate, HttpContext.RequestAborted);

        return Ok(
            signals.Select(e => e with
            {
                Matches = e.Matches is null
                    ? null
                    : [..
                            e.Matches.Select(m => m with
                            {
                                Extract = GetExtractUrl(m.FileName, m.LineNo)
                            })
                    ]
            })
            .OrderByDescending(e => e.LastSeen)
        );
    }

    private string? GetExtractUrl(string filename, int lineno)
        => Url.Action("ExtractLine", "Lookup", new { filename, lineno }, Request.Scheme);

    /// <summary>Extract EDDN event</summary>
    /// <remarks>
    /// Extract line from indexed EDDN capture
    /// </remarks>
    /// <param name="filename">EDDN capture filename without path</param>
    /// <param name="lineno">1-based Line number</param>
    /// <returns>EDDN Event JSON</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("events/{filename}/{lineno}")]
    [HttpHead("events/{filename}/{lineno}")]
    [ProducesResponseType<EDDNEvent>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EDDNEvent>> ExtractLineAsync(string filename, int lineno)
    {
        if (await _service.ExtractLineAsync(filename, lineno, HttpContext.RequestAborted) is not { } line)
        {
            return NotFound();
        }

        return Content(line, "application/json");
    }

    /// <summary>Extract EDDN event (Backwards compatibility endpoint)</summary>
    /// <remarks>
    /// Extract line from indexed EDDN capture
    /// </remarks>
    /// <param name="filename">EDDN capture filename without path</param>
    /// <param name="lineno">1-based Line number</param>
    /// <returns>EDDN Event JSON</returns>
    [ApiExplorerSettings(GroupName = "v1")]
    [HttpGet("extract.php")]
    [HttpHead("extract.php")]
    [ProducesResponseType<EDDNEvent>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EDDNEvent>> ExtractLineV1Async([FromQuery] string filename, [FromQuery] int lineno)
        => await ExtractLineAsync(filename, lineno);

    /// <summary>Get the list of known sectors</summary>
    /// <param name="includeSphereSectors">Include sphere sectors (AKA hand-authored sectors)</param>
    /// <returns>List of sector names</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("sectors")]
    [HttpHead("sectors")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<string>>> GetSectorNamesAsync([FromQuery] bool includeSphereSectors)
        => await _service.GetSectorsAsync(includeSphereSectors, HttpContext.RequestAborted);

    /// <summary>Get systems in a sector</summary>
    /// <remarks>
    /// Note that this will not currently search for systems that would fall inside
    /// a hand-authored sector unless `nameOnly` is `true` and the system name starts
    /// with the given sector name.
    ///
    /// Warning: in sectors close to the galactic core, this can return a large number of results.
    /// </remarks>
    /// <param name="sectorName">Name of the sector</param>
    /// <param name="nameOnly">Match name instead of SystemAddress</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <returns>List of systems</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("sectors/{sectorName}")]
    [HttpHead("sectors/{sectorName}")]
    [ProducesResponseType<List<SectorSystem>>(StatusCodes.Status200OK)]
    public async IAsyncEnumerable<SectorSystem> GetSectorSystemsAsync(
            string sectorName,
            [FromQuery] bool nameOnly = false,
            [FromQuery] bool includeRejected = false
        )
    {
        await foreach (var entry in _service.GetSectorSystemsAsync(sectorName, nameOnly, includeRejected, HttpContext.RequestAborted))
        {
            yield return entry;
        }
    }

    /// <summary>Get systems in a sector and boxel</summary>
    /// <remarks>
    /// Note that this will not currently search for systems that would fall inside
    /// a hand-authored sector unless `nameOnly` is `true` and the system name starts
    /// with the given sector name.
    /// </remarks>
    /// <param name="sectorName">Name of the sector</param>
    /// <param name="boxelName">Boxel suffix without N2 (sequence number)</param>
    /// <param name="nameOnly">Match name instead of SystemAddress</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <returns>List of systems</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("sectors/{sectorName}/{boxelName}")]
    [HttpHead("sectors/{sectorName}/{boxelName}")]
    [ProducesResponseType<List<SectorSystem>>(StatusCodes.Status200OK)]
    public async IAsyncEnumerable<SectorSystem> GetSectorSystemsAsync(
            string sectorName,
            [RegularExpression("^[A-Z][A-Z]-[A-Z] [a-h]([0-9]{1,3}-?)?$")] string boxelName,
            [FromQuery] bool nameOnly = false,
            [FromQuery] bool includeRejected = false
        )
    {
        await foreach (var entry in _service.GetSectorSystemsAsync(sectorName, nameOnly, includeRejected, HttpContext.RequestAborted, boxelName))
        {
            yield return entry;
        }
    }

    /// <summary>Get systems in a sector</summary>
    /// <remarks>
    /// Note that this will not currently search for systems that would fall inside
    /// a hand-authored sector unless `nameOnly` is `true` and the system name starts
    /// with the given sector name.
    ///
    /// Warning: in sectors close to the galactic core, this can return a large number of results.
    /// </remarks>
    /// <param name="sectorName">Name of the sector</param>
    /// <param name="boxelName">Boxel suffix without N2 (sequence number)</param>
    /// <param name="nameOnly">Match name instead of SystemAddress</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <returns>List of systems</returns>
    [ApiExplorerSettings(GroupName = "v1")]
    [HttpGet("regions.php")]
    [HttpHead("regions.php")]
    [ProducesResponseType<List<SectorSystem>>(StatusCodes.Status200OK)]
    public async IAsyncEnumerable<SectorSystem> GetSectorSystemsV1Async(
            [FromQuery(Name = "regionName")] string sectorName,
            [RegularExpression("^[A-Z][A-Z]-[A-Z] [a-h]([0-9]{1,3}-?)?$")] string? boxelName,
            [FromQuery] bool nameOnly = false,
            [FromQuery] bool includeRejected = false
        )
    {
        await foreach (var entry in _service.GetSectorSystemsAsync(sectorName, nameOnly, includeRejected, HttpContext.RequestAborted, boxelName))
        {
            yield return entry;
        }
    }

    /// <summary>Get a list of systems in the gaps between known systems in a sector</summary>
    /// <remarks>
    /// Enumerate the systems in the boxels that have been visited.
    ///
    /// Warning: in sectors close to the galactic core, this can return a large number of results.
    ///
    /// Only searches the base procedural name, and not any name in a hand-authored sector.
    /// </remarks>
    /// <param name="sectorName">Sector name</param>
    /// <returns>List of gap systems</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("gapsystems/{sectorName}")]
    [HttpHead("gapsystems/{sectorName}")]
    [ProducesResponseType<List<SystemGapData>>(StatusCodes.Status200OK)]
    public async IAsyncEnumerable<SystemGapData> GetGapSystemsAsync(
            string sectorName
        )
    {
        await foreach (var entry in _service.EnumerateGapSystemsAsync(sectorName, HttpContext.RequestAborted))
        {
            yield return entry;
        }
    }

    /// <summary>Get a list of systems in the gaps between known systems in a sector boxel</summary>
    /// <remarks>
    /// Enumerate the systems in the boxels that have been visited.
    ///
    /// Only searches the base procedural name, and not any name in a hand-authored sector.
    /// </remarks>
    /// <param name="sectorName">Sector name</param>
    /// <param name="boxelName">Boxel suffix without N2 (sequence number)</param>
    /// <returns>List of gap systems</returns>
    [ApiExplorerSettings(GroupName = "v2")]
    [HttpGet("gapsystems/{sectorName}/{boxelName}")]
    [HttpHead("gapsystems/{sectorName}/{boxelName}")]
    [ProducesResponseType<List<SystemGapData>>(StatusCodes.Status200OK)]
    public async IAsyncEnumerable<SystemGapData> GetGapSystemsAsync(
            string sectorName,
            [RegularExpression("^[A-Z][A-Z]-[A-Z] [a-h]([0-9]{1,3}-?)?$")] string boxelName
        )
    {
        await foreach (var entry in _service.EnumerateGapSystemsAsync(sectorName, HttpContext.RequestAborted, boxelName))
        {
            yield return entry;
        }
    }
}
