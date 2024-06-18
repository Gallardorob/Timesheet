using System.ComponentModel.DataAnnotations;

namespace TimesheetDEV.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage ="Please enter your ID.")]
        [Display(Name = "Enter ID ")]
        public string LoginID { get; set; } = String.Empty;

        [Required(ErrorMessage = "Please enter a valid password")]
        [Display(Name = "Enter Password ")]
        public string LoginPassword { get; set; } = String.Empty;
    }
}