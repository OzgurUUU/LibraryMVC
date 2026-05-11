using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
//Tamam

namespace LibraryMVC.Controllers
{
    public class BorrowsController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public BorrowsController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // Dropdown'ları yüklemek için yardımcı metot
        private void LoadDropdowns(SqlConnection conn, string selectedIsbn = null, string selectedTc = null)
        {
            var books = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand("SELECT bookISBN, bookName FROM [Book]", conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    books.Add(new
                    {
                        Id = reader["bookISBN"].ToString(),
                        Display = reader["bookName"].ToString() + " (" + reader["bookISBN"].ToString() + ")"
                    });
                }
            }
            ViewBag.bookISBN = new SelectList(books, "Id", "Display", selectedIsbn);

            var users = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand("SELECT userTC, userName, userSurname FROM [User]", conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new
                    {
                        Id = reader["userTC"].ToString(),
                        Display = reader["userName"].ToString() + " " + reader["userSurname"].ToString() + " (" + reader["userTC"].ToString() + ")"
                    });
                }
            }
            ViewBag.userTC = new SelectList(users, "Id", "Display", selectedTc);
        }

        // GET: Borrows
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<dynamic> borrows = new List<dynamic>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT br.borrowId, br.borrowStartDate, br.borrowEndDate, br.borrowExtensions, 
                           bk.bookISBN, bk.bookName, 
                           u.userTC, u.userName, u.userSurname
                    FROM [Borrow] br
                    INNER JOIN [Book] bk ON br.bookISBN = bk.bookISBN
                    INNER JOIN [User] u ON br.userTC = u.userTC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        borrows.Add(new
                        {
                            BorrowId = Convert.ToInt32(reader["borrowId"]),
                            BookISBN = reader["bookISBN"].ToString(),
                            BookName = reader["bookName"].ToString(),
                            UserTC = reader["userTC"].ToString(),
                            UserName = reader["userName"].ToString(),
                            UserSurname = reader["userSurname"].ToString(),
                            BorrowStartDate = Convert.ToDateTime(reader["borrowStartDate"]),
                            BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"]),
                            BorrowExtensions = Convert.ToInt32(reader["borrowExtensions"])
                        });
                    }
                }
            }

            return View(borrows);
        }

        // GET: Borrows/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                LoadDropdowns(conn);
            }
            return View();
        }

        // POST: Borrows/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Borrow borrow)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // 1. Kullanıcı limitlerini ve tipini getir
                int bookLimit = 0;
                int borrowPeriod = 0;
                string userSql = @"
                    SELECT t.bookLimit, t.borrowPeriod 
                    FROM [User] u 
                    JOIN [UserType] t ON u.typeId = t.typeId 
                    WHERE u.userTC = @tc";

                using (SqlCommand cmd = new SqlCommand(userSql, conn))
                {
                    cmd.Parameters.AddWithValue("@tc", borrow.UserTC);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bookLimit = Convert.ToInt32(reader["bookLimit"]);
                            borrowPeriod = Convert.ToInt32(reader["borrowPeriod"]);
                        }
                        else
                        {
                            ModelState.AddModelError("UserTC", "Kullanıcı bulunamadı.");
                        }
                    }
                }

                // 2. Kullanıcının mevcut ödünç ve rezervasyon sayısını hesapla
                int currentBorrows = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Borrow WHERE userTC = @tc", conn))
                {
                    cmd.Parameters.AddWithValue("@tc", borrow.UserTC);
                    currentBorrows = (int)cmd.ExecuteScalar();
                }

                int currentReservations = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Reservation WHERE userTC = @tc", conn))
                {
                    cmd.Parameters.AddWithValue("@tc", borrow.UserTC);
                    currentReservations = (int)cmd.ExecuteScalar();
                }

                if (bookLimit <= (currentBorrows + currentReservations))
                {
                    TempData["ExtensionError"] = $"Kullanıcı maksimum ödünç alma limitine ({bookLimit}) ulaştı.";
                    return RedirectToAction("Index");
                }

                // 3. Kitap stok ve uygunluk kontrolü
                int bookTotalCount = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT bookCount FROM [Book] WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", borrow.BookISBN);
                    object result = cmd.ExecuteScalar();
                    if (result != null) bookTotalCount = Convert.ToInt32(result);
                }

                int activeBookBorrows = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Borrow WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", borrow.BookISBN);
                    activeBookBorrows = (int)cmd.ExecuteScalar();
                }

                if (activeBookBorrows >= bookTotalCount)
                {
                    TempData["ExtensionError"] = "Bu kitap şu anda stokta yok (tamamı ödünç verilmiş).";
                    return RedirectToAction("Index");
                }

                // 4. Kaydet
                if (ModelState.IsValid)
                {
                    string insertSql = @"
                        INSERT INTO [Borrow] (bookISBN, userTC, borrowStartDate, borrowEndDate, borrowExtensions) 
                        VALUES (@isbn, @tc, @start, @end, @ext)";

                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@isbn", borrow.BookISBN);
                        cmd.Parameters.AddWithValue("@tc", borrow.UserTC);
                        cmd.Parameters.AddWithValue("@start", DateTime.Today);
                        cmd.Parameters.AddWithValue("@end", DateTime.Today.AddDays(borrowPeriod));
                        cmd.Parameters.AddWithValue("@ext", 0);

                        cmd.ExecuteNonQuery();
                    }
                    return RedirectToAction("Index");
                }

                LoadDropdowns(conn, borrow.BookISBN, borrow.UserTC);
            }
            return View(borrow);
        }

        // GET: Borrows/Delete/5 (Kitap İade Ekranı)
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic borrowDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT br.*, bk.bookName, u.userName, u.userSurname
                    FROM [Borrow] br
                    INNER JOIN [Book] bk ON br.bookISBN = bk.bookISBN
                    INNER JOIN [User] u ON br.userTC = u.userTC
                    WHERE br.borrowId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            borrowDetail = new
                            {
                                BorrowId = Convert.ToInt32(reader["borrowId"]),
                                BookISBN = reader["bookISBN"].ToString(),
                                BookName = reader["bookName"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                BorrowStartDate = Convert.ToDateTime(reader["borrowStartDate"]),
                                BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"]),
                                BorrowExtensions = Convert.ToInt32(reader["borrowExtensions"])
                            };
                        }
                    }
                }
            }

            if (borrowDetail == null) return NotFound();
            return View(borrowDetail);
        }

        // POST: Borrows/Delete/5 (İade İşlemini Onayla)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // 1. İlgili ödünç kaydını çek
                Borrow borrow = null;
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM [Borrow] WHERE borrowId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            borrow = new Borrow
                            {
                                BorrowId = Convert.ToInt32(reader["borrowId"]),
                                BookISBN = reader["bookISBN"].ToString(),
                                UserTC = reader["userTC"].ToString(),
                                BorrowStartDate = Convert.ToDateTime(reader["borrowStartDate"]),
                                BorrowEndDate = Convert.ToDateTime(reader["borrowEndDate"]),
                                BorrowExtensions = Convert.ToInt32(reader["borrowExtensions"])
                            };
                        }
                    }
                }

                if (borrow == null) return NotFound();

                // İşlem Bütünlüğü Başlangıcı (Loglama ve Silme işlemleri bir arada yapılmalı)
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 2. ER Diyagramına göre BorrowLog tablosuna taşı (Arşivleme)
                        // Not: Gönderdiğin ER diyagramında kolon adı 'bokkISBN' olarak yazılmıştı, veritabanında öyleyse buradaki SQL cümlesini 'bokkISBN' yapmalısın.
                        string logSql = @"
                            INSERT INTO [BorrowLog] (borrowId, bookISBN, userTC, borrowStartDate, borrowEndDate, borrowExtension) 
                            VALUES (@id, @isbn, @tc, @start, @end, @ext)";

                        using (SqlCommand cmd = new SqlCommand(logSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", borrow.BorrowId); // Log tablosunda da anahtar
                            cmd.Parameters.AddWithValue("@isbn", borrow.BookISBN);
                            cmd.Parameters.AddWithValue("@tc", borrow.UserTC);
                            cmd.Parameters.AddWithValue("@start", borrow.BorrowStartDate);
                            cmd.Parameters.AddWithValue("@end", borrow.BorrowEndDate);
                            cmd.Parameters.AddWithValue("@ext", borrow.BorrowExtensions);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. (Gerekirse) ER diyagramındaki PenaltyLog tablosuna cezayı aktarma
                        // Eğer FK hatası alırsan diye: Ceza tablosundaki kaydı PenaltyLog'a aktarıp Penalty'den siliyoruz.
                        string movePenaltySql = @"
                            INSERT INTO PenaltyLog (penaltyId, borrowId, userTC, penaltyAmount)
                            SELECT penaltyId, borrowId, userTC, penaltyAmount FROM Penalty WHERE borrowId = @id;
                            DELETE FROM Penalty WHERE borrowId = @id;";
                        using (SqlCommand cmd = new SqlCommand(movePenaltySql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", borrow.BorrowId);
                            cmd.ExecuteNonQuery(); // Ceza yoksa bile çalışır hata vermez
                        }

                        // 4. Aktif ödünç kaydını Borrow tablosundan sil
                        string deleteSql = "DELETE FROM [Borrow] WHERE borrowId = @id";
                        using (SqlCommand cmd = new SqlCommand(deleteSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", borrow.BorrowId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "İade işlemi sırasında bir hata oluştu.";
                        return RedirectToAction("Delete", new { id });
                    }
                }
            }

            return RedirectToAction("Index");
        }

        // GET: Borrows/Extend/5 (Süre Uzatma)
        public IActionResult Extend(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // 1. Ödünç kaydını ve kullanıcının uzatma limitini tek sorguda getir
                int currentExtensions = 0;
                int extensionLimit = 0;
                int borrowPeriod = 0;
                DateTime currentEndDate = DateTime.MinValue;

                string sql = @"
                    SELECT br.borrowExtensions, br.borrowEndDate, t.extensionLimit, t.borrowPeriod 
                    FROM [Borrow] br
                    JOIN [User] u ON br.userTC = u.userTC
                    JOIN [UserType] t ON u.typeId = t.typeId
                    WHERE br.borrowId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            currentExtensions = Convert.ToInt32(reader["borrowExtensions"]);
                            currentEndDate = Convert.ToDateTime(reader["borrowEndDate"]);
                            extensionLimit = Convert.ToInt32(reader["extensionLimit"]);
                            borrowPeriod = Convert.ToInt32(reader["borrowPeriod"]);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }

                // 2. Limit kontrolü
                if (currentExtensions >= extensionLimit)
                {
                    TempData["ExtensionError"] = "Bu kullanıcı için süre uzatma limitine ulaşıldı.";
                    return RedirectToAction("Index");
                }

                // 3. Başka biri bu kitap için (veya bu işlem için) rezervasyon yapmış mı kontrolü
                bool isReserved = false;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Reservation WHERE borrowId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    isReserved = (int)cmd.ExecuteScalar() > 0;
                }

                if (isReserved)
                {
                    TempData["ExtensionError"] = "Bu kitap başka bir kullanıcı tarafından rezerve edilmiştir. Süre uzatılamaz.";
                    return RedirectToAction("Index");
                }

                // 4. Süreyi uzat ve kaydet
                string updateSql = "UPDATE [Borrow] SET borrowEndDate = @newEnd, borrowExtensions = @newExt WHERE borrowId = @id";
                using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@newEnd", currentEndDate.AddDays(borrowPeriod));
                    cmd.Parameters.AddWithValue("@newExt", currentExtensions + 1);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}