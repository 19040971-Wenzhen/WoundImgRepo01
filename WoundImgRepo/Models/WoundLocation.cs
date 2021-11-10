using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WoundImgRepo.Models
{
    public class WoundLocation
    {
        public int wound_location_id { get; set; }

        [Required(ErrorMessage = "Please enter Wound Location")]
        public string name { get; set; }
    }
}
