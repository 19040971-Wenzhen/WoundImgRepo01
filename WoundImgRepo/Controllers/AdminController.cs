using WoundImgRepo.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Dynamic;

using System.Collections;
using System.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Security.Claims;


namespace hostrepository.Controllers
{
    public class AdminController : Controller
    {


        //display register page
        #region DisplayRegistry()
        [Authorize(Roles = "Admin")]
        public IActionResult Registry()
        {
            #region checkuserrole()
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
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion




            return View("~/Views/Admin/Registry.cshtml");

        }
        #endregion

        //display list of users
        #region showUserlist()
        [Authorize(Roles = "Admin")]

        public IActionResult Userlist()
        {
            #region checkuserrole()
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
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion


            List<User> List = DBUtl.GetList<User>("SELECT * FROM useracc");
            return View(List);
        }
        #endregion

        //stores in bad passwords
        #region badPasswords()
        private string[] badPasswords = new[] {
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


        //check if the details keyed in are eligable for registration
        #region Registrypost()
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Registry(string username, string password, string UserPw2, string email, String user_role)
        {

            #region checkuserrole()
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
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion

            Debug.WriteLine(username);
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(UserPw2) || string.IsNullOrEmpty(email) || String.IsNullOrEmpty(user_role))
            {

                //stores in password 1


                ViewData["Msg"] = "One or more fields are missing";
                ViewData["MsgType"] = "danger";
                return View("~/Views/Admin/Registry.cshtml");
            }
            else
            {
                //check for duplicate names
                string dupname = "SELECT * FROM useracc WHERE username = '{0}'";
                DataTable matchdupe = DBUtl.GetTable(dupname, username);
                if (matchdupe.Rows.Count == 1)
                {
                    ViewData["Msg"] = "duplicate name detected! ";
                    ViewData["MsgType"] = "danger";
                    return View("~/Views/Admin/Registry.cshtml");
                }

                //password checker for hackable passwords
                if (badPasswords.Contains(password) || password.Length < 5)
                {
                    ViewData["Msg"] = "main Password too weak.";
                    ViewData["MsgType"] = "warning";
                    return View("~/Views/Admin/Registry.cshtml");
                }

                // compare passwords
                if (password.Equals(UserPw2) != true || UserPw2.Length < 5)
                {
                    ViewData["Msg"] = "second password : error detected! ";
                    ViewData["MsgType"] = "danger";
                    return View("~/Views/Admin/Registry.cshtml");
                }



                Regex list_of_caps = new Regex(@"[A-Z]");

                //check if at least 1 character is in uppercase
                MatchCollection matches = list_of_caps.Matches(password);
                if (matches.Count == 0)
                {
                    ViewData["Msg"] = "Password Has no capital";
                    ViewData["MsgType"] = "danger";
                    return View("~/Views/Admin/Registry.cshtml");
                }

                Regex numbercheck = new Regex(@"[0-9]");

                //check if at least 1 character is in numbers
                MatchCollection matchnum = numbercheck.Matches(password);
                if (matchnum.Count == 0)
                {
                    ViewData["Msg"] = "Password Has no numbers";
                    ViewData["MsgType"] = "danger";
                    return View("~/Views/Admin/Registry.cshtml");
                }


                //check if username is in use
                string namecheck_SQL =
               @"SELECT user_id FROM useracc 
                      WHERE username = '{0}'";

                DataTable matchname = DBUtl.GetTable(namecheck_SQL, username);

                if (matchname.Rows.Count == 1)
                {
                    ViewData["Msg"] = "User currently exist , try using another name ";
                    ViewData["MsgType"] = "danger";
                    return View("~/Views/Admin/Registry.cshtml");
                }

                //check if email is duplicated
                string emailcheck_SQL =
               @"SELECT user_id FROM useracc 
                      WHERE email = '{0}'";

                DataTable matchemail = DBUtl.GetTable(emailcheck_SQL, email);

                if (matchemail.Rows.Count == 1)
                {
                    ViewData["Msg"] = "duplicate email detected";
                    ViewData["MsgType"] = "danger";
                    return View("~/Views/Admin/Registry.cshtml");
                }

                IFormCollection form = HttpContext.Request.Form;
                string Question = form["Qs"].ToString().Trim();
                string answer = form["ans"].ToString().Trim();

                Debug.Write(Question);
                Debug.Write(answer);
                Debug.Write(Question);

                //check if insert is done
                string INSERT = @"INSERT INTO useracc( username, email, password, user_role, status,question ,answer) 
                VALUES ( '{0}', '{1}', HASHBYTES('SHA1', '{2}'), '{3}', 1, '{4}' , HASHBYTES('SHA1', '{5}'))";
                int rowsAffected = DBUtl.ExecSQL(INSERT, username, email, password, user_role, Question, answer);

                if (rowsAffected == 1)
                {
                    //replace dis wif ur homepage
                    TempData["Msg"] = "Successful Registration !";
                    TempData["MsgType"] = "success";
                    return RedirectToAction("Userlist");
                }
                else
                {
                    TempData["Msg"] = "Registration not working";
                    TempData["MsgType"] = "danger";
                    return View();
                }

            }
        }
        #endregion


