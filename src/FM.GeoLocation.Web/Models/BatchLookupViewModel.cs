using System.Collections.Generic;
using FM.GeoLocation.Contract.Models;

namespace FM.GeoLocation.Web.Models
{
    public class BatchLookupViewModel
    {
        public string AddressData { get; set; }
        public List<GeoLocationDto> GeoLocationDtos { get; set; }
    }
}