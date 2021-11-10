using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WoundImgRepo.Models
{
    public class Tissue
    {
        public int tissue_id { get; set; }
        [Required(ErrorMessage = "Please select Wound Tissue")]
        public string name { get; set; }
    }
}