        #region showingedituser()
        [Authorize(Roles = "Admin")]
        public IActionResult EditUser(string id)
        {
            #region checkuserrole()
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
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion
            //gets a user back in
            String Getuser = "SELECT * FROM useracc WHERE user_id = " + id;

            List<User> List = DBUtl.GetList<User>(Getuser);
            foreach (User account in List)
            {
                TempData["id"] = account.user_id;
                TempData["username"] = account.username;
                TempData["email"] = account.email;
                TempData["role"] = account.user_role;
                TempData["password"] = account.password;


            }
            TempData["usernamecurrently"] = "presentnamefirst";
            return View();
        }
        #endregion

        #region Edit user
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult EditUser(int editPW, string username, string password, string UserPw2, string email, String user_role, int id, string editqsORans, string question, string answer)
        {

          

            

            int checkifallgood = 0; //checks if all relative information is pushed out

            #region checkuserrole()
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
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion
            Debug.WriteLine("we are editing on :" + editPW);
            //<---------------------------------------------------------------------------------------------------->
            //resources for checking names
            string dupname = "SELECT user_id FROM useracc WHERE username = '{0}' AND user_id != {1}";
            DataTable matchdupe = DBUtl.GetTable(dupname, username, id);

            //<---------------------------------------------------------------------------------------------------->
            //fault counter
            int atfault = 0;

            //<---------------------------------------------------------------------------------------------------->
            //Resource to check if email is in use
            string emailcheck_SQL =
               @"SELECT user_id FROM useracc 
                      WHERE email = '{0}' AND user_id != {1}";

            DataTable matchemail = DBUtl.GetTable(emailcheck_SQL, email, id);


            //---------------------------------------------------------------------------------------------------------------------------
            //check if non-password fields are empty first
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                atfault = 1;
                ViewData["Msg"] = "non-password fields not entered";
                ViewData["MsgType"] = "danger";
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //check for duplicate names

            else if (matchdupe.Rows.Count > 0)
            {
                atfault = 1;
                ViewData["Msg"] = "duplicate name detected! ";
                ViewData["MsgType"] = "danger";

            }
            //---------------------------------------------------------------------------------------------------------------------------
            //check for duplicate emails

            else if (matchemail.Rows.Count > 0)
            {
                atfault = 1;
                ViewData["Msg"] = "duplicate email detected! ";
                ViewData["MsgType"] = "danger";

            }
            //---------------------------------------------------------------------------------------------------------------------------
            //fields for checking password
            else if (editPW == 1)
            {
                if (string.IsNullOrEmpty(password))
                {
                    password = "";

                }
                if (string.IsNullOrEmpty(UserPw2))
                {

                    UserPw2 = "";
                }

                //---------------------------------------------------------------------------------------------------------------------------
                //check if both passwords are empty
                if (password == "" || UserPw2 == "")
                {
                    atfault = 1;
                    ViewData["Msg"] = "One or more Passwords are empty ";
                    ViewData["MsgType"] = "danger";
                }
                //---------------------------------------------------------------------------------------------------------------------------
                else
                {
                    //check if password contains captial letters
                    Regex list_of_caps = new Regex(@"[A-Z]");
                    MatchCollection matches = list_of_caps.Matches(password);

                    //check if password contains numbers
                    Regex numbercheck = new Regex(@"[0-9]");
                    MatchCollection matchnum = numbercheck.Matches(password);
                    //---------------------------------------------------------------------------------------------------------------------------
                    //check if both passwords are matched up
                    if (password.Equals(UserPw2) != true)
                    {
                        atfault = 1;
                        ViewData["Msg"] = "Passwords do not match ";
                        ViewData["MsgType"] = "danger";
                    }
                    //---------------------------------------------------------------------------------------------------------------------------
                    //check both passwords are up to length
                    else if (password.Length < 5)
                    {
                        atfault = 1;
                        ViewData["Msg"] = "Password(s) length too short ";
                        ViewData["MsgType"] = "danger";
                    }
                    //---------------------------------------------------------------------------------------------------------------------------
                    //check if at least 1 character is in numbers for password
                    else if (matchnum.Count == 0)
                    {
                        atfault = 1;
                        ViewData["Msg"] = "Password Has no numbers";
                        ViewData["MsgType"] = "danger";
                    }
                    //---------------------------------------------------------------------------------------------------------------------------
                    //check if at least 1 character is in uppercase for password
                    else if (matches.Count == 0)
                    {
                        ViewData["Msg"] = "Password Has no capital";
                        ViewData["MsgType"] = "danger";
                        atfault = 1;
                    }
                    //---------------------------------------------------------------------------------------------------------------------------
                    //check if both passwords are presented in badPasswords
                    else if (badPasswords.Contains(password))
                    {
                        atfault = 1;
                        ViewData["Msg"] = "Passwords are not secured";
                        ViewData["MsgType"] = "danger";
                    }
                }

            }



            //---------------------------------------------------------------------------------------------------------------------------
            //triggers if no fault is detected
            if (atfault == 0)
            {

                int rowsAffected = 0;
                //---------------------------------------------------------------------------------------------------------------------------
                //Triggers if user allows password reset
                if (editPW == 1)
                {
                    String edituserconfirmed = @"UPDATE useracc SET 
                                                        username = '{0}' ,email = '{1}' ,password = HASHBYTES('SHA1', '{2}') ,user_role = '{3}' 
                                                        WHERE user_id = {4}";
                    rowsAffected = DBUtl.ExecSQL(edituserconfirmed, username, email, password, user_role, id);
                    TempData["Msg"] = "User updated";
                    TempData["MsgType"] = "success";
                    checkifallgood = 1;
                }
                //---------------------------------------------------------------------------------------------------------------------------
                //Triggers if user deny password reset
                if (editPW == 0)
                {
                    String edituserconfirmed = @"UPDATE useracc SET 
                                                        username = '{0}' ,email = '{1}' ,user_role = '{2}' 
                                                        WHERE user_id = {3}";
                    rowsAffected = DBUtl.ExecSQL(edituserconfirmed, username, email, user_role, id);

                    TempData["Msg"] = "User updated";
                    TempData["MsgType"] = "success";
                    checkifallgood = 1;
                }
                if (rowsAffected != 1)
                {
                    TempData["Msg"] = "User Record failed";
                    TempData["MsgType"] = "danger";
                }
            }
            //---------------------------------------------------------------------------------------------------------------------------
            //brings user details back into form if something isn't right
            if (atfault > 0)
            {
                //inject user details back into the form
                String Getuser = "SELECT * FROM useracc WHERE user_id = " + id;
                List<User> List = DBUtl.GetList<User>(Getuser);
                foreach (User account in List)
                {
                    TempData["id"] = account.user_id;
                    TempData["email"] = account.email;
                    TempData["role"] = account.user_role;
                    TempData["password"] = account.password;
                    TempData["usernamecurrently"] = username;
                    TempData["username"] = account.username;
                }

                return View();
            }

            //-------------------------------------------------------------------------------------------------------------
            //check if user would like to edit password after all is good
            if (atfault == 0)
            {

                Debug.WriteLine("activate edit qs N ans"); // acts as a signal that the question editing process is going on
                //get the user's username and password
                //-------------------------------------------------------------------------------------------------------------
                //edit only question
                if (editqsORans == "1")
                {
 question = question.Trim();
                    String edituserconfirmed = @"UPDATE useracc SET 
                                                        question = '{0}'
                                                        WHERE user_id = {1}";
                    int rowsAffected = DBUtl.ExecSQL(edituserconfirmed, question, id);

                }
                //-------------------------------------------------------------------------------------------------------------
                //edit only answer
                if (editqsORans == "2")
                { answer = answer.Trim();
                    String edituserconfirmed = @"UPDATE useracc SET 
                                                        answer  = HASHBYTES('SHA1', '{0}')
                                                        WHERE user_id = {1}";
                    int rowsAffected = DBUtl.ExecSQL(edituserconfirmed, answer, id);
                }
                //-------------------------------------------------------------------------------------------------------------
                //edit both
                if (editqsORans == "3")
                {  answer = answer.Trim();
                    question = question.Trim();
                    String edituserconfirmed = @"UPDATE useracc SET 
                                                         question = '{0}' ,  answer  = HASHBYTES('SHA1', '{1}')
                                                        WHERE user_id = {2}";
                    int rowsAffected = DBUtl.ExecSQL(edituserconfirmed, question, answer, id);
                }
                //-------------------------------------------------------------------------------------------------------------
            }
            //-------------------------------------------------------------------------------------------------------------
            return RedirectToAction("Userlist");
        }
        #endregion

