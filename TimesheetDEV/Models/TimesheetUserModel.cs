namespace TimesheetDEV.Models
{
    public class TimesheetUserModel
    {
        public int LOG_ID { get; set; }
        public int ID { get; set; }
        public string First_Name { get; set; } = string.Empty;
        public string Last_Name { get; set; } = string.Empty;
        public DateTime? START_TIMESTAMP { get; set; }
        public DateTime? END_TIMESTAMP { get; set; }
    }
}
