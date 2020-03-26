using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
            const string cfConnectingIpKey = "CF-Connecting-IP";
            const string xForwardedForHeaderKey = "X-Forwarded-For";

            IPAddress address = null;

            if (_httpContext.HttpContext.Request.Headers.ContainsKey(cfConnectingIpKey))
            {
                var cfConnectingIp = _httpContext.HttpContext.Request.Headers[cfConnectingIpKey];
                IPAddress.TryParse(cfConnectingIp, out address);
            }

            if (address == null && _httpContext.HttpContext.Request.Headers.ContainsKey(xForwardedForHeaderKey))
            {
                var forwardedAddress = _httpContext.HttpContext.Request.Headers[xForwardedForHeaderKey];
                IPAddress.TryParse(forwardedAddress, out address);
            }

            if (address == null)
                address = _httpContext.HttpContext.Connection.RemoteIpAddress;

            var sessionGeoLocationDto =
                _httpContext.HttpContext.Session.GetObjectFromJson<GeoLocationDto>(UserLocationSessionKey);

            if (sessionGeoLocationDto != null)
                return View(sessionGeoLocationDto);

            GeoLocationDto geoLocationDto;

            try
            {
                geoLocationDto = await _geoLocationClient.LookupAddress(address.ToString());
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
            return View(new LookupAddressViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupAddress(LookupAddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.AddressData))
            {
                ModelState.AddModelError(nameof(model.AddressData), "You need to provide address data");
                return View(model);
            }

            try
            {
                model.GeoLocationDto = await _geoLocationClient.LookupAddress(model.AddressData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving geo-location data for {address}", model.AddressData);
                ModelState.AddModelError(nameof(model.AddressData), "Failed to perform a geo-lookup for this address");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult BatchLookup()
        {
            return View(new BatchLookupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchLookup(BatchLookupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.AddressData))
            {
                ModelState.AddModelError(nameof(model.AddressData), "You need to provide address data, one entry per line");
                return View(model);
            }

            List<string> addresses;
            try
            {
                addresses = model.AddressData.Split(Environment.NewLine).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse address data");
                ModelState.AddModelError(nameof(model.AddressData), "Failed to parse address data");
                return View(model);
            }

            if (addresses.Count >= 20)
            {
                ModelState.AddModelError(nameof(model.AddressData), "You can only search for a maximum of 20 addresses in one request");
                return View(model);
            }

            try
            {
                model.GeoLocationDtos = await _geoLocationClient.LookupAddressBatch(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving geo-location data for {addresses}", addresses);
                ModelState.AddModelError(nameof(model.AddressData), "Failed to perform a geo-lookup for this address batch");
            }

            return View(model);
        }
    }
}