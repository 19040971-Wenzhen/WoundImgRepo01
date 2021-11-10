using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WoundImgRepo.Models
{
    public class CombineClass
    {
        public Wound wound { get; set; }
        public WoundCategory woundc { get; set; }
        public WoundLocation woundl { get; set; }
        public Tissue tissue { get; set; }
        public WVersion woundv { get; set; }
        public Image image { get; set; }
        public IFormFile annotationimage { get; set; }
        public IFormFile maskimage { get; set; }
        public List<IFormFile> annotationimages { get; set; }
        public List<IFormFile> maskimages { get; set; }
    }

    //get/set data from database
    public class WoundRecord
    {
        public int woundid { get; set; }
        public string woundname { get; set; }
        public string woundstage { get; set; }
        public string woundremarks { get; set; }
        public string woundcategoryname { get; set; }
        public string woundlocationname { get; set; }
        public string versionname { get; set; }
        public string tissuename { get; set; }
        public string username { get; set; }
        public string imagefile { get; set; }
        public int imageid { get; set; }
        public IFormFile annotationimage { get; set; }
        public IFormFile maskimage { get; set; }
        public List<AnnotationMaskImage> annotationMaskImage { get; set; }
        public int versionid { get; set; }

    }

    public class WoundDetailsViewModel
    {
        public List<WoundRecord> woundRecordList { get; set; }
        public WoundRecord woundRecord { get; set; }
    }
}
