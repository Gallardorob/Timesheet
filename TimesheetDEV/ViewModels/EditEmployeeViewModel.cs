namespace TimesheetDEV.ViewModels
{
    public class EditEmployeeViewModel
    {
        public int LOG_ID { get; set; }
        public int ID { get; set; }
        public string First_Name { get; set; } = string.Empty;
        public string Last_Name { get; set; } = String.Empty;
        public DateOnly? CURRENT_DATE { get; set; }
        public TimeOnly? CLOCKED_IN { get; set; }
        public TimeOnly? CLOCKED_OUT { get; set; }
        public TimeSpan TotalTimeSpan { get; set; }
    }
}