using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Xml.Linq;
using TimesheetDEV.Models;
using TimesheetDEV.ViewModels;

namespace TimesheetDEV.Controllers
{
    public class SupervisorController : Controller
    {
        private readonly ILogger<SupervisorController> _logger;
        private readonly IConfiguration _configuration; 

        public SupervisorController(ILogger<SupervisorController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel _loginUser)
        {
            // Check the state of the model
            if (!ModelState.IsValid)
            {
                return View(_loginUser);
            }

            // Model looks good we start checking the users credentials. 
            try
            {
                var connString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
                string dbName = _configuration.GetValue<string>("DatabaseName");

                PeopleModel supervisorUser = new PeopleModel();
                List<ActiveEmployeeModel> activeEmployees = new List<ActiveEmployeeModel>();

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // Grab user's current record from DB.
                    string sqlText = $"Select Distinct * FROM {dbName}.[People] WHERE ID = '{_loginUser.LoginID}'";
                    SqlCommand cmd = new SqlCommand(sqlText, con);
                    var reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            supervisorUser = new PeopleModel
                            {
                                ID = (int)reader["ID"],
                                First_Name = reader["First_Name"].ToString(),
                                Last_Name = reader["Last_Name"].ToString(),
                                Password = reader["PASSWORD"].ToString(),
                                IsActive = (bool)reader["ISACTIVE"],
                                Supervisor = (bool)reader["Supervisor"]
                            };

                            if(!supervisorUser.Supervisor)
                            {
                                ViewBag.Message = $"You are not a supervisor. You cannot access this page.";
                                return View();
                            }

                            // Check if credentials match the users input information.
                            else if (_loginUser.LoginID == supervisorUser.ID.ToString() && _loginUser.LoginPassword == supervisorUser.Password)
                            {
                                activeEmployees = GetActiveEmployees(con);

                                SupervisorsViewModel supervisorsViewModel = new SupervisorsViewModel()
                                {
                                    ActiveEmployees = activeEmployees,
                                    Supervisor = supervisorUser
                                };

                                con.Close();
                                return View("Timesheet", supervisorsViewModel);
                            }
                            else
                            {
                                ViewBag.Message = $"Your ID or password did not match. Please try again.";
                                return View();
                            }
                        }
                        con.Close();
                        return View("SuccessClockIn", supervisorUser);
                    }
                    else
                    {
                        // Could not find a record in DB. Pass a message to tell the user.
                        con.Close();
                        ViewBag.Message = $"User ID {_loginUser.LoginID} was not found. Please try again.";
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        public IActionResult Timesheets()
        {
            return View();
        }

        [HttpPost]
        public ActionResult EmployeeTimesheet(int currentID)
        {
            var tempEditList = new List<EditEmployeeViewModel>();
            EditEmployeeListViewModel editEmployeeRow = new EditEmployeeListViewModel();

            try
            {
                var connString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
                string dbName = _configuration.GetValue<string>("DatabaseName");

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    string sqlText = $"Select * FROM {dbName}.[Timesheet] WHERE ID = '{currentID}' ORDER BY LOG_ID DESC";
                    SqlCommand cmd = new SqlCommand(sqlText, con);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var tempEdit = new EditEmployeeViewModel
                        {
                            LOG_ID = (int)reader["LOG_ID"],
                            ID = (int)reader["ID"],
                            First_Name = reader["First_Name"].ToString(),
                            Last_Name = reader["Last_Name"].ToString(),
                            CURRENT_DATE = Convert.IsDBNull(reader["START_TIMESTAMP"]) ? null : DateOnly.FromDateTime((DateTime)reader["START_TIMESTAMP"]),
                            CLOCKED_IN = Convert.IsDBNull(reader["START_TIMESTAMP"]) ? null : TimeOnly.FromDateTime((DateTime) reader["START_TIMESTAMP"]),
                            CLOCKED_OUT = Convert.IsDBNull(reader["END_TIMESTAMP"]) ? null : TimeOnly.FromDateTime((DateTime) reader["END_TIMESTAMP"]),
                            TotalTimeSpan = (Convert.IsDBNull(reader["START_TIMESTAMP"]) || Convert.IsDBNull(reader["END_TIMESTAMP"]))
                                ? TimeSpan.FromSeconds(0) :
                                GetTotalTime((DateTime) reader["START_TIMESTAMP"], (DateTime)reader["END_TIMESTAMP"])
                        };
                        tempEditList.Add(tempEdit);
                    }
                    con.Close();

                    editEmployeeRow = new EditEmployeeListViewModel
                    {
                        EditEmployeeRow = tempEditList
                    };
                }
            }            
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred. This has been logged.\n{ex.Message}";
                return View("Error");
            }

