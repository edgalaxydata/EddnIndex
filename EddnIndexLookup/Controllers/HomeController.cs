using EddnIndexLookup.Services;
using EddnIndexUpdate.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EddnIndexLookup.Controllers
{
    /// <inheritdoc/>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("")]
    public class HomeController(EddnLookupService service) : Controller
    {
        private readonly EddnLookupService Service = service;

        /// <summary>
        /// Site entry point
        /// </summary>
        [HttpGet]
        public ActionResult Index()
        {
            return Redirect("~/scalar");
        }

        /// <summary>
        /// Render table of storage usage
        /// </summary>
        [Route("/tableinfo.php")]
        public async Task<IActionResult> TableInfo()
        {
            return View(await Service.GetStorageStats(HttpContext.RequestAborted));
        }
    }
}
