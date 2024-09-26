using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace MVCDynamicFormsUltra.Models
{
    [Table("USERPROFILE")]
    public class Profile
    {

        [Required , Column("USERNAME") ,NotNull , Key]
        
        public string username { get; set; }


        [Required, NotNull, EmailAddress(ErrorMessage = "invalid email format")]
        [Column("EMAIL")]
        public string emailid { get; set; }

        [Column("PHOTO")]
        public byte[]? Photo { get; set; }
    }
}
