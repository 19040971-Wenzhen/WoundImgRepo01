
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Security.Claims;
using WoundImgRepo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

namespace WoundImgRepo.Controllers
{

    public class AccountController : Controller
    {
        public string current_user = "";





        #region LogOff
        [Authorize]
        public IActionResult Logoff(string returnUrl = null)
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("LoginPage", "Account");
        }
        #endregion

        #region Loginpage
        [AllowAnonymous]
        public IActionResult LoginPage(string returnUrl = null)
        {
            #region RememberMe()
            int checktheuserrole = 0;
            if (User.IsInRole("Admin"))
            {
                checktheuserrole = 1;
            }
            else if (User.IsInRole("Doctor"))
            {
                checktheuserrole = 2;
            }
            else if (User.IsInRole("Annotator"))
            {
                checktheuserrole = 3;
            }

            //we now check if the account status is unlocked
            string name = User.Identity.Name;

            string getstatus = "SELECT * FROM useracc WHERE username ='" + name + "'";

            List<User> List = DBUtl.GetList<User>(getstatus);
            int status = 0;
            foreach (User account in List)
            {
                status = account.status;
            }

            if (status != 1)
            {
                TempData["ReturnUrl"] = returnUrl;
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Logoff();
                return View();
            }


            if (checktheuserrole == 0)
            {
                TempData["ReturnUrl"] = returnUrl;
                return View();
            }


            #endregion

            else
            {
                return RedirectToAction("TheWounds", "Wound");
            }

        }

        // Login
        #endregion

