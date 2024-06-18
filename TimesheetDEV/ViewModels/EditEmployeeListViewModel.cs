using TimesheetDEV.ViewModels;

namespace TimesheetDEV.Models
{
    // A Model list of all current employees
    public class EditEmployeeListViewModel
    {
        public List<EditEmployeeViewModel> EditEmployeeRow { get; set; } = new List<EditEmployeeViewModel>();
    }
}
