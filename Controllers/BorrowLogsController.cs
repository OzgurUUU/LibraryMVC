using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class BorrowLogsController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public BorrowLogsController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // Güvenlik: Admin olmayanların loglara erişimi engelleniyor
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: BorrowLogs
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<dynamic> borrowLogs = new List<dynamic>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Sadece kodları değil, anlaşılırlık için Kullanıcı Adı ve Kitap Adını da LEFT JOIN ile çekiyoruz
                string sql = @"
                    SELECT bl.borrowId, bl.bookISBN, bl.userTC, bl.borrowStartDate, bl.borrowEndDate, bl.borrowExtension,
                           u.userName, u.userSurname,
                           bk.bookName
                    FROM [BorrowLog] bl
                    LEFT JOIN [User] u ON bl.userTC = u.userTC
                    LEFT JOIN [Book] bk ON bl.bookISBN = bk.bookISBN";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        borrowLogs.Add(new
                        {
                            BorrowId = Convert.ToInt32(reader["borrowId"]),
                            BookISBN = reader["bookISBN"] != DBNull.Value ? reader["bookISBN"].ToString() : "",
                            BookName = reader["bookName"] != DBNull.Value ? reader["bookName"].ToString() : "Bilinmeyen Kitap",
                            UserTC = reader["userTC"] != DBNull.Value ? reader["userTC"].ToString() : "",
                            UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                            BorrowStartDate = Convert.ToDateTime(reader["borrowStartDate"]),
                            BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"]),
                            BorrowExtension = Convert.ToInt32(reader["borrowExtension"])
                        });
                    }
                }
            }

            return View(borrowLogs);
        }

        // GET: BorrowLogs/Details/5
        public IActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic logDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT bl.*, u.userName, u.userSurname, bk.bookName 
                    FROM [BorrowLog] bl
                    LEFT JOIN [User] u ON bl.userTC = u.userTC
                    LEFT JOIN [Book] bk ON bl.bookISBN = bk.bookISBN
                    WHERE bl.borrowId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            logDetail = new
                            {
                                BorrowId = Convert.ToInt32(reader["borrowId"]),
                                BookISBN = reader["bookISBN"] != DBNull.Value ? reader["bookISBN"].ToString() : "",
                                BookName = reader["bookName"] != DBNull.Value ? reader["bookName"].ToString() : "Bilinmeyen Kitap",
                                UserTC = reader["userTC"] != DBNull.Value ? reader["userTC"].ToString() : "",
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                BorrowStartDate = Convert.ToDateTime(reader["borrowStartDate"]),
                                BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"]),
                                BorrowExtension = Convert.ToInt32(reader["borrowExtension"])
                            };
                        }
                    }
                }
            }

            if (logDetail == null) return NotFound();
            return View(logDetail);
        }

        // GET: BorrowLogs/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        // POST: BorrowLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BorrowLog borrowLog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    // Not: Identity Insert kapalıysa ve borrowId log tablosunda da PK ise doğrudan değer atanabilir. 
                    // Ancak ER diyagramına göre borrowId log tablosunda otomatik artan değil, asıl Borrow tablosundan gelen ID olmalı.
                    string sql = @"
                        INSERT INTO [BorrowLog] (borrowId, bookISBN, userTC, borrowStartDate, borrowEndDate, borrowExtension) 
                        VALUES (@id, @isbn, @tc, @start, @end, @ext)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", borrowLog.BorrowId);

                        if (!string.IsNullOrEmpty(borrowLog.BookISBN))
                            cmd.Parameters.AddWithValue("@isbn", borrowLog.BookISBN);
                        else
                            cmd.Parameters.AddWithValue("@isbn", DBNull.Value);

                        if (!string.IsNullOrEmpty(borrowLog.UserTC))
                            cmd.Parameters.AddWithValue("@tc", borrowLog.UserTC);
                        else
                            cmd.Parameters.AddWithValue("@tc", DBNull.Value);

                        cmd.Parameters.AddWithValue("@start", borrowLog.BorrowStartDate);
                        cmd.Parameters.AddWithValue("@end", borrowLog.BorrowEndDate);
                        cmd.Parameters.AddWithValue("@ext", borrowLog.BorrowExtension);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            return View(borrowLog);
        }

        // GET: BorrowLogs/Edit/5
        public IActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            BorrowLog borrowLog = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM [BorrowLog] WHERE borrowId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            borrowLog = new BorrowLog
                            {
                                BorrowId = Convert.ToInt32(reader["borrowId"]),
                                BookISBN = reader["bookISBN"].ToString(),
                                UserTC = reader["userTC"].ToString(),
                                BorrowStartDate = Convert.ToDateTime(reader["borrowStartDate"]),
                                BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"]),
                                BorrowExtension = Convert.ToInt32(reader["borrowExtension"])
                            };
                        }
                    }
                }
            }

            if (borrowLog == null) return NotFound();
            return View(borrowLog);
        }

        // POST: BorrowLogs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BorrowLog borrowLog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        UPDATE [BorrowLog] 
                        SET bookISBN = @isbn, userTC = @tc, borrowStartDate = @start, borrowEndDate = @end, borrowExtension = @ext 
                        WHERE borrowId = @id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@isbn", string.IsNullOrEmpty(borrowLog.BookISBN) ? DBNull.Value : borrowLog.BookISBN);
                        cmd.Parameters.AddWithValue("@tc", string.IsNullOrEmpty(borrowLog.UserTC) ? DBNull.Value : borrowLog.UserTC);
                        cmd.Parameters.AddWithValue("@start", borrowLog.BorrowStartDate);
                        cmd.Parameters.AddWithValue("@end", borrowLog.BorrowEndDate);
                        cmd.Parameters.AddWithValue("@ext", borrowLog.BorrowExtension);
                        cmd.Parameters.AddWithValue("@id", borrowLog.BorrowId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(borrowLog);
        }

        // GET: BorrowLogs/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic logDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT bl.*, u.userName, u.userSurname, bk.bookName 
                    FROM [BorrowLog] bl
                    LEFT JOIN [User] u ON bl.userTC = u.userTC
                    LEFT JOIN [Book] bk ON bl.bookISBN = bk.bookISBN
                    WHERE bl.borrowId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            logDetail = new
                            {
                                BorrowId = Convert.ToInt32(reader["borrowId"]),
                                BookISBN = reader["bookISBN"].ToString(),
                                BookName = reader["bookName"] != DBNull.Value ? reader["bookName"].ToString() : "Bilinmiyor",
                                UserTC = reader["userTC"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                BorrowStartDate = Convert.ToDateTime(reader["borrowStartDate"]),
                                BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"]),
                                BorrowExtension = Convert.ToInt32(reader["borrowExtension"])
                            };
                        }
                    }
                }
            }

            if (logDetail == null) return NotFound();
            return View(logDetail);
        }

        // POST: BorrowLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM [BorrowLog] WHERE borrowId = @id";

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