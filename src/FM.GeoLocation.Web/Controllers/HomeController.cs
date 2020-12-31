using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FM.GeoLocation.Contract.Interfaces;
using FM.GeoLocation.Contract.Models;
using FM.GeoLocation.Web.Extensions;
using FM.GeoLocation.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FM.GeoLocation.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string UserLocationSessionKey = "UserGeoLocationDto";
        private const string BatchLookupSessionKey = "BatchLookupAddressData";
        private readonly IAddressValidator _addressValidator;
        private readonly IWebHostEnvironment _environment;

        private readonly IGeoLocationClient _geoLocationClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger,
            IGeoLocationClient geoLocationClient,
            IHttpContextAccessor httpContext,
            IAddressValidator addressValidator,
            IWebHostEnvironment environment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _geoLocationClient = geoLocationClient ?? throw new ArgumentNullException(nameof(geoLocationClient));
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            _addressValidator = addressValidator ?? throw new ArgumentNullException(nameof(addressValidator));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public async Task<IActionResult> Index()
        {
            var sessionGeoLocationDto =
                _httpContext.HttpContext.Session.GetObjectFromJson<LookupAddressResponse>(UserLocationSessionKey);

            if (sessionGeoLocationDto != null)
                return View(sessionGeoLocationDto);

            var address = GetUsersIpForLookup();

            var lookupAddressResponse = await _geoLocationClient.LookupAddress(address.ToString());

            if (lookupAddressResponse == null || !lookupAddressResponse.Success)
            {
                _logger.LogError("Failed to retrieve geo-location data for {address}", address);
                return View(lookupAddressResponse);
            }

            _httpContext.HttpContext.Session.SetObjectAsJson(UserLocationSessionKey, lookupAddressResponse);

            return View(lookupAddressResponse);
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
        public async Task<IActionResult> LookupAddress(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return View(new LookupAddressViewModel());

            var model = new LookupAddressViewModel
            {
                AddressData = id
            };

            return await LookupAddress(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupAddress(LookupAddressViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (string.IsNullOrWhiteSpace(model.AddressData))
            {
                ModelState.AddModelError(nameof(model.AddressData),
                    "You must provide an address to query against. IP or DNS is acceptable.");
                return View(model);
            }

            if (!_addressValidator.ConvertAddress(model.AddressData, out var validatedAddress))
            {
                ModelState.AddModelError(nameof(model.AddressData),
                    "The address provided is invalid. IP or DNS is acceptable.");
                return View(model);
            }

            model.LookupAddressResponse = await _geoLocationClient.LookupAddress(model.AddressData);

            if (!model.LookupAddressResponse.Success)
            {
                ModelState.AddModelError(nameof(model.AddressData), model.LookupAddressResponse.ErrorMessage);
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult BatchLookup()
        {
            var addressData = _httpContext.HttpContext.Session.GetString(BatchLookupSessionKey);

            if (!string.IsNullOrWhiteSpace(addressData))
                return View(new BatchLookupViewModel
                {
                    AddressData = addressData
                });
            return View(new BatchLookupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchLookup(BatchLookupViewModel model)
        {
            _httpContext.HttpContext.Session.SetString(BatchLookupSessionKey, model.AddressData);

            if (!ModelState.IsValid) return View(model);

            if (string.IsNullOrWhiteSpace(model.AddressData))
            {
                ModelState.AddModelError(nameof(model.AddressData),
                    "You must provide a line separated list of addresses. IP or DNS is acceptable.");
                return View(model);
            }

            List<string> addresses;
            try
            {
                addresses = model.AddressData.Split(Environment.NewLine).ToList();
            }
            catch
            {
                ModelState.AddModelError(nameof(model.AddressData),
                    "Invalid data, you must provide a line separated list of addresses. IP or DNS is acceptable.");
                return View(model);
            }

            if (addresses.Count >= 20)
            {
                ModelState.AddModelError(nameof(model.AddressData),
                    "You can only search for a maximum of 20 addresses in one request");
                return View(model);
            }

            model.LookupAddressBatchResponse = await _geoLocationClient.LookupAddressBatch(addresses);

            if (!model.LookupAddressBatchResponse.Success)
            {
                ModelState.AddModelError(nameof(model.AddressData), model.LookupAddressBatchResponse.ErrorMessage);
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult RemoveData()
        {
            var model = new RemoveMyDataViewModel
            {
                AddressData = GetUsersIpForLookup().ToString()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveData(RemoveMyDataViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (string.IsNullOrWhiteSpace(model.AddressData))
            {
                ModelState.AddModelError(nameof(model.AddressData),
                    "You must provide an address to query against. IP or DNS is acceptable.");
                return View(model);
            }

            if (!_addressValidator.ConvertAddress(model.AddressData, out var validatedAddress))
            {
                ModelState.AddModelError(nameof(model.AddressData),
                    "The address provided is invalid. IP or DNS is acceptable.");
                return View(model);
            }

            var removeDataForAddressResponse = await _geoLocationClient.RemoveDataForAddress(model.AddressData);

            if (!removeDataForAddressResponse.Success)
            {
                ModelState.AddModelError(nameof(model.AddressData), removeDataForAddressResponse.ErrorMessage);
                return View(model);
            }

            model.RemoveDataForAddressResponse = removeDataForAddressResponse;

            return View(model);
        }

        private IPAddress GetUsersIpForLookup()
        {
            const string cfConnectingIpKey = "CF-Connecting-IP";
            const string xForwardedForHeaderKey = "X-Forwarded-For";

            if (_environment.IsDevelopment()) return IPAddress.Parse("8.8.8.8");

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

            return address;
        }
    }
}