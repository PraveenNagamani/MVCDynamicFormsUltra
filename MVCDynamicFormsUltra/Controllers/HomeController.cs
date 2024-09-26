using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using MVCDynamicFormsUltra.DBContext;
using MVCDynamicFormsUltra.Models;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Data;
using System.Diagnostics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MVCDynamicFormsUltra.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string Mongoconnstring = "";
        private readonly string Orclconnstring = "";
        private readonly IConnectionMultiplexer _Redis ;
        private readonly ILogger _logger;
        DBConnect dmvc = null;
        private readonly EFClasses _EFClass;
        private readonly LaunchAPI API;
        
        string ErrMsg = "";
        public HomeController(IConfiguration config, ILogger<HomeController> logger, EFClasses _EFClass, LaunchAPI launchAPI, IConnectionMultiplexer Redis)
        {
            configuration = config;
            Mongoconnstring = configuration["ConnectionStrings:MongoConnection"];
            Orclconnstring = configuration["ConnectionStrings:OrclConnection"];
            _Redis = Redis;
            _logger = logger;
            this._EFClass = _EFClass;
            dmvc =  new DBConnect(_logger);
            API = launchAPI;
        }

        public IActionResult Index()
        {

            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("inside privacy");
            string[,] Filterparams = new string[1, 3];
            Filterparams[0, 0] = "random_no"; Filterparams[0, 1] = "Eq"; Filterparams[0, 2] = "1313068";

            ViewBag.jsonstring = dmvc.GetNoSQLBson(Mongoconnstring, "random_no_data", ref ErrMsg, Filterparams);
            _logger.LogWarning($" bson data received {ViewBag.jsonstring} ");
            _logger.LogError($" bson data received showing error {ViewBag.jsonstring} ");
            return View();
        }

        public IActionResult Twitty()
        {
            List<string> tlist = new List<string>();
            tlist.Add("Modi"); tlist.Add("Dhoni"); tlist.Add("US Election"); tlist.Add("Work Life Balance"); tlist.Add("HYDRAA"); tlist.Add("Himanchal Floods");
            ViewBag.Trends = tlist;

            string[,] Filterparams = new string[1, 3];
            Filterparams[0, 0] = "model"; Filterparams[0, 1] = "Eq"; Filterparams[0, 2] = "Sprinter";

            var db = _Redis.GetDatabase();
            string result = db.StringGet("sample_bicycle:1004");

            return View();
        }
        public IActionResult Redis()
        {
            var db = _Redis.GetDatabase();
            string TRID = DateTime.Now.ToString("yyyyMMddHHmmss");
            db.StringSet("RedisTest" + TRID, Guid.NewGuid().ToString());

            string? returnstr = db.StringGet("RedisTest" + TRID);
            ViewBag.jsonstring = JsonConvert.SerializeObject(returnstr);

            return View("Privacy");
        }
                
        public IActionResult DataEntry(string paramdata = "")
        {

           
            string userName =  HttpContext.Session.GetString("UserName");
            if(userName == null) {
                return RedirectToAction("Error",new { ErrMsg = "OOPS !! Session Expired"});

            }            
            else if (paramdata != "")
            {
                Dictionary<string, string> dictparams = new Dictionary<string, string>();
                dictparams = JsonConvert.DeserializeObject<Dictionary<string, string>>(paramdata);
                dictparams.Add("#USERID#",userName);

                string value = HttpContext.Session.GetString("sess_nform");

                NformController nc = new NformController(configuration, _logger, _EFClass);
                var model = nc.Cascade("CustDetails",value, ref ErrMsg, dictparams);
                ViewBag.Nform = model; //TempData["Nform"] = model;
                if (ErrMsg != "") { _logger.LogError(ErrMsg); return RedirectToAction("Error", new { ErrMsg = "InValid Model Data !!" + this.ErrMsg }); }

                if (ModelState.IsValid)
                {
                    HttpContext.Session.SetString("sess_nform", JsonConvert.SerializeObject(model));
                    return View(model);
                }
                else
                {
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key].Errors;

                        foreach (var error in errors)
                        {
                            ErrMsg += key + ": " + error.ErrorMessage;
                        }
                    }
                    return RedirectToAction("Error", new { ErrMsg = "InValid Data !!" + this.ErrMsg });
                }

            }
            else
            {
                NformController nc = new NformController(configuration, _logger, _EFClass);
                var model = nc.LoadControls("CustDetails", ref ErrMsg,userName);
                
                if (ModelState.IsValid) {
                    HttpContext.Session.SetString("sess_nform", JsonConvert.SerializeObject(model));
                    
                    return View(model); 
                } else {
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key].Errors;
                        
                        foreach (var error in errors)
                        {
                            ErrMsg += key + ": " + error.ErrorMessage;
                        }
                    }
                    return RedirectToAction("Error", new { ErrMsg = "InValid Data !!" + this.ErrMsg }); 
                }

            }



        }


        [HttpPost]
        public IActionResult CascadeDataEntry(IFormCollection data)
        {

            Dictionary<string, string> cascadeparams = new Dictionary<string, string>();
            foreach (var key in data.Keys)
            {
                cascadeparams.Add("#" + key.ToUpper() + "#", data[key]);
            }

            string jsonparams =  JsonConvert.SerializeObject(cascadeparams);
            return RedirectToAction("DataEntry", new { paramdata = jsonparams } );
        }

        [HttpPost]
        public IActionResult ProfileView(string buttonid, string username, string emailid)
        {
            if (buttonid == "btnsave")
            {
                CreateProfile(username, emailid);
                if (ErrMsg != string.Empty)
                {

                    return RedirectToAction("Error");
                }
            }

            List<Profile> profilelist = new List<Profile>();
            profilelist = SearchProfile(username);

            if (buttonid == "btnlogin" || buttonid == "btnsave")
            {
                if (profilelist.Count > 0)
                {
                    HttpContext.Session.SetString("UserName", username);
                    return RedirectToAction("DataEntry");
                }
            }

            return View(profilelist);

        }

        public async Task<bool> CreateProfile(string username, string emailid)
        {
            IFormFile image = Request.Form.Files["Photo"];
            byte[] imageBytes = null;
            if (image != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.CopyTo(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
            }

            try
            {
                if(API != null)
                {
                    string[,] sqlparams = null;
                   
                    Dictionary<string,string> d = new Dictionary<string,string>();
                    d.Add("USERNAME", username); d.Add("EMAILID", emailid); 
                    string response = "";
                    response = await API.CallAPI("Customer",false, d, imageBytes);
                    return response.Contains("Success") ? true : false;
                }
                else
                {
                    Profile p = new Profile() { username = username, emailid = emailid, Photo = imageBytes };

                    _EFClass.Add(p);
                    _EFClass.SaveChanges();
                    return true;
                }
                
            }
            catch (Exception e)
            {
                ErrMsg = e.GetBaseException().ToString();
                _logger.LogCritical("CreateProfile : " + ErrMsg);
                return false;
            }

        }

        public List<Profile> SearchProfile(string username)
        {
            try
            {
                var profiles = _EFClass.Profiles
                .Where(u => u.username == username)
                .ToList();
                return profiles;
            }
            catch (Exception e)
            {
                ErrMsg = e.GetBaseException().ToString();
                _logger.LogCritical("SearchProfile : " + ErrMsg);
                return null;
            }

        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string ErrMsg)
        {
            if(ErrMsg == null)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
            else
            {
                return View(new ErrorViewModel { RequestId = ErrMsg });
            }
            
        }

        public IActionResult ProfileView()
        {
            return View();
        }
    }
}