        #region postlogin
        [AllowAnonymous]
        [HttpPost]
        public IActionResult LoginPage(LogInUser user)
        {


            int status = 0;

            //getting the status of users
            string getuserstatus = "SELECT * FROM useracc  WHERE username = '" + user.Username + "' ";
            List<User> List = DBUtl.GetList<User>(getuserstatus);
            foreach (User account in List)
            {
                status = account.status;
            }
            System.Diagnostics.Debug.WriteLine("user status is" + status);





            if (!AuthenticateUser(user.Username, user.Password,
                                  out ClaimsPrincipal principal))
            {


                ViewData["Msg"] = "Incorrect Username or Password";
                ViewData["MsgType"] = "danger";
                return View();
            }
            else if (status != 1)
            {
                ViewData["Msg"] = "Account is deactivated , please contact your supervisor for support.";
                ViewData["MsgType"] = "danger";
                return View();
            }
            else
            {
                HttpContext.SignInAsync(
                   CookieAuthenticationDefaults.AuthenticationScheme,
                   principal,
                   new AuthenticationProperties
                   {
                       IsPersistent = user.RememberMe
                   }
                   );



                if (TempData["returnUrl"] != null)
                {
                    string returnUrl = TempData["returnUrl"].ToString();
                    if (Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                }





                // Update the Last Login Timestamp of the User
                string update = "UPDATE useracc SET last_login=GETDATE() WHERE username='{0}' AND password= HASHBYTES('SHA1', '{1}')";
                DBUtl.ExecSQL(update, user.Username, user.Password);


                System.Diagnostics.Debug.WriteLine("login success!");
                current_user = user.Username;
                return RedirectToAction("TheWounds", "Wound");
            }
        }

        [AllowAnonymous]
        public IActionResult Forbidden()
        {
            return View();
        }
        #endregion


        private bool AuthenticateUser(string usname, string pw,

                                         out ClaimsPrincipal principal)
        {
            principal = null;


            string sql = @"SELECT * FROM useracc 
                         WHERE username = '{0}' AND password = HASHBYTES('SHA1', '{1}')";

            // TODO: L09 Task 1 - Make Login Secure, use the new way of calling DBUtl
            //string select = String.Format(sql, uid, pw);
            DataTable ds = DBUtl.GetTable(sql, usname, pw);
            //DataTable ds = DBUtl.GetTable(select);
            if (ds.Rows.Count == 1)
            {
                principal =
                   new ClaimsPrincipal(
                      new ClaimsIdentity(
                         new Claim[] {
                        new Claim(ClaimTypes.NameIdentifier, usname),
                        new Claim(ClaimTypes.Name, ds.Rows[0]["username"].ToString()),
                        new Claim(ClaimTypes.Role, ds.Rows[0]["user_role"].ToString())
                         },
                         CookieAuthenticationDefaults.AuthenticationScheme));
                return true;
            }
            return false;
        }






        //step 1 of recoveirng password : getting the user to key in the name
        #region forgetmeone
        public IActionResult forgetmeone()
        {
            return View();
        }
        #endregion

        //step 2 , check if name is available
        #region forgetmeonepost
        [HttpPost]
        public IActionResult forgetmeone(string Username)
        {
            string getid = "SELECT * FROM useracc WHERE username = '{0}'";

            DataTable aide = DBUtl.GetTable(getid, Username);
            ViewBag.Question = "";

            //if the name is found
            if (aide.Rows.Count == 1)
            {
                List<User> currentuser = DBUtl.GetList<User>(getid, Username);
                ViewBag.currentuserID = "";
                ViewBag.question = "";
                foreach (User Uzer in currentuser)
                {
                    TempData["IDcurrentuser"] = Uzer.user_id.ToString();


                //gets the question
                    TempData["question"] = Uzer.question;
                }
       

                return RedirectToAction("forgetmetwo");

            }
            else
            {//if not
                ViewData["Msg"] = "User not found! Please check again or contact your Admin!";
                ViewData["MsgType"] = "danger";
            }
            return View();

        }
        #endregion

        //displays area for keying security questions
        #region forgetmetwo
        public IActionResult forgetmetwo()
        {
       
            return View();
        }
        #endregion

        //now we check the answer with the question
        #region forgetmetwopost

        [HttpPost]
        public IActionResult forgetmetwo(String answer , String question , String user_id)
        {
            TempData["question"] = question;
            TempData["IDcurrentuser"] = user_id;
            
           

            string check = "select * FROM useracc WHERE user_id = {0} AND question  = '{1}' AND answer = HASHBYTES('SHA1','{2}')";
            List<User> currentuser = DBUtl.GetList<User>(check, user_id, question , answer);
            Debug.WriteLine(currentuser.Count);
            if (currentuser.Count == 1)
            {

                //bring to the last view (change password)
              return  RedirectToAction("resetpw");
            }
            else
            {
                ViewData["Msg"] = "Question answered wrongly , please check again or contact your supervisor";
                ViewData["MsgType"] = "danger";
            }
            return View();
        }
        #endregion



        public IActionResult resetpw()
        {
            string check = "select * FROM useracc WHERE user_id = {0}";
            List<User> getter = DBUtl.GetList<User>(check, TempData["IDcurrentuser"]);

            foreach (User useR in getter)
            {
                TempData["namecurrentuser"] = useR.username;
            }

            return View();
        }



        //stores in bad passwords
        #region badPasswords()
        private string [] badPasswords = new[] {
            "111111",
            "12345",
            "123456",
            "1234567",
            "12345678",
            "123456789",
            "abc123",
            "password",
            "password1",
            "qwerty",
            "Password",
            "Password1",
            "QWERTY"
        };
        #endregion

        //reset password
        #region resetpwpost
        [HttpPost]
        public IActionResult resetpw(String user_id , String username , String password , String UserPw2)
        {
            int error = 0;

            TempData["IDcurrentuser"] = user_id;

            TempData["namecurrentuser"] = username;

            Regex numbercheck = new Regex(@"[0-9]");
            if(String.IsNullOrEmpty(password) || String.IsNullOrEmpty(UserPw2))
            {
                ViewData["Msg"] = "empty password case found! ";
                ViewData["MsgType"] = "danger";
                return View();
            }


            //check if at least 1 character is in numbers
            MatchCollection matchnum = numbercheck.Matches(password);

            Regex list_of_caps = new Regex(@"[A-Z]");

            //check if at least 1 character is in uppercase
            MatchCollection matches = list_of_caps.Matches(password);


            //password checker for hackable passwords
            if (badPasswords.Contains(password) || password.Length < 5)
            {
                ViewData["Msg"] = "main Password too weak.";
                ViewData["MsgType"] = "warning";
                error = 1; return View();
            }

            // compare passwords
            else if (password.Equals(UserPw2) != true || UserPw2.Length < 5)
            {
                ViewData["Msg"] = "second password : error detected! ";
                ViewData["MsgType"] = "danger";
                error = 1; return View();
            }

            else if (matches.Count == 0)
            {
                ViewData["Msg"] = "Password Has no capital";
                ViewData["MsgType"] = "danger";
                error = 1; return View();
            }

            else if (matchnum.Count == 0)
            {
                ViewData["Msg"] = "Password Has no numbers";
                ViewData["MsgType"] = "danger";
                error = 1; return View();
            }
            int rowsAffected = 0;

            if (error !=1)
            {

                Debug.WriteLine("i will try to pusgh :" + user_id);
                String edituserconfirmed = @"UPDATE useracc SET 
                                                         password = HASHBYTES('SHA1', '{0}')
                                                        WHERE user_id = {1}";
                rowsAffected = DBUtl.ExecSQL(edituserconfirmed, password, user_id);
            }

            if(rowsAffected == 1)
            {
                return RedirectToAction("LoginPage");
            }
            else
            {
                ViewData["Msg"] = DBUtl.DB_Message;
                ViewData["MsgType"] = "danger";
            }

            return View();
       
        }
        #endregion
    
    
    }

}