        #region Delete
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            #region checkuserrole()
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
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion
            int AIDE = 0;

            String Getuser = "SELECT * FROM useracc WHERE username = '" + User.Identity.Name + "'";
            List<User> List = DBUtl.GetList<User>(Getuser);
            foreach (User account in List)
            {
                AIDE = account.user_id;

            }


            if (id == AIDE)
            {
                TempData["Msg"] = "deleting own record account is NOT allowed";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Userlist");
            }
            else
            {

                string delete = "DELETE FROM useracc WHERE user_id={0}";


                string formatdelete = String.Format(delete, id);
                Debug.WriteLine(formatdelete);
                int res = DBUtl.ExecSQL(delete, id);
                if (res == 1)
                {
                    TempData["Msg"] = "User Record Deleted";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Msg"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                }
            }
            return RedirectToAction("Userlist");
        }
        #endregion

        #region status edit
        public IActionResult Statusedit(int id)
        {
            #region checkuserrole()
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
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion
            //------------------------------------------------------------------------------------------------------------------------
            //check if user tried to deactivate their own account
            int AIDE = 0;

            String Getuser_viaid = "SELECT * FROM useracc WHERE username = '" + User.Identity.Name + "'";
            List<User> Lista = DBUtl.GetList<User>(Getuser_viaid);
            foreach (User account in Lista)
            {
                AIDE = account.user_id;

            }
            if (id == AIDE)
            {
                TempData["Msg"] = "de-activating your own account is NOT allowed";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Userlist");
            }

            //---------------------------------------------------------------------------------------------------------------------------
            //Takes in a user details
            String Getuser = "SELECT * FROM useracc WHERE user_id = " + id;
            List<User> List = DBUtl.GetList<User>(Getuser);
            int status = 0;
            foreach (User account in List)
            {
                status = account.status;
            }
            string update = "";
            //---------------------------------------------------------------------------------------------------------------------------
            //edit status
            if (status == 0)
            {
                update = "UPDATE useracc SET status = 1 WHERE user_id = {0}";
                TempData["Msg"] = "User account activated";
                TempData["MsgType"] = "success";
            }
            else if (status != 0)
            {
                update = "UPDATE useracc SET status = 0 WHERE user_id = {0}";
                TempData["Msg"] = "User account de-activated";
                TempData["MsgType"] = "success";
            }

            int rowsAffected = DBUtl.ExecSQL(update, id);

            return RedirectToAction("Userlist");
        }
        #endregion

