using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace WoundImgRepo.Models
{
    // This is for registration
    public class User
    {
     
         //BEWARE USERID IS FOR DATABASE
        [Required(ErrorMessage = "Please enter User ID")]
        public int user_id { get; set; }
   
        [Required]
        public string username { get; set; }

        [Required(ErrorMessage = "Please enter Email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string email { get; set; }



        //set password
        [Required(ErrorMessage = "Please enter Password")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Password must be 5-20 characters")]
        [DataType(DataType.Password)]
        public string password { get; set; }

        //just don't remove this
        [Required(ErrorMessage = "Please enter Password")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Password must be 5-20 characters")]
        [DataType(DataType.Password)]
        public string UserPw { get; set; }

        //conform password
        [Compare("UserPw", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string UserPw2 { get; set; }

        //user role
        [Required]
        [RegularExpression("(Doctor|Annotator|Admin)", ErrorMessage = "Select option")]
        public string user_role { get; set; }

        //for actiavting and deactivating accounts
        public int status { get; set; }

        //logging
        public DateTime last_login { get; set; }


        //checking if editing the password is needed (ONLY USED IN EDITUSER)
        public int editPW { get; set; }

        //checking if editing the securityquestion or answer is needed (ONLY USED IN EDITUSER)
        public int editqsORans { get; set; }

        public String question { get; set; }

        public string answer { get; set; }

    }
}
