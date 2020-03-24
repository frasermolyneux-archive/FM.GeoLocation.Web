using System.Diagnostics;
using System.Threading.Tasks;
using FM.GeoLocation.Client;
using FM.GeoLocation.Web.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FM.GeoLocation.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGeoLocationClient _geoLocationClient;
        private readonly IHttpConnectionFeature _httpConnection;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IGeoLocationClient geoLocationClient,
            IHttpConnectionFeature httpConnection)
        {
            _logger = logger;
            _geoLocationClient = geoLocationClient;
            _httpConnection = httpConnection;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.GeoData = await _geoLocationClient.LookupAddress(_httpConnection.RemoteIpAddress.ToString());

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}