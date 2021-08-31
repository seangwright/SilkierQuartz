using System.ComponentModel.DataAnnotations;

namespace SilkierQuartz.Models
{
    public class AuthenticateViewModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool IsPersist { get; set; }
    }
}