        #region add/edit/delete version table
        public IActionResult Version()
        {
            #region checkuserrole
            int checktheuserrole = 0;
            if (User.IsInRole("Admin"))
            {
                checktheuserrole = 1;
            }
            else if (User.IsInRole("Doctor"))
            {
                checktheuserrole = 0;
            }
            else if (User.IsInRole("Annotator"))
            {
                checktheuserrole = 0;
            }
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion

            var versions = DBUtl.GetList<WVersion>($"SELECT * FROM version");
            return View(versions);
        }

        public IActionResult AddVersion(string name)
        {
            string message = "Internal Server Error";
            if (!string.IsNullOrEmpty(name))
            {
                var versions = DBUtl.GetList<WVersion>($"SELECT * FROM version");
                if (!versions.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    string wVLSql = @"INSERT INTO version(name)
                                      VALUES('{0}')";
                    var vExec = DBUtl.ExecSQL(wVLSql, name);
                    if (vExec == 1)
                    {
                        TempData["Msg"] = "New version created";
                        TempData["MsgType"] = "success";
                        return RedirectToAction("Version");
                    }
                    message = DBUtl.DB_Message;
                }
                message = "Version name already exist";
            }
            TempData["Msg"] = message;
            TempData["MsgType"] = "danger";
            return RedirectToAction("Version");
        }

