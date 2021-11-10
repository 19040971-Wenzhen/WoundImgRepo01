using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WoundImgRepo.Models
{
    public class Annotation
    {
        public int annotation_id { get; set; }
        public int mask_image_id { get; set; }
        public int wound_id { get; set; }
        public int user_id { get; set; }
        public int annotation_image_id { get; set; }
    }
}