            return View(editEmployeeRow);
        }

        [HttpPost]
        public ActionResult EditEmployeeShift(int logId)
        {
            EditEmployeeViewModel editEmployee = new EditEmployeeViewModel();
            try
            {
                var connString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
                string dbName = _configuration.GetValue<string>("DatabaseName");

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    string sqlText = $"Select * FROM {dbName}.[Timesheet] WHERE LOG_ID = '{logId}'";
                    SqlCommand cmd = new SqlCommand(sqlText, con);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        editEmployee = new EditEmployeeViewModel
                        {
                            LOG_ID = (int)reader["LOG_ID"],
                            ID = (int)reader["ID"],
                            First_Name = reader["First_Name"].ToString(),
                            Last_Name = reader["Last_Name"].ToString(),
                            CURRENT_DATE = Convert.IsDBNull(reader["START_TIMESTAMP"]) ? null : DateOnly.FromDateTime((DateTime)reader["START_TIMESTAMP"]),
                            CLOCKED_IN = Convert.IsDBNull(reader["START_TIMESTAMP"]) ? null : TimeOnly.FromDateTime((DateTime)reader["START_TIMESTAMP"]),
                            CLOCKED_OUT = Convert.IsDBNull(reader["END_TIMESTAMP"]) ? null : TimeOnly.FromDateTime((DateTime)reader["END_TIMESTAMP"])
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred. This has been logged.\n{ex.Message}";
                return View("Error");

            }
            return View("EditEmployee", editEmployee);
        }

        [HttpPost]
        public ActionResult DeleteEmployeeShift(int logID)
        {
            try
            {
                var connString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
                string dbName = _configuration.GetValue<string>("DatabaseName");

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    string sqlText = $"DELETE FROM {dbName}.[Timesheet] WHERE LOG_ID = '{logID}'";
                    SqlCommand cmd = new SqlCommand(sqlText, con);
                    var reader = cmd.ExecuteReader();
                    con.Close();
                }
            }
            catch (Exception ex) {
                ViewBag.ErrorMessage = $"An error occurred. This has been logged.\n{ex.Message}";
                return View("Error");
            }

            return View("Login");
        }

        [HttpPost]
        public ActionResult SaveEmployeeShift(EditEmployeeViewModel eevModel)
        {
            try
            {
                var connString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
                var dbName = _configuration.GetValue<string>("DatabaseName");

                string[] arrDate = eevModel.CURRENT_DATE.ToString().Split('/');
                string sMonth = (arrDate[0].Length == 1) ? "0" + arrDate[0] : arrDate[0].ToString();
                string sDay = (arrDate[1].Length == 1) ? "0" + arrDate[1] : arrDate[1].ToString();
                string sYear = arrDate[2].ToString();
                string sDate = $"{sYear}-{sMonth}-{sDay}";

                string sCInTime = eevModel.CLOCKED_IN.Value.ToTimeSpan().ToString();
                string sCOutTime = eevModel.CLOCKED_OUT.Value.ToTimeSpan().ToString();

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    string sqlText = $"UPDATE {dbName}.Timesheet SET START_TIMESTAMP = '{sDate} {sCInTime}', END_TIMESTAMP = '{sDate} {sCOutTime}' WHERE LOG_ID = {eevModel.LOG_ID}";
                    SqlCommand cmd = new SqlCommand(sqlText, con);
                    var reader = cmd.ExecuteReader();
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred. This has been logged.\n{ex.Message}";
                return View("Error");
            }

            return View("Login");
        }

        private List<ActiveEmployeeModel> GetActiveEmployees(SqlConnection _conn)
        {
            var tempActiveList = new List<ActiveEmployeeModel>();
            string dbName = _configuration.GetValue<string>("DatabaseName");

            string sqlText = $"Select ID, First_Name, Last_Name FROM {dbName}.[People] where IsActive = '1'";
            SqlCommand cmd = new SqlCommand(sqlText, _conn);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var tempEmp = new ActiveEmployeeModel
                {
                    ID = (int) reader["ID"],
                    First_Name = reader["First_Name"].ToString(),
                    Last_Name = reader["Last_Name"].ToString()
                };
                tempActiveList.Add(tempEmp);
            }
            return tempActiveList;
        }

        private TimeSpan GetTotalTime(DateTime clockIn, DateTime clockOut)
        {
            // Convert clocked in time to seconds.
            int ClockInHours = clockIn.Hour;
            int ClockInMinutes = clockIn.Minute;
            int ClockInSeconds = clockIn.Second;
            int ClockInHrsToSeconds = ClockInHours * 60 * 60;
            int ClockInMinsToSeconds = ClockInMinutes * 60;
            int ClockInTotalSeconds = ClockInHrsToSeconds + ClockInMinsToSeconds + ClockInSeconds;

            // Convert clocked out time to seconds.
            int ClockOutHours = clockOut.Hour;
            int ClockOutMinutes = clockOut.Minute;
            int ClockOutSeconds = clockOut.Second;
            int ClockOutHrsToSeconds = ClockOutHours * 60 * 60;
            int ClockOutMinsToSeconds = ClockOutMinutes * 60;
            int ClockOutTotalSeconds = ClockOutHrsToSeconds + ClockOutMinsToSeconds + ClockOutSeconds;

            // Subtract clocked in and clocked out times. 
            int TotalSeconds = Math.Abs(ClockInTotalSeconds - ClockOutTotalSeconds);

            // Convert total seconds to TimeSpan. 
            TimeSpan ts = TimeSpan.FromSeconds(TotalSeconds);

            return ts;
        }

        //private SqlDataReader dataReader()
        //{
        //        var connString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
        //        var dbName = _configuration.GetValue<string>("DatabaseName");
        //    try
        //    {
        //        using (SqlConnection con = new SqlConnection(connString))
        //        {
        //            con.Open();
        //            //string sqlText = $"UPDATE {dbName}.Timesheet SET START_TIMESTAMP = '{sDate} {sCInTime}', END_TIMESTAMP = '{sDate} {sCOutTime}' WHERE LOG_ID = {eevModel.LOG_ID}";
        //            SqlCommand cmd = new SqlCommand(sqlText, con);
        //            var reader = cmd.ExecuteReader();
        //            con.Close();
        //        }
        //    } catch (Exception ex) { 
        //    }
        //    return 
        //}
    }
}