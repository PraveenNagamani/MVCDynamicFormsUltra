using Microsoft.AspNetCore.Mvc;
using MVCDynamicFormsUltra.DBContext;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static MVCDynamicFormsUltra.DBContext.DBConnect;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : Controller
    {
        public readonly IConfiguration Configuration;
        private readonly string orclconnection;
        string ErrMsg="";
        public CustomerController(IConfiguration configuration)
        {
            Configuration = configuration;
            orclconnection =  configuration["ConnectionStrings:OrclConnection"] ?? throw new ArgumentNullException("Orcl Connection string not found");
        }
        [HttpPost(Name = "Create")]
        public async Task<string> CreateProfile(string username, string emailid, IFormFile filecontent)
        {
            
            return await insertcustomerprofile(username, emailid, filecontent);
        }

        private async Task<string> insertcustomerprofile(string username, string emailid, IFormFile filecontents)
        {
            
            //List<OracleParameter> orclparams = new List<OracleParameter>();
            DBConnect dBConnect = new DBConnect();
                     


            byte[] imageBytes = null;
            if (filecontents != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    filecontents.CopyTo(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
            }

            OracleParameter[] oraparams = new OracleParameter[]
           {
                //new OracleParameter("USERNAME", OracleDbType.Varchar2, username, ParameterDirection.Input),
                //new OracleParameter("EMAILID", OracleDbType.Varchar2, emailid, ParameterDirection.Input),
                new OracleParameter("PHOTO",OracleDbType.Blob, imageBytes , ParameterDirection.Input)
            };

            string[,] sqlparams = null;
            sqlparams = dBConnect.prepareparamsbyarg(ref ErrMsg, "#USERNAME#", username, "#EMAILID#", emailid);

            //dBConnect.addByteArrayToParam(ref ErrMsg, ref orclparams, "#PHOTO#", imageBytes);

            ExecuteNonQueryAsyncResult e = new ExecuteNonQueryAsyncResult();
            e = await dBConnect.ExecuteNonQueryAsync("insert into USERPROFILE(USERNAME,EMAIL,PHOTO) values('#USERNAME#','#EMAILID#',:PHOTO)", orclconnection,sqlparams, oraparams, ErrMsg);

            return string.IsNullOrEmpty(e.ErrMsg) ? "Success : " + e.rowsAffected : e.ErrMsg;
        }
    }
}
