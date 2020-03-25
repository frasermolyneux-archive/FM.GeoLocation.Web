using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FM.GeoLocation.Client;
using FM.GeoLocation.Contract.Models;
using FM.GeoLocation.Web.Extensions;
using FM.GeoLocation.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FM.GeoLocation.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string UserLocationSessionKey = "UserGeoLocationDto";
        private readonly IGeoLocationClient _geoLocationClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger,
            IGeoLocationClient geoLocationClient,
            IHttpContextAccessor httpContext)
        {
            _logger = logger;
            _geoLocationClient = geoLocationClient;
            _httpContext = httpContext;
        }

        public async Task<IActionResult> Index()
        {
            var address = _httpContext.HttpContext.Connection.RemoteIpAddress.ToString();

            var sessionGeoLocationDto =
                _httpContext.HttpContext.Session.GetObjectFromJson<GeoLocationDto>(UserLocationSessionKey);

            if (sessionGeoLocationDto != null)
                return View(sessionGeoLocationDto);

            GeoLocationDto geoLocationDto;

            try
            {
                geoLocationDto = await _geoLocationClient.LookupAddress(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving geo-location data for {address}", address);

                geoLocationDto = new GeoLocationDto
                {
                    Address = "Unknown",
                    City = "Unknown",
                    Country = "Unknown",
                    Latitude = 0.0,
                    Longitude = 0.0
                };
            }

            _httpContext.HttpContext.Session.SetObjectAsJson(UserLocationSessionKey, geoLocationDto);

            return View(geoLocationDto);
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

        [HttpGet]
        public IActionResult LookupAddress()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupAddress(string address)
        {
            GeoLocationDto geoLocationDto;

            try
            {
                geoLocationDto = await _geoLocationClient.LookupAddress(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving geo-location data for {address}", address);
                geoLocationDto = null;
            }

            return View(geoLocationDto);
        }

        [HttpGet]
        public IActionResult BatchLookup()
        {
            return View(new BatchLookupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchLookup(string addressData)
        {
            var model = new BatchLookupViewModel
            {
                AddressData = addressData
            };

            if (string.IsNullOrWhiteSpace(addressData))
            {
                ModelState.AddModelError(nameof(addressData), "You need to provide address data, one entry per line");
                return View(model);
            }

            List<string> addresses;
            try
            {
                addresses = addressData.Split(Environment.NewLine).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse address data");
                ModelState.AddModelError(nameof(addressData), "Failed to parse address data");
                return View(model);
            }

            try
            {
                model.GeoLocationDtos = await _geoLocationClient.LookupAddressBatch(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving geo-location data for {addresses}", addresses);
            }

            return View(model);
        }
    }
}