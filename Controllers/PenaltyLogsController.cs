using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class PenaltyLogsController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public PenaltyLogsController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: PenaltyLogs
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<dynamic> penaltyLogs = new List<dynamic>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Sadece ID'leri değil, anlaşılır olması için Kullanıcı adlarını da çekiyoruz
                string sql = @"
                    SELECT pl.penaltyId, pl.borrowId, pl.userTC, pl.penaltyAmount, 
                           u.userName, u.userSurname
                    FROM [PenaltyLog] pl
                    LEFT JOIN [User] u ON pl.userTC = u.userTC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        penaltyLogs.Add(new
                        {
                            PenaltyId = Convert.ToInt32(reader["penaltyId"]),
                            BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                            UserTC = reader["userTC"].ToString(),
                            UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                            PenaltyAmount = Convert.ToDecimal(reader["penaltyAmount"])
                        });
                    }
                }
            }

            return View(penaltyLogs);
        }

        // GET: PenaltyLogs/Details/5
        public IActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic logDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT pl.*, u.userName, u.userSurname 
                    FROM [PenaltyLog] pl
                    LEFT JOIN [User] u ON pl.userTC = u.userTC
                    WHERE pl.penaltyId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            logDetail = new
                            {
                                PenaltyId = Convert.ToInt32(reader["penaltyId"]),
                                BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                                UserTC = reader["userTC"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                PenaltyAmount = Convert.ToDecimal(reader["penaltyAmount"])
                            };
                        }
                    }
                }
            }

            if (logDetail == null) return NotFound();
            return View(logDetail);
        }

        // GET: PenaltyLogs/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        // POST: PenaltyLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PenaltyLog penaltyLog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "INSERT INTO [PenaltyLog] (borrowId, userTC, penaltyAmount) VALUES (@borrowId, @userTC, @amount)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        // borrowId 0 ise veya boşsa NULL gönderilebilir, veritabanı tasarımına göre ayarlandı
                        if (penaltyLog.BorrowId > 0)
                            cmd.Parameters.AddWithValue("@borrowId", penaltyLog.BorrowId);
                        else
                            cmd.Parameters.AddWithValue("@borrowId", DBNull.Value);

                        cmd.Parameters.AddWithValue("@userTC", penaltyLog.UserTC);
                        cmd.Parameters.AddWithValue("@amount", penaltyLog.PenaltyAmount);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            return View(penaltyLog);
        }

        // GET: PenaltyLogs/Edit/5
        public IActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            PenaltyLog penaltyLog = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM [PenaltyLog] WHERE penaltyId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            penaltyLog = new PenaltyLog
                            {
                                PenaltyId = Convert.ToInt32(reader["penaltyId"]),
                                BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                                UserTC = reader["userTC"].ToString(),
                                PenaltyAmount = Convert.ToDecimal(reader["penaltyAmount"])
                            };
                        }
                    }
                }
            }

            if (penaltyLog == null) return NotFound();
            return View(penaltyLog);
        }

        // POST: PenaltyLogs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(PenaltyLog penaltyLog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE [PenaltyLog] SET borrowId = @borrowId, userTC = @userTC, penaltyAmount = @amount WHERE penaltyId = @id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        if (penaltyLog.BorrowId > 0)
                            cmd.Parameters.AddWithValue("@borrowId", penaltyLog.BorrowId);
                        else
                            cmd.Parameters.AddWithValue("@borrowId", DBNull.Value);

                        cmd.Parameters.AddWithValue("@userTC", penaltyLog.UserTC);
                        cmd.Parameters.AddWithValue("@amount", penaltyLog.PenaltyAmount);
                        cmd.Parameters.AddWithValue("@id", penaltyLog.PenaltyId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            return View(penaltyLog);
        }

        // GET: PenaltyLogs/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic logDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT pl.*, u.userName, u.userSurname 
                    FROM [PenaltyLog] pl
                    LEFT JOIN [User] u ON pl.userTC = u.userTC
                    WHERE pl.penaltyId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            logDetail = new
                            {
                                PenaltyId = Convert.ToInt32(reader["penaltyId"]),
                                BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                                UserTC = reader["userTC"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                PenaltyAmount = Convert.ToDecimal(reader["penaltyAmount"])
                            };
                        }
                    }
                }
            }

            if (logDetail == null) return NotFound();
            return View(logDetail);
        }

        // POST: PenaltyLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM [PenaltyLog] WHERE penaltyId = @id";

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