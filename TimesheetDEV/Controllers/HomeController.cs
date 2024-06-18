using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Diagnostics;
using TimesheetDEV.Models;
using TimesheetDEV.ViewModels;

namespace TimesheetDEV.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
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

                PeopleModel currentUser = new PeopleModel();

                using (SqlConnection con = new SqlConnection(connString))
                {
                    string successfulMessage = string.Empty;
                    con.Open();

                    // Grab user's current record from DB.
                    string sqlText = $"Select Distinct * FROM {dbName}.[People] WHERE ID = '{_loginUser.LoginID}'";
                    SqlCommand cmd = new SqlCommand(sqlText, con);
                    var reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            currentUser = new PeopleModel
                            {
                                ID = (int) reader["ID"],
                                First_Name = reader["First_Name"].ToString(),
                                Last_Name = reader["Last_Name"].ToString(),
                                Password = reader["PASSWORD"].ToString(),
                                IsActive = (bool) reader["ISACTIVE"]
                            };

                            // Check if credentials match the users input information.
                            // If they do then they can clock in or clock out. 
                            if (_loginUser.LoginID == currentUser.ID.ToString() && _loginUser.LoginPassword == currentUser.Password)
                            {
                                successfulMessage = ClockInUser(con, currentUser);
                            }
                            else
                            {
                                ViewBag.Message = $"Your ID or password did not match. Please try again.";
                                return View();
                            }

                        }
                        con.Close();
                        ViewBag.Message = successfulMessage;
                        return View("SuccessClockIn", currentUser);
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

        // Handles clock in or clock out of current user.
        private string ClockInUser(SqlConnection sqlConn, PeopleModel currentUser)
        {
            string message = string.Empty;
            string dbName = _configuration.GetValue<string>("DatabaseName");

            var clockedLogID = ClockIn(sqlConn, currentUser.ID);
            string sqlQuery = String.Empty;

            // If clockedLogID string is empty then the user has not clocked in and we can insert a new clock in entry. 
            // Otherwise the user has clocked in for the day and we can use the log id to clock them out. 
            if (String.IsNullOrEmpty(clockedLogID)) {
                sqlQuery = $"INSERT INTO {dbName}.[Timesheet] (ID, First_Name, Last_Name, START_TIMESTAMP) VALUES ({currentUser.ID}, '{currentUser.First_Name}', '{currentUser.Last_Name}', GetDate())";
                message = "clocked in";
            }
            else
            {
                sqlQuery = $"UPDATE {dbName}.[Timesheet] SET END_TIMESTAMP = GetDate() WHERE LOG_ID = {clockedLogID} and END_TIMESTAMP IS NULL";
                message = "clocked out";
            }

            SqlCommand updateCmd = new SqlCommand(sqlQuery, sqlConn);

            return message;
        }

        // Check if the user has clocked in for the day already.
        private string ClockIn(SqlConnection sqlConn, int userID)
        {
            var logidRow = String.Empty;
            string dbName = _configuration.GetValue<string>("DatabaseName");

            string sqlQuery = $"SELECT LOG_ID FROM {dbName}.[Timesheet] WHERE ID = '{userID}' AND START_TIMESTAMP >= CONVERT(date, GETDATE()) AND END_TIMESTAMP IS NULL";
            SqlCommand updateCmd = new SqlCommand(sqlQuery, sqlConn);
            var sqlReader = updateCmd.ExecuteReader();

            while (sqlReader.Read())
            {
                logidRow = sqlReader["LOG_ID"].ToString();
            }
            return logidRow;
        }
    }
}