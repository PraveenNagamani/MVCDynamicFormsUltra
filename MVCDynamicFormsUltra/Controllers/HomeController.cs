using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
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
using System.Net;
using System.Security.Claims;
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
        DBConnect dmvc ;
        private readonly EFClasses _EFClass;
        private readonly LaunchAPI API;
        
        string ErrMsg = "";
        public HomeController(IConfiguration config, ILogger<HomeController> logger, EFClasses _EFClass, LaunchAPI launchAPI, IConnectionMultiplexer Redis, DBConnect db)
        {
            configuration = config;
            Mongoconnstring = configuration["ConnectionStrings:MongoConnection"];
            Orclconnstring = configuration["ConnectionStrings:OrclConnection"];
            _Redis = Redis;
            _logger = logger;
            this._EFClass = _EFClass;
            dmvc =  db;
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

        [HttpGet]
        public IActionResult Twitty()
        {
            List<string> tlist = new List<string>();
            string userName = HttpContext.Session.GetString("UserName");
            if (userName == null)
            {
                return RedirectToAction("Error", new { ErrMsg = "OOPS !! Session Expired" });

            }
            else
            {
                if (userName == "praveen") { userName = "userid1"; }

            }

            tlist.Add("Modi"); tlist.Add("Dhoni"); tlist.Add("US Election"); tlist.Add("Work Life Balance"); tlist.Add("HYDRAA"); tlist.Add("Himanchal Floods"); tlist.Add("SRK");
            ViewBag.Trends = tlist;

            var db = _Redis.GetDatabase();
            string query = $"user:{userName}:messages_sorted";
            RedisValue[] messages = db.SortedSetRangeByScore(query, order: Order.Descending);
           
            List<Tweet> tweets = new List<Tweet>();
            foreach (var msg in messages)
            {
                 HashEntry[] he =  db.HashGetAll($"message:{msg}");
                var d = he.ToDictionary(
                    entry => (string?) entry.Name,
                    entry => (string?) entry.Value
                    );

                 d.TryGetValue("Content", out string tContent);
                d.TryGetValue("title", out string ttitle);
                d.TryGetValue("user", out string tauthor);
                

                tweets.Add(new Tweet
                {
                    Content = tContent,
                    Title = ttitle,
                    author = tauthor
                });
                
            }
            return View(tweets);
        }

        [HttpPost]
        public IActionResult Twitty(IFormCollection data)
        {

            if (data.Keys.Contains("Control"))
            {
                RedirectToAction("SetPostLikeCount", "RedisConnect");
            }
            return PartialView();
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

                NformController nc = new NformController(configuration, _logger, _EFClass,dmvc);
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
                NformController nc = new NformController(configuration, _logger, _EFClass,dmvc);
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
            List<Profile> profilelist = new List<Profile>();

            if (buttonid == "btnsave")
            {
                CreateProfile(username, emailid);
                if (ErrMsg != string.Empty)
                {

                    return RedirectToAction("Error");
                }
                
            }
            else
            {
                if (buttonid == "btnlogin" || buttonid == "btnsave")
                {
                    if (!GetProfileFromCookie(username))
                    {
                        profilelist = SearchProfile(username);
                    
                        if (profilelist.Count > 0)
                        {
                            HttpContext.Session.SetString("UserName", username);
                            SetProfileCookie(username);
                            //return RedirectToAction("DataEntry");
                            return RedirectToAction("Twitty");
                        }
                        else
                        {
                            return RedirectToAction("Error",new { ErrMsg = "No User Found" });
                        }
                    }
                    else
                    {
                        HttpContext.Session.SetString("UserName", username);
                        //return RedirectToAction("DataEntry");
                        return RedirectToAction("Twitty");
                    }
                }
            }

            
            return View(profilelist);

        }
        public bool SetProfileCookie(string username)
        {
            CookieOptions cookieoptions = new CookieOptions
            {
                Expires = DateTime.Now.AddMonths(12)
            };

            HttpContext.Response.Cookies.Append("UserName", username, cookieoptions);

            return GetProfileFromCookie(username);

        }

        internal bool GetProfileFromCookie(string username)
        {

            string user = HttpContext.Request.Cookies["UserName"];
            if(string.IsNullOrEmpty(user)) { return false; }
            if(user == username) {
                

                var claims = new List<Claim>
                {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "User")
                };

                // Create claims identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create authentication properties (e.g., for persistence)
                var authProperties = new AuthenticationProperties
                {
                    //IsPersistent = true, // Remember the user
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Set expiration
                };

                // Sign in the user (this sets User.Identity.IsAuthenticated = true)
                 HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);


                return true; 
            }
            return false;
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
                    if (response != null) {
                        if (response.Contains("Success")) { SetProfileCookie(username); return true; } else {  return false; }
                        
                    }
                    return  false;
                }
                else
                {
                    Profile p = new Profile() { username = username, emailid = emailid, Photo = imageBytes };

                    _EFClass.Add(p);
                    _EFClass.SaveChanges();
                    SetProfileCookie(username);
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

        public IActionResult LogOut()
        {
            //HttpContext.Session.Clear();
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("ProfileView");
        }
    }
}