        public IActionResult EditVersion(string name, int id)
        {
            string getVersionSql = @"SELECT * 
                                     FROM version 
                                     WHERE version_id={0}";
            List<WVersion> versionRecordFound = DBUtl.GetList<WVersion>(getVersionSql, id);

            //check wound category name not exist
            var versions = DBUtl.GetList<WVersion>($"SELECT * FROM version");
            if (versions.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Msg"] = "Version name already exist. Try giving unique name";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Version");
            }

            if (versionRecordFound.Count == 1)
            {
                if (!ModelState.IsValid)
                {
                    ViewData["Msg"] = "Invalid Input";
                    ViewData["MsgType"] = "danger";
                    return View("Version");
                }
                else
                {
                    string wvSql = @"UPDATE version
                                     SET name='{0}'
                                     WHERE version_id={1}";
                    var wvExec = DBUtl.ExecSQL(wvSql, name, id);
                    if (wvExec == 1)
                    {
                        TempData["Msg"] = "Version record updated";
                        TempData["MsgType"] = "success";
                    }
                    else
                    {
                        TempData["Msg"] = DBUtl.DB_Message;
                        TempData["MsgType"] = "danger";
                    }
                    return RedirectToAction("Version");
                }
            }
            else
            {
                TempData["Msg"] = "Version record does not exist";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Version");
            }
        }

