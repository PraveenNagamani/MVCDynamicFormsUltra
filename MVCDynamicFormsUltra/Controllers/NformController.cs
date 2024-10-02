using Microsoft.AspNetCore.Mvc;
using MVCDynamicFormsUltra.DBContext;
using MVCDynamicFormsUltra.Models;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace MVCDynamicFormsUltra.Controllers
{
    public class NformController : Controller
    {
        private readonly string Orclconnstring = "";
        private readonly EFClasses eFClasses;

        //ConnectDBMVC.DBMethods dmvc = new ConnectDBMVC.DBMethods();

        private string ErrMsg = "";

        private readonly ILogger logger;
        DBConnect dBConnect;

        public NformController(IConfiguration config, ILogger logger,EFClasses eFClasses, DBConnect db)
        {
            this.eFClasses = eFClasses;
            Orclconnstring = config["ConnectionStrings:OrclConnection"];
            this.logger = logger;
            dBConnect = db;
        }

        public NformController() { }
        public IActionResult LoadData()
        {
            //try
            //{
            //    string sortColumn = "", sortColumnDirection = "", searchValue = "";
            //    int recordsTotal = 0;
            //    try
            //    {
            //        var draw = HttpContext.Request.Form["draw"].FirstOrDefault();

            //        // Skip number of Rows count  
            //        var start = Request.Form["start"].FirstOrDefault();

            //        // Paging Length 10,20  
            //        var length = Request.Form["length"].FirstOrDefault();

            //        // Sort Column Name  
            //        sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

            //        // Sort Column Direction (asc, desc)  
            //        sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();

            //        // Search Value from (Search box)  
            //        searchValue = Request.Form["search[value]"].FirstOrDefault();

            //        //Paging Size (10, 20, 50,100)  
            //        int pageSize = length != null ? Convert.ToInt32(length) : 0;

            //        int skip = start != null ? Convert.ToInt32(start) : 0;


            //    }
            //    catch (Exception ex)
            //    {

            //    }


            //    var customerData = dmvc.GetDatatable("select customerid,firstname from pascustomers", Orclconnstring, ref ErrMsg);

            //    //Sorting  


            //    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
            //    {
            //        customerData = dmvc.GetDatatable("select * from pascustomers order by customerid", Orclconnstring, ref ErrMsg);
            //    }
            //    //Search  
            //    if (!string.IsNullOrEmpty(searchValue))
            //    {
            //        customerData = dmvc.GetDatatable("select * from pascustomers where firstname = '" + searchValue + "'", Orclconnstring, ref ErrMsg);
            //    }

            //    //total number of rows counts   
            //    recordsTotal = customerData.Rows.Count;
            //    //Paging   
            //    var data = customerData;
            //    //Returning Json Data
            //    var jsresult = new
            //    {

            //        recordsFiltered = recordsTotal,
            //        recordsTotal = recordsTotal,
            //        data = data
            //    };

            //    ContentResult c = new ContentResult { Content = JsonConvert.SerializeObject(data, Formatting.Indented), ContentType = "application/json" };
            //    return c;
            //    //return Json( JsonConvert.SerializeObject(jsresult, Formatting.Indented));

            //    //return View();

            //}
            //catch (Exception)
            //{
            //    throw;
            //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            //}
            return View();

        }

        public List<Nform> LoadControls(string wizstepkey, ref string ErrMsg, string userid = "")
        {
            List<Nform> controls = null;

            string[,] sqlparams = null;

            //DBConnect dBConnect = new DBConnect(logger);

            sqlparams = dBConnect.prepareparamsbyarg(ref ErrMsg,"#USERID#", userid);
            try
            {
                controls = eFClasses.Nforms
                .Where(u => u.WIZSTEPKEY == wizstepkey).OrderBy(u => u.SORTORDER)
                .ToList();

                foreach(Nform ctrl in controls)
                {
                    if (ctrl.CONTROLID == null && ctrl.SQLTEXT != null)
                    {

                        if (ctrl.OBJECTTYPE == "DropDown" || ctrl.OBJECTTYPE == "CheckBox")
                        {
                            ctrl.SQLTEXT = dBConnect.BindDropDown(ctrl.SQLTEXT, Orclconnstring, sqlparams, ref ErrMsg);
                        }
                        else
                        {
                            if (ctrl.SQLTEXT.ToLower().StartsWith("select") || ctrl.SQLTEXT.ToLower().StartsWith("with"))
                            {
                                ctrl.SQLTEXT = dBConnect.ExecuteScalar(ctrl.SQLTEXT, Orclconnstring, sqlparams, ref ErrMsg);
                            }
                        }

                    }
                    else
                    {
                        ctrl.DisableControl = "disabled";
                        ctrl.SQLTEXT = null;
                    }
                    
                }

            }
            catch (Exception e)
            {
                ErrMsg = e.GetBaseException().ToString();
                logger.LogCritical("LoadControls : " + ErrMsg);
                
            }

            return controls;
        }

        public List<Nform> Cascade(string wizstepkey, string serializemodel, ref string ErrMsg, Dictionary<string, string> dictparams = null)
        {
            List<Nform> controls = null;
            string[,] sqlparams = null;
            //DBConnect dBConnect = new DBConnect(logger);
            try
            {
                sqlparams = dBConnect.prepareparmasbydictionary(ref ErrMsg, dictparams);
                string ctrlid = dictparams["#CONTROLID#"].ToUpper();

                controls = serializemodel == null ? default(List<Nform>) : JsonConvert.DeserializeObject<List<Nform>>(serializemodel);

                var mastercontrols = eFClasses.Nforms.Where(u => u.WIZSTEPKEY == wizstepkey);

                foreach (Nform ctrl in controls)
                {
                    if(ctrl.CONTROLID != null)
                    {
                        foreach (string subctrlid in ctrl.CONTROLID.Split(","))
                        {
                            
                            if (subctrlid == ctrlid)
                            {

                                ctrl.SQLTEXT = mastercontrols.Where(v => v.CONTROLID.ToUpper().Contains(subctrlid.ToUpper())).Where(u => u.COLUMNNAME == ctrl.COLUMNNAME).First().SQLTEXT;

                                if (ctrl.SQLTEXT == null) continue;
                                if (ctrl.OBJECTTYPE == "DropDown" || ctrl.OBJECTTYPE == "CheckBox")
                                {
                                    ctrl.SQLTEXT = dBConnect.BindDropDown(ctrl.SQLTEXT, Orclconnstring, sqlparams, ref ErrMsg);

                                    if (ErrMsg != "") { logger.LogError(ctrl.SQLTEXT + ErrMsg); ErrMsg = ""; }
                                }
                                else
                                {
                                    if (ctrl.SQLTEXT.ToLower().StartsWith("select") || ctrl.SQLTEXT.ToLower().StartsWith("with"))
                                    {
                                        ctrl.SQLTEXT = dBConnect.ExecuteScalar(ctrl.SQLTEXT, Orclconnstring, sqlparams, ref ErrMsg);
                                    }
                                }

                                ctrl.DisableControl = ""; 
                            }
                            else
                            {
                                string result = "";
                                preserveControlvalues(ctrl, dictparams, out result);
                                ctrl.SQLTEXT = result;
                            }
                            break;
                        }
                    }
                    else
                    {
                        string result = "";
                        preserveControlvalues( ctrl, dictparams, out result);
                        ctrl.SQLTEXT = result;
                        
                    }
                    

                }

            }
            catch (Exception e) {
                ErrMsg = e.GetBaseException().ToString();
                logger.LogCritical("Cascade : " + ErrMsg);
                
            }

            

            return controls;
        }

        internal void preserveControlvalues( Nform ctrl, Dictionary<string,string> dictparams, out string sqltext)
        {
            //DBConnect dBConnect = new DBConnect(logger);
            string ErrMsg = "";
            string[,] sqlparams = dBConnect.prepareparmasbydictionary(ref ErrMsg, dictparams);

            if (ctrl.OBJECTTYPE == "DropDown" || ctrl.OBJECTTYPE == "CheckBox")
            {
                ctrl.SQLTEXT = dBConnect.BindDropDown(ctrl.SQLTEXT, Orclconnstring, sqlparams, ref ErrMsg, dictparams.ContainsKey("#" + ctrl.COLUMNNAME.ToUpper() + "#") == true ? dictparams["#" + ctrl.COLUMNNAME.ToUpper() + "#"].ToString() : "");
                
            }
            else
            {
                ctrl.SQLTEXT = dictparams.ContainsKey("#" + ctrl.COLUMNNAME.ToUpper() + "#") == true ? dictparams["#" + ctrl.COLUMNNAME.ToUpper() + "#"].ToString() : "";
            }
            sqltext = ctrl.SQLTEXT;
        }
    }
}
