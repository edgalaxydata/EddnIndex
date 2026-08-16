using EddnIndexLookup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EddnIndexLookup.Controllers;

/// <inheritdoc/>
[ApiExplorerSettings(IgnoreApi = true)]
[Route("")]
public class HomeController(EddnLookupService service) : Controller
{
    private readonly EddnLookupService _service = service;

    /// <summary>
    /// Site entry point
    /// </summary>
    [HttpGet]
    public ActionResult Index()
        => Redirect("~/scalar");

    /// <summary>
    /// Render table of storage usage
    /// </summary>
    [Route("/tableinfo.php")]
    public async Task<IActionResult> TableInfoAsync()
        => View(await _service.GetStorageStatsAsync(HttpContext.RequestAborted));
}