        public IActionResult DeleteVersion(int id)
        {
            string wvSql = @"SELECT * 
                             FROM version 
                             WHERE version_id={0}";
            DataTable versionRecordFound = DBUtl.GetTable(wvSql, id);
            if (versionRecordFound.Rows.Count != 1)
            {
                TempData["Message"] = "Version record does not exist";
                TempData["MsgType"] = "warning";
            }
            else
            {
                wvSql = "DELETE FROM version WHERE version_id={0}";
                int wvExec = DBUtl.ExecSQL(wvSql, id);
                if (wvExec == 1)
                {
                    TempData["Msg"] = "Version record deleted";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Msg"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                }
            }
            return RedirectToAction("Version");
        }
        #endregion

        #region add/edit/delete wound category table
        public IActionResult WoundCategory()
        {
            #region checkuserrole
            int checktheuserrole = 0;
            if (User.IsInRole("Admin"))
            {
                checktheuserrole = 1;
            }
            else if (User.IsInRole("Doctor"))
            {
                checktheuserrole = 0;
            }
            else if (User.IsInRole("Annotator"))
            {
                checktheuserrole = 0;
            }
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion

            var woundCategories = DBUtl.GetList<WoundCategory>($"SELECT * FROM wound_category");
            return View(woundCategories);
        }

        public IActionResult AddWoundCategory(string name)
        {
            string message = "Internal Server Error";
            if (!string.IsNullOrEmpty(name))
            {
                var woundCategories = DBUtl.GetList<WoundCategory>($"SELECT * FROM wound_category");
                if (!woundCategories.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    string wcSql = @"INSERT INTO wound_category(name)
                                     VALUES('{0}')";
                    var wcExec = DBUtl.ExecSQL(wcSql, name);
                    if (wcExec == 1)
                    {
                        TempData["Msg"] = "New wound category created";
                        TempData["MsgType"] = "success";
                        return RedirectToAction("WoundCategory");
                    }
                    message = DBUtl.DB_Message;
                }
                message = "Wound category name already exist";
            }
            TempData["Msg"] = message;
            TempData["MsgType"] = "danger";
            return RedirectToAction("WoundCategory");
        }

        public IActionResult EditWoundCategory(string name, int id)
        {
            string getWoundCategorySql = @"SELECT * 
                                           FROM wound_category 
                                           WHERE wound_category_id={0}";
            List<WoundCategory> woundCategoryRecordFound = DBUtl.GetList<WoundCategory>(getWoundCategorySql, id);

            //check wound category name not exist
            var woundCategories = DBUtl.GetList<WoundCategory>($"SELECT * FROM wound_category");
            if (woundCategories.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Msg"] = "Wound Category name already exist. Try giving unique name";
                TempData["MsgType"] = "warning";
                return RedirectToAction("WoundCategory");
            }

            if (woundCategoryRecordFound.Count == 1)
            {
                if (!ModelState.IsValid)
                {
                    ViewData["Msg"] = "Invalid Input";
                    ViewData["MsgType"] = "danger";
                    return View("EditWoundCategory");
                }
                else
                {
                    string wcSql = @"UPDATE wound_category
                                     SET name='{0}'
                                     WHERE wound_category_id={1}";
                    var wcExec = DBUtl.ExecSQL(wcSql, name, id);
                    if (wcExec == 1)
                    {
                        TempData["Msg"] = "Wound category record updated";
                        TempData["MsgType"] = "success";
                    }
                    else
                    {
                        TempData["Msg"] = DBUtl.DB_Message;
                        TempData["MsgType"] = "danger";
                    }
                    return RedirectToAction("WoundCategory");
                }
            }
            else
            {
                TempData["Msg"] = "Wound category record does not exist";
                TempData["MsgType"] = "warning";
                return RedirectToAction("WoundCategory");
            }
        }

        public IActionResult DeleteWoundCategory(int id)
        {
            string wcSql = @"SELECT * 
                             FROM wound_category 
                             WHERE wound_category_id={0}";
            DataTable woundCategoryRecordFound = DBUtl.GetTable(wcSql, id);
            if (woundCategoryRecordFound.Rows.Count != 1)
            {
                TempData["Msg"] = "Wound category record does not exist";
                TempData["MsgType"] = "warning";
            }
            else
            {
                wcSql = "DELETE FROM wound_category WHERE wound_category_id={0}";
                int wcExec = DBUtl.ExecSQL(wcSql, id);
                if (wcExec == 1)
                {
                    TempData["Msg"] = "Wound category record deleted";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Msg"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                }
            }
            return RedirectToAction("WoundCategory");
        }
        #endregion

        #region add/edit/delete tissue table
        public IActionResult Tissue()
        {
            #region checkuserrole
            int checktheuserrole = 0;
            if (User.IsInRole("Admin"))
            {
                checktheuserrole = 1;
            }
            else if (User.IsInRole("Doctor"))
            {
                checktheuserrole = 0;
            }
            else if (User.IsInRole("Annotator"))
            {
                checktheuserrole = 0;
            }
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion
            var tissues = DBUtl.GetList<Tissue>($"SELECT * FROM tissue");
            return View(tissues);

        }

        public IActionResult AddTissue(string name)
        {
            string message = "Internal Server Error";
            if (!string.IsNullOrEmpty(name))
            {
                var tissues = DBUtl.GetList<Tissue>($"SELECT * FROM tissue");
                if (!tissues.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    string tSql = @"INSERT INTO tissue(name)
                                    VALUES('{0}')";
                    var tExec = DBUtl.ExecSQL(tSql, name);
                    if (tExec == 1)
                    {
                        TempData["Msg"] = "New tissue type created";
                        TempData["MsgType"] = "success";
                        return RedirectToAction("Tissue");
                    }
                    message = DBUtl.DB_Message;
                }
                message = "Tissue type already exist";
            }
            TempData["Msg"] = message;
            TempData["MsgType"] = "danger";
            return RedirectToAction("Tissue");
        }

        public IActionResult EditTissue(string name, int id)
        {
            string getTissueSql = @"SELECT * 
                                    FROM tissue 
                                    WHERE tissue_id={0}";
            List<Tissue> tissueRecordFound = DBUtl.GetList<Tissue>(getTissueSql, id);

            //check tissue name not exist
            var tissue = DBUtl.GetList<Tissue>($"SELECT * FROM tissue");
            if (tissue.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Msg"] = "Tissue name already exist. Try giving unique name";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Tissue");
            }

            if (tissueRecordFound.Count == 1)
            {
                if (!ModelState.IsValid)
                {
                    ViewData["Msg"] = "Invalid Input";
                    ViewData["MsgType"] = "danger";
                    return View("Tissue");
                }
                else
                {
                    string tSql = @"UPDATE tissue
                                    SET name='{0}'
                                    WHERE tissue_id={1}";
                    var tExec = DBUtl.ExecSQL(tSql, name, id);
                    if (tExec == 1)
                    {
                        TempData["Msg"] = "Tissue record updated";
                        TempData["MsgType"] = "success";
                    }
                    else
                    {
                        TempData["Msg"] = DBUtl.DB_Message;
                        TempData["MsgType"] = "danger";
                    }
                    return RedirectToAction("Tissue");
                }
            }
            else
            {
                TempData["Msg"] = "Tissue record does not exist";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Tissue");
            }
        }

        public IActionResult DeleteTissue(int id)
        {
            string tSql = @"SELECT * 
                            FROM tissue 
                            WHERE tissue_id={0}";
            DataTable tissueRecordFound = DBUtl.GetTable(tSql, id);
            if (tissueRecordFound.Rows.Count != 1)
            {
                TempData["Msg"] = "Tissue record does not exist";
                TempData["MsgType"] = "warning";
            }
            else
            {
                tSql = "DELETE FROM tissue WHERE tissue_id={0}";
                int tExec = DBUtl.ExecSQL(tSql, id);
                if (tExec == 1)
                {
                    TempData["Msg"] = "Tissue record deleted";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Msg"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                }
            }
            return RedirectToAction("Tissue");
        }
        #endregion
    }
}
