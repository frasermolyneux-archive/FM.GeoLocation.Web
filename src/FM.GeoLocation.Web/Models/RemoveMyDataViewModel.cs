using System.ComponentModel.DataAnnotations;
using FM.GeoLocation.Contract.Models;

namespace FM.GeoLocation.Web.Models
{
    public class RemoveMyDataViewModel
    {
        [Required(ErrorMessage = "You need to enter ip/domain data to purge from the service")]
        [MaxLength(256, ErrorMessage = "The inputted address data is greater than the maximum length")]
        [DataType(DataType.Text)]
        public string AddressData { get; set; }

        public RemoveDataForAddressResponse RemoveDataForAddressResponse { get; set; }
    }
}