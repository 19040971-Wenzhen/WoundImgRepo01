using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WoundImgRepo.Models
{
    public class WVersion
    {
        public int version_id { get; set; }

        [Required(ErrorMessage = "Please select Wound Version")]
        public string name { get; set; }
    }
}
