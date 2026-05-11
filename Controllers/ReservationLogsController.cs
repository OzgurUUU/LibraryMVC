using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class ReservationLogsController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public ReservationLogsController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // Güvenlik: Önceki kodda unutulan yetki kontrolü eklendi
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: ReservationLogs
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<dynamic> reservationLogs = new List<dynamic>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Sadece TC Kimlik no yerine kullanıcının Ad-Soyad bilgisini de getirmek için LEFT JOIN kullanıyoruz
                string sql = @"
                    SELECT rl.reservationId, rl.borrowId, rl.userTC, rl.reservationDate, 
                           u.userName, u.userSurname
                    FROM [ReservationLog] rl
                    LEFT JOIN [User] u ON rl.userTC = u.userTC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reservationLogs.Add(new
                        {
                            ReservationId = Convert.ToInt32(reader["reservationId"]),
                            BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                            UserTC = reader["userTC"].ToString(),
                            UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                            ReservationDate = Convert.ToDateTime(reader["reservationDate"])
                        });
                    }
                }
            }

            return View(reservationLogs);
        }

        // GET: ReservationLogs/Details/5
        public IActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic logDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT rl.*, u.userName, u.userSurname 
                    FROM [ReservationLog] rl
                    LEFT JOIN [User] u ON rl.userTC = u.userTC
                    WHERE rl.reservationId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            logDetail = new
                            {
                                ReservationId = Convert.ToInt32(reader["reservationId"]),
                                BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                                UserTC = reader["userTC"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                ReservationDate = Convert.ToDateTime(reader["reservationDate"])
                            };
                        }
                    }
                }
            }

            if (logDetail == null) return NotFound();
            return View(logDetail);
        }

        // GET: ReservationLogs/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        // POST: ReservationLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ReservationLog reservationLog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "INSERT INTO [ReservationLog] (borrowId, userTC, reservationDate) VALUES (@borrowId, @userTC, @date)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        if (reservationLog.BorrowId > 0)
                            cmd.Parameters.AddWithValue("@borrowId", reservationLog.BorrowId);
                        else
                            cmd.Parameters.AddWithValue("@borrowId", DBNull.Value); // Güvenlik için NULL kontrolü

                        cmd.Parameters.AddWithValue("@userTC", reservationLog.UserTC);
                        cmd.Parameters.AddWithValue("@date", reservationLog.ReservationDate);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            return View(reservationLog);
        }

        // GET: ReservationLogs/Edit/5
        public IActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            ReservationLog reservationLog = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM [ReservationLog] WHERE reservationId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reservationLog = new ReservationLog
                            {
                                ReservationId = Convert.ToInt32(reader["reservationId"]),
                                BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                                UserTC = reader["userTC"].ToString(),
                                ReservationDate = Convert.ToDateTime(reader["reservationDate"])
                            };
                        }
                    }
                }
            }

            if (reservationLog == null) return NotFound();
            return View(reservationLog);
        }

        // POST: ReservationLogs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ReservationLog reservationLog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE [ReservationLog] SET borrowId = @borrowId, userTC = @userTC, reservationDate = @date WHERE reservationId = @id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        if (reservationLog.BorrowId > 0)
                            cmd.Parameters.AddWithValue("@borrowId", reservationLog.BorrowId);
                        else
                            cmd.Parameters.AddWithValue("@borrowId", DBNull.Value);

                        cmd.Parameters.AddWithValue("@userTC", reservationLog.UserTC);
                        cmd.Parameters.AddWithValue("@date", reservationLog.ReservationDate);
                        cmd.Parameters.AddWithValue("@id", reservationLog.ReservationId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(reservationLog);
        }

        // GET: ReservationLogs/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic logDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT rl.*, u.userName, u.userSurname 
                    FROM [ReservationLog] rl
                    LEFT JOIN [User] u ON rl.userTC = u.userTC
                    WHERE rl.reservationId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            logDetail = new
                            {
                                ReservationId = Convert.ToInt32(reader["reservationId"]),
                                BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                                UserTC = reader["userTC"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                ReservationDate = Convert.ToDateTime(reader["reservationDate"])
                            };
                        }
                    }
                }
            }

            if (logDetail == null) return NotFound();
            return View(logDetail);
        }

        // POST: ReservationLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM [ReservationLog] WHERE reservationId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}