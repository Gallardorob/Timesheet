namespace TimesheetDEV.Models
{
    public class PeopleModel
    {
        public int ID { get; set; }
        public string First_Name { get; set; } = string.Empty;
        public string Last_Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public bool Supervisor { get; set; } = false;

    }
}