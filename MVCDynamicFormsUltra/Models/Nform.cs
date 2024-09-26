using System.ComponentModel.DataAnnotations.Schema;

namespace MVCDynamicFormsUltra.Models
{
    
    [Table("cfgwizardpage")]
    public class Nform
    {
        
        public required string WIZSTEPKEY { get; set; }
        public required string TABLENAME { get; set; }
        public required string COLUMNNAME { get; set; }
        public int ?SORTORDER { get; set; }
        public string ?OBJECTTYPE { get; set; }
        public string ?DISPLAYTEXT { get; set; }
        public string ?SQLTEXT { get; set; }
        public string ?CASCADEID { get; set; }
        public string ?CONTROLID { get; set; }
        public string ?DISPLAYMSG { get; set; }
        public string ?CSSCLASS { get; set; }
        public string ?CASCADINGCSS { get; set; }
        [NotMapped]
        public string ?DisableControl { get; set; }  

    }
}
