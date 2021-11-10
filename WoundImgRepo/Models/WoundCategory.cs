using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WoundImgRepo.Models
{
    public class WoundCategory
    {
        public int wound_category_id { get; set; }

        [Required(ErrorMessage = "Please select Wound Category")]
        public string name { get; set; }
    }
}
