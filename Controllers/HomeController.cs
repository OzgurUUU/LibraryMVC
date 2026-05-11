using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
//Tamam

namespace LibraryMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        // Veritabanı bağlantı dizesini appsettings.json'dan çekmek için IConfiguration kullanıyoruz (Dependency Injection)
        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? "Server=localhost;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public IActionResult Index()
        {
            // Session kontrolü (ASP.NET Core yapısı)
            if (HttpContext.Session.GetString("admin") != "admin")
                return RedirectToAction("Login", "Home");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Tüm istatistikleri tek bir sorguda alarak veritabanı trafiğini ve performansı optimize ettik
                string sql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM [Book]) AS TotalBooks,
                        (SELECT COUNT(*) FROM [BookAuthor]) AS TotalAuthors,
                        (SELECT COUNT(*) FROM [User]) AS TotalUsers,
                        (SELECT COUNT(*) FROM [BookPublisher]) AS TotalPublishers,
                        (SELECT COUNT(*) FROM [Borrow]) AS TotalBorrows,
                        (SELECT COUNT(*) FROM [Reservation]) AS TotalReservations
                ";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ViewBag.TotalBooks = reader["TotalBooks"];
                            ViewBag.TotalAuthors = reader["TotalAuthors"];
                            ViewBag.TotalUsers = reader["TotalUsers"];
                            ViewBag.TotalPublishers = reader["TotalPublishers"];
                            ViewBag.TotalBorrows = reader["TotalBorrows"];
                            ViewBag.TotalReservations = reader["TotalReservations"];
                        }
                    }
                }
            }

            return View();
        }

        public IActionResult DailyDuty()
        {
            if (HttpContext.Session.GetString("admin") != "admin")
                return RedirectToAction("Login", "Home");

            DateTime today = DateTime.Today;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Teslim tarihi geçmiş olan ödünç almaları getir
                string selectBorrowsSql = "SELECT borrowId, userTC, borrowEndDate FROM [Borrow] WHERE borrowEndDate < @today";
                List<dynamic> overdueBorrows = new List<dynamic>();

                using (SqlCommand cmd = new SqlCommand(selectBorrowsSql, conn))
                {
                    cmd.Parameters.AddWithValue("@today", today);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            overdueBorrows.Add(new
                            {
                                BorrowId = Convert.ToInt32(reader["borrowId"]),
                                UserTC = reader["userTC"].ToString(),
                                BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"])
                            });
                        }
                    }
                }

                // 2. Gecikmiş olanlar için ceza işlemlerini uygula
                foreach (var borrow in overdueBorrows)
                {
                    int daysOverdue = (today - borrow.BorrowEndDate).Days;

                    // Bu ödünç alma için halihazırda bir ceza var mı kontrol et
                    string checkPenaltySql = "SELECT COUNT(*) FROM [Penalty] WHERE borrowId = @borrowId";
                    bool penaltyExists = false;

                    using (SqlCommand checkCmd = new SqlCommand(checkPenaltySql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@borrowId", borrow.BorrowId);
                        penaltyExists = (int)checkCmd.ExecuteScalar() > 0;
                    }

                    if (!penaltyExists)
                    {
                        // Ceza yoksa yeni ceza kaydı oluştur (ER diyagramındaki sütunlara göre)
                        string insertSql = "INSERT INTO [Penalty] (userTC, borrowId, penaltyAmount) VALUES (@userTC, @borrowId, @penaltyAmount)";
                        using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@userTC", borrow.UserTC);
                            insertCmd.Parameters.AddWithValue("@borrowId", borrow.BorrowId);
                            insertCmd.Parameters.AddWithValue("@penaltyAmount", (decimal)daysOverdue); // Gün başı ceza bedeli eklenecekse burası çarpılabilir
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Ceza zaten varsa miktarı güncelle
                        string updateSql = "UPDATE [Penalty] SET penaltyAmount = @penaltyAmount WHERE borrowId = @borrowId";
                        using (SqlCommand updateCmd = new SqlCommand(updateSql, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@penaltyAmount", (decimal)daysOverdue);
                            updateCmd.Parameters.AddWithValue("@borrowId", borrow.BorrowId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            TempData["DutyMessage"] = "Günlük kontroller tamamlandı. Cezalar güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password) // FormCollection yerine doğrudan parametre eşleme kullanıyoruz
        {
            if (username == "admin" && password == "admin")
            {
                HttpContext.Session.SetString("admin", "admin");
                return RedirectToAction("Index", "Home");
            }

            // Hatalı girişte Session'ı temizle
            HttpContext.Session.Remove("admin");
            ViewBag.LoginError = "Geçersiz kullanıcı adı veya şifre.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }
    }
}