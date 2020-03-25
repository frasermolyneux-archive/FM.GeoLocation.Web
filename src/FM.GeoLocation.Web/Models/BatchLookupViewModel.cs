using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FM.GeoLocation.Contract.Models;

namespace FM.GeoLocation.Web.Models
{
    public class BatchLookupViewModel
    {
        [Required(ErrorMessage = "You need to enter ip/domain data to lookup, one entry per line.")]
        [MaxLength(1024, ErrorMessage = "The inputted address data is greater than the maximum length")]
        [DataType(DataType.MultilineText)]
        public string AddressData { get; set; }

        public List<GeoLocationDto> GeoLocationDtos { get; set; }
    }
}