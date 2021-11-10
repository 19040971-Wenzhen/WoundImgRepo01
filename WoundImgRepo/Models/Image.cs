using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WoundImgRepo.Models
{
    public class Image
    {
        public int image_id { get; set; }
        
        public string name { get; set; }
        public string type { get; set; }
        public IFormFile Photo { get; set; }
        public string img_file { get; set; }
    }
}
