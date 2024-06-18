using TimesheetDEV.Models;

namespace TimesheetDEV.ViewModels
{
    public class SupervisorsViewModel
    {
        public List<ActiveEmployeeModel> ActiveEmployees { get; set; } = new List<ActiveEmployeeModel>();
        public PeopleModel Supervisor { get; set; } = new PeopleModel();
    }
}