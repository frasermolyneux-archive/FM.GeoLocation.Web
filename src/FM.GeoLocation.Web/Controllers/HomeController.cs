﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FM.GeoLocation.Client;
using FM.GeoLocation.Contract.Models;
using FM.GeoLocation.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FM.GeoLocation.Web.Controllers
{
    public class HomeController : Controller
    {
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

            GeoLocationDto geoLocationDto;

            try
            {
                geoLocationDto = await _geoLocationClient.LookupAddress(address);
            }
            catch (Exception e)
            {
                geoLocationDto = new GeoLocationDto
                {
                    Address = "Unknown",
                    City = "Unknown",
                    Country = "Unknown",
                    Latitude = 0.0,
                    Longitude = 0.0
                };
            }

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
    }
}