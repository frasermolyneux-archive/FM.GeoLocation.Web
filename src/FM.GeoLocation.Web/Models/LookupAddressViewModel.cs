using System.ComponentModel.DataAnnotations;
using FM.GeoLocation.Contract.Models;

namespace FM.GeoLocation.Web.Models
{
    public class LookupAddressViewModel
    {
        [Required(ErrorMessage = "You need to enter ip/domain data to lookup")]
        [MaxLength(256, ErrorMessage = "The inputted address data is greater than the maximum length")]
        [DataType(DataType.Text)]
        public string AddressData { get; set; }

        public GeoLocationDto GeoLocationDto { get; set; }
    }
}