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
using System.Diagnostics;
using System.Dynamic;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace WoundImgRepo.Controllers
{
    public class WoundController : Controller
    {
        #region Index()
        [Authorize(Roles = "Admin, Annotator")]
        public IActionResult Index()
        {
            #region checkuserrole
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
            ViewBag.showhidecheckchecker = 0;
            ViewBag.keyword = "";
            ViewBag.selection = "nothing";
            List<WoundRecord> list = DBUtl.GetList<WoundRecord>(@"SELECT w.wound_id as woundid, w.name as woundname, w.wound_stage as woundstage, w.remarks as woundremarks, 
                                                                  wc.name as woundcategoryname, wl.name as woundlocationname, t.name as tissuename,
                                                                  v.name as versionname, i.img_file as imagefile, i.image_id as imageid, u.username
                                                                  FROM wound w
                                                                  INNER JOIN image i ON i.image_id = w.image_id
                                                                  INNER JOIN wound_category wc ON wc.wound_category_id = w.wound_category_id
                                                                  INNER JOIN wound_location wl ON wl.wound_location_id = w.wound_location_id
                                                                  INNER JOIN tissue t ON t.tissue_id = w.tissue_id
                                                                  INNER JOIN version v ON v.version_id = w.version_id
                                                                  INNER JOIN useracc u ON u.user_id = w.user_id");
            list = list.GroupBy(x => x.woundname).Select(y => y.FirstOrDefault())?.ToList();
            return View("Index", list);
        }
        #endregion

        #region multidelete()
        [Authorize(Roles = "Admin, Annotator")]

        public IActionResult MultiDeleteWounds(IFormCollection col)
        {
            #region checkuserrole()
            int checktheuserrole = 0;
            if (User.IsInRole("Admin"))
            {
                checktheuserrole = 1;
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
            string deletables = col["DBL"];    //fetches the list
            //check if user keyed in anything yet
            if (deletables.Length == 0)
            {
                TempData["Msg"] = "No records selected.";
                TempData["MsgType"] = "danger";



                return RedirectToAction("Index");
            }

            //check the strings one by one

            String[] ahreileast = deletables.Split(',');       //splits the string into an array of string
            Regex NOT_numbers = new Regex(@"[^0-9]");    //It is a regex to check if a value isn't a number

            foreach (string iD in ahreileast)
            {
                //check if this string has anything that is not a number
                MatchCollection matchNnum = NOT_numbers.Matches(iD);

                if (matchNnum.Count == 0)
                {
                    Debug.WriteLine("you good");
                    deletefunction(iD);
                }
                else
                {
                    Debug.WriteLine("TEXT DETECTED :" + iD);
                }


            }




            return RedirectToAction("Index");
        }
        #endregion

        #region Delete()
        public IActionResult Delete(String id)
        {
            string nopic = "";
            String mainpictureID = ""; //gets the main picture ID , it can be used to track down other record versions
            int multipleimagescheck = 0;
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
                checktheuserrole = 3;
            }
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion

            #region Getid()
            //-------------------------------------------------------------------------------------
            string getpicid = ""; //store id of pictures

            //gets the list of ids for pictures
            //do note that w.image_id , im.image_id & i.image_id are STILL picture id , however they are just named different now
            string tableid = @" SELECT  w.image_id AS woundid, i.image_id AS versionid , im.image_id AS imageid
                                                   FROM annotation an
                                                   INNER JOIN image i ON an.annotation_image_id = i.image_id
                                                   INNER JOIN image im ON an.mask_image_id = im.image_id
                                                   INNER JOIN wound w ON an.wound_id = w.wound_id
                                                   WHERE w.wound_id = {0} ;";




            //do note that w.image_id , im.image_id & i.image_id are STILL picture id , however they are just different now
            List<WoundRecord> gotallidw = DBUtl.GetList<WoundRecord>(tableid, id);

            gotallidw.ToArray(); //converts DB list to an array

            //add the saved values as a usable SQL string
            foreach (WoundRecord iD in gotallidw)
            {
                mainpictureID = "" + iD.woundid;  //recrds down the main image id
                Debug.WriteLine("the main picture is" + mainpictureID);
                if (multipleimagescheck == 0)
                {
                    getpicid += "image_id =" + iD.imageid;


                    multipleimagescheck += 1; //a record has more than 1 existing set of pictures it is important to check
                }
                else
                {
                    getpicid += "OR image_id =" + iD.imageid;
                }

                //if the registered record has a annotation or a mask id , then add it in
                if (iD.woundid > 0)
                {


                    getpicid += " OR image_id =" + iD.woundid;
                }

                if (iD.versionid > 0)
                {
                    nopic = iD.versionid.ToString();
                    getpicid += " OR image_id =" + iD.versionid;
                }

            }

            //------------------------------------------------------------------------------------------------
            //error occurs only if there is no annotation or mask , if that's the case:
            Debug.WriteLine("nopic is" + nopic);


            string getsinglepicid = "SELECT image_id AS imageid FROM wound  WHERE wound_id = {0}"; // gets only the picture id
            List<WoundRecord> gotoneid = DBUtl.GetList<WoundRecord>(getsinglepicid, id);

            gotoneid.ToArray();
            if (nopic.Length == 0)
            {
                foreach (WoundRecord iD in gotoneid)
                {
                    getpicid = "image_id =" + iD.imageid;
                }
            }
            mainpictureID = gotoneid[0].imageid.ToString();
            #endregion
            //----------------------------------------------------------------------------------------------
            //Gets the wound_location _id for deletion

            String getthelocationid = "SELECT wound_location_id AS woundid FROM wound WHERE wound_id ={0}"; //sql string to get id

            String categoryid = ""; //stores in category ID

            List<WoundRecord> gotWLid = DBUtl.GetList<WoundRecord>(getthelocationid, id);

            gotWLid.ToArray(); //converts DB list to an array

            foreach (WoundRecord iD in gotWLid)
            {
                categoryid += iD.woundid;
                break;
            }
            String deletethelocationid = "DELETE FROM wound_location WHERE wound_location_id ={0}";
            string deletecategoryidF = string.Format(deletethelocationid, categoryid);
            Debug.WriteLine(deletecategoryidF);

            Debug.WriteLine(categoryid);
            //---------------------------------------------------------------------------------------
            //Find annotation id of other records first


            String GetALlAnnotationid = @"SELECT annotation_id AS imageid FROM annotation an
                                                   INNER JOIN image i ON an.annotation_image_id = i.image_id
                                                   INNER JOIN image im ON an.mask_image_id = im.image_id
                                                   INNER JOIN wound w ON an.wound_id = w.wound_id

                           WHERE w.Image_id = {0}";
            String allAnnotation = ""; //stores in the annotation id relating to wounds and their corresponding versions

            Debug.WriteLine("the main picture is in annotation " + mainpictureID + " and also + " + DBUtl.ExecSQL(GetALlAnnotationid, mainpictureID));

            Debug.WriteLine("the main picture is" + mainpictureID);
            //get all records of varations of the wound which has other annotations
            List<WoundRecord> gotALLannoID = DBUtl.GetList<WoundRecord>(GetALlAnnotationid, mainpictureID);




            gotALLannoID.ToArray(); // turn it into an array
            int countannoadd = 0;
            foreach (WoundRecord iD in gotALLannoID)
            {
                if (countannoadd == 0)
                {
                    allAnnotation += "DELETE FROM annotation WHERE annotation_id = " + iD.imageid;
                    countannoadd += 1;
                }
                else
                {
                    allAnnotation += " OR annotation_id =  " + iD.imageid;
                }

            }
            Debug.WriteLine(String.Format("DELETE FROM annotation WHERE {0}", allAnnotation));

            //-----------------------------------------------------------------------------------
            //Next we have to find all existing record's own version records
            String allRelatedWoundRec = ""; //gets all other wound records

            String RetreveallOTHERWoundRecNver = "SELECT wound_id AS woundid FROM wound WHERE image_id = {0} ";

            int addcommas = 0; //adds a counter for the system to see if commas should be added

            string storeothers = ""; //store other versions

            //gets ALL ID related to main record and remove the main record out

            Debug.WriteLine(string.Format(RetreveallOTHERWoundRecNver, mainpictureID));

            bool otherRecordsExist = false; //tells the system that other records exist

            //check if the table will bring back results of other existing records

            List<WoundRecord> gotallOTHERWoundRecNver = DBUtl.GetList<WoundRecord>(RetreveallOTHERWoundRecNver, mainpictureID);

            gotallOTHERWoundRecNver.ToArray(); //covert list of records to an array
            if (gotallOTHERWoundRecNver.Count > 1)
            {


                foreach (WoundRecord iD in gotallOTHERWoundRecNver)
                {
                    otherRecordsExist = true;
                    //this function records all existing wound records
                    allRelatedWoundRec += " OR wound_id=" + iD.woundid;

                    //This function records all existing wounds
                    if (addcommas != 1)
                    {
                        addcommas = 1;
                        storeothers += "" + iD.woundid;
                    }
                    else
                    {
                        storeothers += "," + iD.woundid;
                    }

                }
            }
            //if other records do exist
            if (otherRecordsExist == true)
            {
                //takes the string of stored records and turn it into a array
                string[] otherrecordId = storeothers.Split(',');

                foreach (string iD in otherrecordId)
                {//store an SQL string to delete other pictures
                    getpicid += picturedelete(iD);
                }
            }

            //---------------------------------------------------------------------------------------
            //                                    String for annotation                       String for wound                    string for picture deletion
            string deletewoundandannotationSQL = " {3} DELETE FROM wound WHERE wound_id={0} {4}  {1} {2}";
            //4.String for other records
            //string for location deletion
            string deleteallpictures = "DELETE FROM image WHERE {0}"; //formats off the images to delete

            string FDP = string.Format(deleteallpictures, getpicid); //combines the sentences


            //checks if the excecutions of Delete statements worked
            //Putting everthing together
            Debug.WriteLine(string.Format(deletewoundandannotationSQL, id, FDP, deletecategoryidF, allAnnotation, allRelatedWoundRec));
            if (DBUtl.ExecSQL(deletewoundandannotationSQL, id, FDP, deletecategoryidF, allAnnotation, allRelatedWoundRec) == 1)
            {

                TempData["Msg"] = "Wound record deleted!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index");


            }
            else
            {

                TempData["Msg"] = "All records must have a mask or annotation before deleting ";

                TempData["Msg"] = DBUtl.DB_Message;

                TempData["MsgType"] = "danger";
                return RedirectToAction("Index");
            }
        }
        #endregion

        //Delete function for multi delete
        #region DeleteFunction()
        public void deletefunction(String id)
        {
            string nopic = "";
            String mainpictureID = ""; //gets the main picture ID , it can be used to track down other record versions
            int multipleimagescheck = 0;


            #region Getid()
            //-------------------------------------------------------------------------------------
            string getpicid = ""; //store id of pictures

            //gets the list of ids for pictures
            //do note that w.image_id , im.image_id & i.image_id are STILL picture id , however they are just named different now
            string tableid = @" SELECT  w.image_id AS woundid, i.image_id AS versionid , im.image_id AS imageid
                                                   FROM annotation an
                                                   INNER JOIN image i ON an.annotation_image_id = i.image_id
                                                   INNER JOIN image im ON an.mask_image_id = im.image_id
                                                   INNER JOIN wound w ON an.wound_id = w.wound_id
                                                   WHERE w.wound_id = {0} ;";




            //do note that w.image_id , im.image_id & i.image_id are STILL picture id , however they are just different now
            List<WoundRecord> gotallidw = DBUtl.GetList<WoundRecord>(tableid, id);

            gotallidw.ToArray(); //converts DB list to an array

            //add the saved values as a usable SQL string
            foreach (WoundRecord iD in gotallidw)
            {
                mainpictureID = "" + iD.woundid;  //recrds down the main image id
                Debug.WriteLine("the main picture is" + mainpictureID);
                if (multipleimagescheck == 0)
                {
                    getpicid += "image_id =" + iD.imageid;


                    multipleimagescheck += 1; //a record has more than 1 existing set of pictures it is important to check
                }
                else
                {
                    getpicid += "OR image_id =" + iD.imageid;
                }

                //if the registered record has a annotation or a mask id , then add it in
                if (iD.woundid > 0)
                {


                    getpicid += " OR image_id =" + iD.woundid;
                }

                if (iD.versionid > 0)
                {
                    nopic = iD.versionid.ToString();
                    getpicid += " OR image_id =" + iD.versionid;
                }

            }

            //------------------------------------------------------------------------------------------------
            //error occurs only if there is no annotation or mask , if that's the case:
            Debug.WriteLine("nopic is" + nopic);


            string getsinglepicid = "SELECT image_id AS imageid FROM wound  WHERE wound_id = {0}"; // gets only the picture id
            List<WoundRecord> gotoneid = DBUtl.GetList<WoundRecord>(getsinglepicid, id);

            gotoneid.ToArray();
            if (nopic.Length == 0)
            {
                foreach (WoundRecord iD in gotoneid)
                {
                    getpicid = "image_id =" + iD.imageid;
                }
            }
            mainpictureID = gotoneid[0].imageid.ToString();
            #endregion
            //----------------------------------------------------------------------------------------------
            //Gets the wound_location _id for deletion

            String getthelocationid = "SELECT wound_location_id AS woundid FROM wound WHERE wound_id ={0}"; //sql string to get id

            String categoryid = ""; //stores in category ID

            List<WoundRecord> gotWLid = DBUtl.GetList<WoundRecord>(getthelocationid, id);

            gotWLid.ToArray(); //converts DB list to an array

            foreach (WoundRecord iD in gotWLid)
            {
                categoryid += iD.woundid;
                break;
            }
            String deletethelocationid = "DELETE FROM wound_location WHERE wound_location_id ={0}";
            string deletecategoryidF = string.Format(deletethelocationid, categoryid);
            Debug.WriteLine(deletecategoryidF);

            Debug.WriteLine(categoryid);
            //---------------------------------------------------------------------------------------
            //Find annotation id of other records first


            String GetALlAnnotationid = @"SELECT annotation_id AS imageid FROM annotation an
                                                   INNER JOIN image i ON an.annotation_image_id = i.image_id
                                                   INNER JOIN image im ON an.mask_image_id = im.image_id
                                                   INNER JOIN wound w ON an.wound_id = w.wound_id

                           WHERE w.Image_id = {0}";
            String allAnnotation = ""; //stores in the annotation id relating to wounds and their corresponding versions

            Debug.WriteLine("the main picture is in annotation " + mainpictureID + " and also + " + DBUtl.ExecSQL(GetALlAnnotationid, mainpictureID));

            Debug.WriteLine("the main picture is" + mainpictureID);
            //get all records of varations of the wound which has other annotations
            List<WoundRecord> gotALLannoID = DBUtl.GetList<WoundRecord>(GetALlAnnotationid, mainpictureID);




            gotALLannoID.ToArray(); // turn it into an array
            int countannoadd = 0;
            foreach (WoundRecord iD in gotALLannoID)
            {
                if (countannoadd == 0)
                {
                    allAnnotation += "DELETE FROM annotation WHERE annotation_id = " + iD.imageid;
                    countannoadd += 1;
                }
                else
                {
                    allAnnotation += " OR annotation_id =  " + iD.imageid;
                }

            }
            Debug.WriteLine(String.Format("DELETE FROM annotation WHERE {0}", allAnnotation));

            //-----------------------------------------------------------------------------------
            //Next we have to find all existing record's own version records
            String allRelatedWoundRec = ""; //gets all other wound records

            String RetreveallOTHERWoundRecNver = "SELECT wound_id AS woundid FROM wound WHERE image_id = {0} ";

            int addcommas = 0; //adds a counter for the system to see if commas should be added

            string storeothers = ""; //store other versions

            //gets ALL ID related to main record and remove the main record out

            Debug.WriteLine(string.Format(RetreveallOTHERWoundRecNver, mainpictureID));

            bool otherRecordsExist = false; //tells the system that other records exist

            //check if the table will bring back results of other existing records

            List<WoundRecord> gotallOTHERWoundRecNver = DBUtl.GetList<WoundRecord>(RetreveallOTHERWoundRecNver, mainpictureID);

            gotallOTHERWoundRecNver.ToArray(); //covert list of records to an array
            if (gotallOTHERWoundRecNver.Count > 1)
            {


                foreach (WoundRecord iD in gotallOTHERWoundRecNver)
                {
                    otherRecordsExist = true;
                    //this function records all existing wound records
                    allRelatedWoundRec += " OR wound_id=" + iD.woundid;

                    //This function records all existing wounds
                    if (addcommas != 1)
                    {
                        addcommas = 1;
                        storeothers += "" + iD.woundid;
                    }
                    else
                    {
                        storeothers += "," + iD.woundid;
                    }

                }
            }
            //if other records do exist
            if (otherRecordsExist == true)
            {
                //takes the string of stored records and turn it into a array
                string[] otherrecordId = storeothers.Split(',');

                foreach (string iD in otherrecordId)
                {//store an SQL string to delete other pictures
                    getpicid += picturedelete(iD);
                }
            }

            //---------------------------------------------------------------------------------------
            //                                    String for annotation                       String for wound                    string for picture deletion
            string deletewoundandannotationSQL = " {3} DELETE FROM wound WHERE wound_id={0} {4}  {1} {2}";
            //4.String for other records
            //string for location deletion
            string deleteallpictures = "DELETE FROM image WHERE {0}"; //formats off the images to delete

            string FDP = string.Format(deleteallpictures, getpicid); //combines the sentences


            //checks if the excecutions of Delete statements worked
            //Putting everthing together
            Debug.WriteLine(string.Format(deletewoundandannotationSQL, id, FDP, deletecategoryidF, allAnnotation, allRelatedWoundRec));
            DBUtl.ExecSQL(deletewoundandannotationSQL, id, FDP, deletecategoryidF, allAnnotation, allRelatedWoundRec);

        }
        #endregion

        //deletefunction for seperate functions
        #region picturedeletefunction()
        public String picturedelete(string id)
        {
            String nopic = ""; //stores in as a checkpoint , check if the picture doesn't have a annotation /mask

            //gets the table id of pictures OTHER than the main picture itself
            string tableidforpictures = @" SELECT  i.image_id AS versionid , im.image_id AS imageid
                                                   FROM annotation an
                                                   INNER JOIN image i ON an.annotation_image_id = i.image_id
                                                   INNER JOIN image im ON an.mask_image_id = im.image_id
                                                   INNER JOIN wound w ON an.wound_id = w.wound_id
                                                   WHERE w.wound_id = {0} ;";

            List<WoundRecord> getpicrecords = DBUtl.GetList<WoundRecord>(tableidforpictures, id);

            getpicrecords.ToArray();

            // to check if more than one anno/mask is in , to ensure proper string layout
            int multipleimagescheck = 0;

            string getpicid = "";

            foreach (WoundRecord iD in getpicrecords)
            {
                if (multipleimagescheck == 0)
                {
                    getpicid += " OR image_id =" + iD.imageid;


                    multipleimagescheck += 1; //a record has more than 1 existing set of pictures it is important to check
                }
                else
                {
                    getpicid += " OR image_id =" + iD.imageid;
                }

                //if the registered record has a annotation or a mask id , then add it in
                if (iD.woundid > 0)
                {
                    //The id from woundID will be equal to zero

                    getpicid += " OR image_id =" + iD.woundid;
                }

                if (iD.versionid > 0)
                {
                    nopic = iD.versionid.ToString();
                    getpicid += " OR image_id =" + iD.versionid;
                }

            }
            //As the bits of "or image_id" will be placed in , we must ensure to reset the getpicid
            if (nopic.Length == 0)
            {
                getpicid = "";
            }

            return getpicid;

        }
        #endregion

        #region Indexpost()
        public IActionResult Indexpost()
        {
            IFormCollection form = HttpContext.Request.Form;
            string searchedsection = form["searchedsection"].ToString();
            string searchedobj = form["searchedobj"].ToString().Trim();
            Debug.WriteLine("doin index search with " + searchedsection);

            String listinput = @"SELECT w.wound_id as woundid, w.name as woundname, w.wound_stage as woundstage, w.remarks as woundremarks, 
                                      wc.name as woundcategoryname, wl.name as woundlocationname, t.name as tissuename,
                                      v.name as versionname, i.img_file as imagefile, i.image_id as imageid, u.username
                                      FROM wound w
                                      INNER JOIN image i ON i.image_id = w.image_id
                                      INNER JOIN wound_category wc ON wc.wound_category_id = w.wound_category_id
                                      INNER JOIN wound_location wl ON wl.wound_location_id = w.wound_location_id
                                      INNER JOIN tissue t ON t.tissue_id = w.tissue_id
                                      INNER JOIN version v ON v.version_id = w.version_id
                                        INNER JOIN useracc u ON u.user_id = w.user_id
                                        WHERE " + searchedsection + " LIKE '%" + searchedobj + "%'";


            ViewBag.showhidecheckchecker = 1;
            Debug.WriteLine(listinput);
            ViewBag.keyword = searchedobj;
            ViewBag.selection = searchedsection;

            List<WoundRecord> list = DBUtl.GetList<WoundRecord>(listinput);

            return View("Index", list);
        }
        #endregion

        #region Details()
        public IActionResult Details(int id)
        {
            #region checkuserrole
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

            var wound = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE wound_id={id}")[0];
            string selectWoundSql = @"SELECT w.wound_id as woundid, w.name as woundname, w.wound_stage as woundstage, w.remarks as woundremarks, 
                                      wc.name as woundcategoryname, wl.name as woundlocationname, t.name as tissuename, 
                                      v.name as versionname, v.version_id as versionid, i.img_file as imagefile, i.image_id as imageid
                                      FROM wound w
                                      INNER JOIN image i ON i.image_id = w.image_id
                                      INNER JOIN wound_category wc ON wc.wound_category_id = w.wound_category_id
                                      INNER JOIN wound_location wl ON wl.wound_location_id = w.wound_location_id
                                      INNER JOIN tissue t ON t.tissue_id = w.tissue_id
                                      INNER JOIN version v ON v.version_id = w.version_id
                                      WHERE w.name='{0}'";
            //retrieve all wound record
            List<WoundRecord> recordFound = DBUtl.GetList<WoundRecord>(selectWoundSql, wound.name);
            //create new list of WoundRecord
            var woundRecordList = new List<WoundRecord>();
            if (recordFound.Count > 0)
            {
                //check if any of the wound record that has been found has the same version name in the list (not needed since 1 wound record can only have 1 version)
                foreach (var woundRecord in recordFound)
                {
                    string selectAnnotationSql = @"SELECT i.img_file as annotationimagefile, im.img_file as maskimagefile, annotation_id as annotationid
                                                   FROM annotation an
                                                   INNER JOIN image i ON an.annotation_image_id = i.image_id
                                                   INNER JOIN image im ON an.mask_image_id = im.image_id
                                                   INNER JOIN wound w ON an.wound_id = w.wound_id
                                                   WHERE w.wound_id={0} AND w.version_id={1}";
                    List<AnnotationMaskImage> annotationMaskImageList = DBUtl.GetList<AnnotationMaskImage>(selectAnnotationSql, woundRecord.woundid, woundRecord.versionid);

                    if (woundRecordList.Any(x => x.versionname.Equals(woundRecord.versionname, StringComparison.OrdinalIgnoreCase)))
                    {
                        //get the wound record that has the same version name and add in the additional annotation/mask image (not needed since 1 wound record can only have 1 version)
                        var sameVersionNameWR = woundRecordList.FirstOrDefault(x => x.versionname.Equals(woundRecord.versionname, StringComparison.OrdinalIgnoreCase));
                        sameVersionNameWR.annotationMaskImage.AddRange(annotationMaskImageList);
                    }
                    else
                    {
                        woundRecord.annotationMaskImage = new List<AnnotationMaskImage>();
                        woundRecord.annotationMaskImage.AddRange(annotationMaskImageList);
                        woundRecordList.Add(woundRecord);
                    }
                }
                //set version data dropdown list (not needed since 1 wound record can only have 1 version)
                //SetVersionViewData();

                //assign value to properties and pass to view
                var woundDetailsViewModel = new WoundDetailsViewModel()
                {
                    woundRecordList = woundRecordList,
                    woundRecord = recordFound[0]
                };
                return View(woundDetailsViewModel);
            }
            else
            {
                TempData["Msg"] = "Wound record does not exist";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region PreviousOrNextWound() / GetPreviousOrNextWound()
        public IActionResult PreviousOrNextWound(int woundId, bool previous = false)
        {
            var woundList = DBUtl.GetList<Wound>($"SELECT * FROM wound")?.GroupBy(x => x.name)?.Select(y => y.FirstOrDefault());
            var valid = previous ? woundList.First().wound_id != woundId : woundList.Last().wound_id != woundId;
            if (valid)
            {
                var wound = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE wound_id={woundId}")[0];
                var previousOrNextWound = GetPreviousOrNextWound(wound.name, previous, woundId);
                if (previousOrNextWound != null)
                {
                    return RedirectToAction("Details", new { id = previousOrNextWound.wound_id });
                }
            }
            TempData["Msg"] = (previous ? "Previous" : "Next") + " wound record does not exist";
            TempData["MsgType"] = "warning";
            return RedirectToAction("Details", new { id = woundId });
        }

        private Wound GetPreviousOrNextWound(string name, bool previous, int woundId = 0)
        {

            var wound = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE name='{name}'")[0];
            var id = 0;
            if (wound.wound_id == woundId)
            {
                id = previous ? wound.wound_id - 1 : wound.wound_id + 1;
            }
            else
            {
                id = previous ? woundId - 1 : woundId + 1;
            }
            var previousWound = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE wound_id={id} and name != '{name}'");
            if (!previousWound.Any())
            {
                return GetPreviousOrNextWound(name, previous, id);
            }
            return previousWound[0];
        }
        #endregion

        #region TheWounds()
        public IActionResult TheWounds()
        {
            #region checkuserrole
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

            List<WoundRecord> list = DBUtl.GetList<WoundRecord>(@"SELECT w.wound_id as woundid, w.name as woundname, w.wound_stage as woundstage, w.remarks as woundremarks, 
                                                                  wc.name as woundcategoryname, wl.name as woundlocationname, t.name as tissuename,
                                                                  v.name as versionname, i.img_file as imagefile, i.image_id as imageid, u.username
                                                                  FROM wound w
                                                                  INNER JOIN image i ON i.image_id = w.image_id
                                                                  INNER JOIN wound_category wc ON wc.wound_category_id = w.wound_category_id
                                                                  INNER JOIN wound_location wl ON wl.wound_location_id = w.wound_location_id
                                                                  INNER JOIN tissue t ON t.tissue_id = w.tissue_id
                                                                  INNER JOIN version v ON v.version_id = w.version_id
                                                                  INNER JOIN useracc u ON u.user_id = w.user_id");
            list = list.GroupBy(x => x.woundname).Select(y => y.FirstOrDefault())?.ToList();
            return View(list);
        }
        #endregion

        #region ZoomImage()
        public IActionResult ZoomImage(int id)
        {
            var wound = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE wound_id={id}")[0];
            string selectWoundSql = @"SELECT w.wound_id as woundid, w.name as woundname, w.wound_stage as woundstage, w.remarks as woundremarks, 
                                      wc.name as woundcategoryname, wl.name as woundlocationname, t.name as tissuename, 
                                      v.name as versionname, v.version_id as versionid, i.img_file as imagefile, i.image_id as imageid
                                      FROM wound w
                                      INNER JOIN image i ON i.image_id = w.image_id
                                      INNER JOIN wound_category wc ON wc.wound_category_id = w.wound_category_id
                                      INNER JOIN wound_location wl ON wl.wound_location_id = w.wound_location_id
                                      INNER JOIN tissue t ON t.tissue_id = w.tissue_id
                                      INNER JOIN version v ON v.version_id = w.version_id
                                      WHERE w.name='{0}'";
            List<WoundRecord> recordFound = DBUtl.GetList<WoundRecord>(selectWoundSql, wound.name);
            recordFound = recordFound.GroupBy(x => x.woundname).Select(y => y.FirstOrDefault())?.ToList();
            return View(recordFound);
        }
        #endregion

        #region SetWoundCategoryViewData()
        public void SetWoundCategoryViewData()
        {
            var getWoundCategory = DBUtl.GetList<WoundCategory>("SELECT * FROM wound_category");
            List<SelectListItem> wcSelectList = new List<SelectListItem>();
            foreach (var wc in getWoundCategory)
            {
                wcSelectList.Add(new SelectListItem()
                {
                    Text = wc.name,
                    Value = wc.name
                });
            }
            ViewData["woundCategory"] = new SelectList(wcSelectList, "Text", "Value");
        }
        #endregion

        #region SetTissueViewData()
        public void SetTissueViewData()
        {
            var getTissue = DBUtl.GetList<Tissue>("SELECT * FROM tissue");
            List<SelectListItem> tSelectList = new List<SelectListItem>();
            foreach (var t in getTissue)
            {
                tSelectList.Add(new SelectListItem()
                {
                    Text = t.name,
                    Value = t.name
                });
            }
            ViewData["tissue"] = new SelectList(tSelectList, "Text", "Value");
        }
        #endregion

        #region SetVersionViewData()
        public void SetVersionViewData()
        {
            var getVersion = DBUtl.GetList<WVersion>("SELECT * FROM version");
            List<SelectListItem> versionsSelectList = new List<SelectListItem>();
            foreach (var version in getVersion)
            {
                versionsSelectList.Add(new SelectListItem()
                {
                    Text = version.name,
                    Value = version.name
                });
            }
            ViewData["version"] = new SelectList(versionsSelectList, "Text", "Value");
        }
        #endregion

        #region Create()
        public IActionResult Create()
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
                checktheuserrole = 3;
            }
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("LoginPage", "Account", new { returnUrl = "/Wound/Create" });
            }
            //set wound category, tissue, version data dropdown list
            SetWoundCategoryViewData();
            SetVersionViewData();
            SetTissueViewData();
            return View();
        }

        [HttpPost]
        public IActionResult Create(CombineClass cc)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("LoginPage", "Account", new { returnUrl = "/Wound/Create" });
            }

            //set wound category, tissue, version data dropdown list
            SetWoundCategoryViewData();
            SetVersionViewData();
            SetTissueViewData();
            if (!ModelState.IsValid)
            {
                ViewData["Msg"] = "Invalid Input";
                ViewData["MsgType"] = "warning";
                return View("Create");
            }
            else
            {
                //check wound name not exist
                var wound = DBUtl.GetList<Wound>($"SELECT * FROM wound");
                if (wound.Any(x => x.name.Equals(cc.wound.name, StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["Msg"] = "Wound name already exist. Try giving unique name";
                    TempData["MsgType"] = "warning";
                    return RedirectToAction("Index");
                }

                //useracc table
                var userDetail = DBUtl.GetList<User>("SELECT * FROM useracc WHERE username = '" + User.Identity.Name + "'")[0];

                //image table
                string picfilename = DoPhotoUpload(cc.image.Photo);
                string imageSql = @"INSERT INTO image(name, type, img_file)
                                    VALUES('{0}','{1}','{2}')";
                int imageRowsAffected = DBUtl.ExecSQL(imageSql, cc.wound.name, "Original Wound Image", picfilename);
                Image img = DBUtl.GetList<Image>("SELECT image_id FROM image ORDER BY image_id DESC")[0];

                string anpicfilename = DoPhotoUpload(cc.annotationimage);
                string animageSql = @"INSERT INTO image(name, type, img_file)
                                      VALUES('{0}','{1}','{2}')";
                int animageRowsAffected = DBUtl.ExecSQL(animageSql, cc.wound.name, "Annotation Image", anpicfilename);
                var anImg = DBUtl.GetList<Image>("SELECT image_id FROM image WHERE name='" + cc.wound.name + "'AND type='Annotation Image'");

                string maskpicfilename = DoPhotoUpload(cc.maskimage);
                string maskimageSql = @"INSERT INTO image(name, type, img_file)
                                        VALUES('{0}','{1}','{2}')";
                int maskimageRowsAffected = DBUtl.ExecSQL(maskimageSql, cc.wound.name, "Mask Image", maskpicfilename);
                var maskImg = DBUtl.GetList<Image>("SELECT image_id FROM image WHERE name='" + cc.wound.name + "'AND type='Mask Image'");

                //wound_category table
                WoundCategory wc = DBUtl.GetList<WoundCategory>($"SELECT wound_category_id FROM wound_category WHERE name='{cc.woundc.name}'")[0];

                //wound_location table
                string wLSql = @"INSERT INTO wound_location(name)
                                 VALUES('{0}')";
                int wLRowsAffected = DBUtl.ExecSQL(wLSql, cc.woundl.name);
                WoundLocation wl = DBUtl.GetList<WoundLocation>("SELECT wound_location_id FROM wound_location ORDER BY wound_location_id DESC")[0];

                //tissue table
                Tissue t = DBUtl.GetList<Tissue>($"SELECT tissue_id FROM Tissue WHERE name='{cc.tissue.name}'")[0];

                //version table
                WVersion v = DBUtl.GetList<WVersion>($"SELECT version_id FROM Version WHERE name='{cc.woundv.name}'")[0];

                //wound table 
                string wSql = @"INSERT INTO wound(name, wound_stage, remarks, 
                                                  wound_category_id, wound_location_id, tissue_id, version_id, image_id, user_id)
                                VALUES('{0}','{1}','{2}',{3},{4},{5},{6},{7},{8})";
                int wRowsAffected = DBUtl.ExecSQL(wSql, cc.wound.name, cc.wound.wound_stage, cc.wound.remarks,
                                                        wc.wound_category_id, wl.wound_location_id, t.tissue_id,
                                                        v.version_id, img.image_id, userDetail.user_id);
                Wound w = DBUtl.GetList<Wound>("SELECT wound_id FROM wound ORDER BY wound_id DESC")[0];

                //annotation table
                int anRowsAffected = 0;
                int imgCount = 0;
                if (anImg.Count == maskImg.Count)
                {
                    anImg.ForEach(img =>
                    {
                        string anSql = @"INSERT INTO annotation(mask_image_id, wound_id, annotation_image_id, user_id)
                                         VALUES({0},{1},{2},{3})";
                        anRowsAffected = DBUtl.ExecSQL(anSql, maskImg[imgCount].image_id, w.wound_id, img.image_id, userDetail.user_id);
                        imgCount += 1;
                    });
                }

                if (imageRowsAffected == 1 &&
                    animageRowsAffected == 1 &&
                    maskimageRowsAffected == 1 &&
                    wLRowsAffected == 1 &&
                    wRowsAffected == 1 &&
                    anRowsAffected == 1)
                {
                    TempData["Msg"] = "New wound created";
                    TempData["MsgType"] = "success";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Msg"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                    return View("Create");
                }
            }
        }
        #endregion

        #region DeleteAnnotationMaskImage()
        public IActionResult DeleteAnnotationMaskImage(int woundid, int annotationid)
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
                checktheuserrole = 3;
            }
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion

            Debug.WriteLine("" + annotationid);

            var getAnnotation = DBUtl.GetList<Annotation>($"SELECT * FROM annotation WHERE annotation_id={annotationid}")[0];

            string annotationSql = "DELETE FROM annotation WHERE annotation_id={0}";
            int annotationRowsAffected = DBUtl.ExecSQL(annotationSql, annotationid);

            string maskImgSql = "DELETE FROM image WHERE image_id={0}";
            int maskImgRowsAffected = DBUtl.ExecSQL(maskImgSql, getAnnotation.mask_image_id);

            string annotationImgSql = "DELETE FROM image WHERE image_id={0}";
            int annotationImgRowsAffected = DBUtl.ExecSQL(annotationImgSql, getAnnotation.annotation_image_id);

            if (maskImgRowsAffected == 1 &&
                annotationImgRowsAffected == 1 &&
                annotationRowsAffected == 1)
            {
                TempData["Msg"] = "Annotation and Mask image deleted";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = DBUtl.DB_Message;
                TempData["MsgType"] = "danger";
            }
            return RedirectToAction("Details", new { id = woundid });
        }
        #endregion

        #region UpdateAnnotationMaskImage()
        [HttpPost]
        public IActionResult UpdateAnnotationMaskImage(WoundDetailsViewModel details)
        {
            var wr = details.woundRecord;
            if (!ModelState.IsValid)
            {
                ViewData["Msg"] = "Invalid Input";
                ViewData["MsgType"] = "warning";
                return RedirectToAction("Details", new { id = wr.woundid });
            }
            else
            {
                //useracc table
                var userDetail = DBUtl.GetList<User>("SELECT * FROM useracc WHERE username = '" + User.Identity.Name + "'")[0];

                //(not needed since 1 wound record can only have 1 version)
                //var version = DBUtl.GetList<WVersion>($"SELECT * FROM version WHERE name='{wr.versionname}'")[0];
                //var woundList = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE name='{wr.woundname}' AND version_id={version.version_id}");
                //var wound = new Wound();
                //if (woundList.Any())
                //{
                //    wound = woundList.FirstOrDefault();
                //}
                //else
                //{
                //    var getWound = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE wound_id={wr.woundid}")[0];
                //    //wound table 
                //    string wSql = @"INSERT INTO wound(name, wound_stage, remarks, 
                //                                      wound_category_id, wound_location_id, tissue_id, version_id, image_id, user_id)
                //                    VALUES('{0}','{1}','{2}',{3},{4},{5},{6},{7},{8})";
                //    int wRowsAffected = DBUtl.ExecSQL(wSql, getWound.name, getWound.wound_stage, getWound.remarks,
                //                                            getWound.wound_category_id, getWound.wound_location_id, getWound.tissue_id,
                //                                            version.version_id, getWound.image_id, userDetail.user_id);
                //    wound = DBUtl.GetList<Wound>("SELECT * FROM wound ORDER BY wound_id DESC")[0];
                //}

                //image table
                string anpicfilename = DoPhotoUpload(wr.annotationimage);
                string animageSql = @"INSERT INTO image(name, type, img_file)
                                      VALUES('{0}','{1}','{2}')";
                int animageRowsAffected = DBUtl.ExecSQL(animageSql, wr.woundname, "Annotation Image", anpicfilename);
                var anImg = DBUtl.GetList<Image>("SELECT image_id FROM image WHERE name='" + wr.woundname + "'AND type='Annotation Image' AND img_file='" + anpicfilename + "'");

                string maskpicfilename = DoPhotoUpload(wr.maskimage);
                string maskimageSql = @"INSERT INTO image(name, type, img_file)
                                        VALUES('{0}','{1}','{2}')";
                int maskimageRowsAffected = DBUtl.ExecSQL(maskimageSql, wr.woundname, "Mask Image", maskpicfilename);
                var maskImg = DBUtl.GetList<Image>("SELECT image_id FROM image WHERE name='" + wr.woundname + "'AND type='Mask Image' AND img_file='" + maskpicfilename + "'");

                //annotation table
                int anRowsAffected = 0;
                int imgCount = 0;
                if (anImg.Count == maskImg.Count)
                {
                    anImg.ForEach(img =>
                    {
                        string anSql = @"INSERT INTO annotation(mask_image_id, wound_id, annotation_image_id, user_id)
                                         VALUES({0},{1},{2},{3})";
                        anRowsAffected = DBUtl.ExecSQL(anSql, maskImg[imgCount].image_id, wr.woundid, img.image_id, userDetail.user_id);
                        imgCount += 1;
                    });
                }

                if (animageRowsAffected == 1 &&
                    maskimageRowsAffected == 1 &&
                    anRowsAffected == 1)
                {
                    TempData["Msg"] = "Annotation and Mask images added successfully";
                    TempData["MsgType"] = "success";
                    return RedirectToAction("Details", new { id = wr.woundid });
                }
                else
                {
                    TempData["Msg"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                    return RedirectToAction("Details", new { id = wr.woundid });
                }
            }
        }
        #endregion

        #region Update()
        public IActionResult Update(int id)
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
                checktheuserrole = 3;
            }
            if (checktheuserrole == 0)
            {
                return View("~/Views/Account/Forbidden.cshtml");
            }
            #endregion

            string selectWoundSql = @"SELECT wound_id as woundid, w.name as woundname, w.wound_stage as woundstage, w.remarks as woundremarks, 
                                      wc.name as woundcategoryname, wl.name as woundlocationname, t.name as tissuename, 
                                      v.name as versionname, i.img_file as imagefile, i.image_id as imageid
                                      FROM wound w
                                      INNER JOIN image i ON i.image_id = w.image_id
                                      INNER JOIN wound_category wc ON wc.wound_category_id = w.wound_category_id
                                      INNER JOIN wound_location wl ON wl.wound_location_id = w.wound_location_id
                                      INNER JOIN tissue t ON t.tissue_id = w.tissue_id
                                      INNER JOIN version v ON v.version_id = w.version_id
                                      WHERE wound_id={0}";
            List<WoundRecord> recordFound = DBUtl.GetList<WoundRecord>(selectWoundSql, id);
            if (recordFound.Count == 1)
            {
                WoundRecord woundRecord = recordFound[0];
                var wound = DBUtl.GetList<Wound>($"SELECT * FROM wound WHERE wound_id={id}")[0];
                var img = DBUtl.GetList<Image>($"SELECT img_file FROM image WHERE image_id={woundRecord.imageid}")[0];
                //record - Image(wound image), Wound(id, name, stage, remarks), WoundCategory(id, name), WoundLocation(id, name), Tissue(id, name), WVersion(id, name)
                CombineClass record = new CombineClass()
                {
                    wound = new Wound() { wound_id = id, name = woundRecord.woundname, wound_stage = woundRecord.woundstage, remarks = woundRecord.woundremarks },
                    woundc = new WoundCategory() { wound_category_id = wound.wound_category_id, name = woundRecord.woundcategoryname },
                    woundl = new WoundLocation() { wound_location_id = wound.wound_location_id, name = woundRecord.woundlocationname },
                    tissue = new Tissue() { tissue_id = wound.tissue_id, name = woundRecord.tissuename },
                    woundv = new WVersion() { version_id = wound.version_id, name = woundRecord.versionname },
                    image = new Image() { img_file = img.img_file }
                };
                //set wound category, tissue, version data dropdown list
                SetWoundCategoryViewData();
                //(not needed since 1 wound record can only have 1 version)
                //SetVersionViewData();
                SetTissueViewData();
                return View(record);
            }
            else
            {
                TempData["Msg"] = "Wound record does not exist";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Details");
            }
        }

        [HttpPost]
        public IActionResult Update(CombineClass cc)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Msg"] = "Invalid Input";
                ViewData["MsgType"] = "warning";
                return View("Index");
            }
            else
            {
                //wound_category table
                WoundCategory wc = DBUtl.GetList<WoundCategory>($"SELECT wound_category_id FROM wound_category WHERE name='{cc.woundc.name}'")[0];

                //wound_location table
                string wLSql = @"UPDATE wound_location
                                 SET name='{0}'
                                 WHERE wound_location_id={1}";
                int wLRowsAffected = DBUtl.ExecSQL(wLSql, cc.woundl.name, cc.woundl.wound_location_id);
                WoundLocation wl = DBUtl.GetList<WoundLocation>($"SELECT wound_location_id FROM wound_location WHERE name='{cc.woundl.name}'")[0];

                //tissue table
                Tissue t = DBUtl.GetList<Tissue>($"SELECT tissue_id FROM Tissue WHERE name='{cc.tissue.name}'")[0];

                //version table
                WVersion v = DBUtl.GetList<WVersion>($"SELECT version_id FROM Version WHERE name='{cc.woundv.name}'")[0];

                //wound table
                string wSql = @"UPDATE wound
                                SET name='{0}', wound_stage='{1}', remarks='{2}', wound_category_id={3}, wound_location_id={4}, tissue_id={5}, version_id={6}
                                WHERE wound_id={7}";
                int wRowsAffected = DBUtl.ExecSQL(wSql, cc.wound.name, cc.wound.wound_stage, cc.wound.remarks, wc.wound_category_id, wl.wound_location_id, t.tissue_id, v.version_id, cc.wound.wound_id);

                if (wRowsAffected == 1 &&
                    wLRowsAffected == 1)
                {
                    TempData["Msg"] = "Wound record updated";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Msg"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                }
            }
            return RedirectToAction("Details", new { id = cc.wound.wound_id });
        }
        #endregion

        #region DoPhotoUpload()
        private string DoPhotoUpload(IFormFile photo)
        {

            string fext = Path.GetExtension(photo.FileName);
            string uname = Guid.NewGuid().ToString();
            string fname = uname + fext;
            string fullpath = Path.Combine(_env.WebRootPath, "photos/" + fname);
            using (FileStream fs = new FileStream(fullpath, FileMode.Create))
            {
                photo.CopyTo(fs);
            }
            return fname;
        }

        private IWebHostEnvironment _env;
        public WoundController(IWebHostEnvironment environment)
        {
            _env = environment;
        }
        #endregion

        #region filterfoundrymainpage
        public IActionResult findonmain()
        {
            #region checkuserrole
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
            IFormCollection form = HttpContext.Request.Form;
            string searchedsection = form["searchedsection"].ToString();
            string searchedobj = form["searchedobj"].ToString().Trim();
            Debug.WriteLine("doin index search with " + searchedsection);

            String listinput = @"SELECT w.wound_id as woundid, w.name as woundname, w.wound_stage as woundstage, w.remarks as woundremarks, 
                                      wc.name as woundcategoryname, wl.name as woundlocationname, t.name as tissuename,
                                      v.name as versionname, i.img_file as imagefile, i.image_id as imageid, u.username
                                      FROM wound w
                                      INNER JOIN image i ON i.image_id = w.image_id
                                      INNER JOIN wound_category wc ON wc.wound_category_id = w.wound_category_id
                                      INNER JOIN wound_location wl ON wl.wound_location_id = w.wound_location_id
                                      INNER JOIN tissue t ON t.tissue_id = w.tissue_id
                                      INNER JOIN version v ON v.version_id = w.version_id
                                        INNER JOIN useracc u ON u.user_id = w.user_id
                                        WHERE " + searchedsection + " LIKE '%" + searchedobj + "%'";


            ViewBag.showhidecheckchecker = 1;
            Debug.WriteLine(listinput);
            ViewBag.keyword = searchedobj;
            ViewBag.selection = searchedsection;
            List<WoundRecord> list = DBUtl.GetList<WoundRecord>(listinput);



            list = list.GroupBy(x => x.woundname).Select(y => y.FirstOrDefault())?.ToList();
            return View("TheWounds", list);
        }
        #endregion
    }
}
