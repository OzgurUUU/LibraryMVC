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
    public class ReservationsController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public ReservationsController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // Dropdown verilerini yüklemek için yardımcı metot
        private void LoadDropdowns(SqlConnection conn, int? selectedBorrowId = null, string selectedTc = null)
        {
            // Ödünç listesini getir (Sadece aktif olanları ve kitap bilgisiyle beraber)
            var borrows = new List<dynamic>();
            string borrowSql = @"
                SELECT br.borrowId, bk.bookName, br.borrowEndDate 
                FROM [Borrow] br
                INNER JOIN [Book] bk ON br.bookISBN = bk.bookISBN";

            using (SqlCommand cmd = new SqlCommand(borrowSql, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    borrows.Add(new
                    {
                        Id = Convert.ToInt32(reader["borrowId"]),
                        Display = $"Ödünç ID: {reader["borrowId"]} - Kitap: {reader["bookName"]} (Bitiş: {Convert.ToDateTime(reader["borrowEndDate"]).ToShortDateString()})"
                    });
                }
            }
            ViewBag.borrowId = new SelectList(borrows, "Id", "Display", selectedBorrowId);

            // Kullanıcıları getir
            var users = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand("SELECT userTC, userName, userSurname FROM [User]", conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new
                    {
                        Id = reader["userTC"].ToString(),
                        Display = $"{reader["userName"]} {reader["userSurname"]} ({reader["userTC"]})"
                    });
                }
            }
            ViewBag.userTC = new SelectList(users, "Id", "Display", selectedTc);
        }

        // GET: Reservations
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<dynamic> reservations = new List<dynamic>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Rezervasyon, Ödünç alınan kitap bilgisi ve Kullanıcı bilgilerini birleştiriyoruz
                string sql = @"
                    SELECT rs.reservationId, rs.reservationDate, 
                           br.borrowId, bk.bookName, 
                           u.userTC, u.userName, u.userSurname
                    FROM [Reservation] rs
                    INNER JOIN [Borrow] br ON rs.borrowId = br.borrowId
                    INNER JOIN [Book] bk ON br.bookISBN = bk.bookISBN
                    INNER JOIN [User] u ON rs.userTC = u.userTC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reservations.Add(new
                        {
                            ReservationId = Convert.ToInt32(reader["reservationId"]),
                            ReservationDate = Convert.ToDateTime(reader["reservationDate"]),
                            BorrowId = Convert.ToInt32(reader["borrowId"]),
                            BookName = reader["bookName"].ToString(),
                            UserTC = reader["userTC"].ToString(),
                            UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString()
                        });
                    }
                }
            }

            return View(reservations);
        }

        // GET: Reservations/Create
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

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Reservation reservation)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // 1. Kullanıcının limitini çek
                int bookLimit = 0;
                string limitSql = @"
                    SELECT t.bookLimit 
                    FROM [User] u 
                    JOIN [UserType] t ON u.typeId = t.typeId 
                    WHERE u.userTC = @tc";
                using (SqlCommand cmd = new SqlCommand(limitSql, conn))
                {
                    cmd.Parameters.AddWithValue("@tc", reservation.UserTC);
                    object result = cmd.ExecuteScalar();
                    if (result != null) bookLimit = Convert.ToInt32(result);
                    else
                    {
                        ModelState.AddModelError("UserTC", "Kullanıcı bulunamadı.");
                        LoadDropdowns(conn, reservation.BorrowId, reservation.UserTC);
                        return View(reservation);
                    }
                }

                // 2. Kullanıcının aktif ödünçlerini say
                int currentBorrows = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [Borrow] WHERE userTC = @tc", conn))
                {
                    cmd.Parameters.AddWithValue("@tc", reservation.UserTC);
                    currentBorrows = (int)cmd.ExecuteScalar();
                }

                // 3. Kullanıcının aktif rezervasyonlarını say
                int currentReservations = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [Reservation] WHERE userTC = @tc", conn))
                {
                    cmd.Parameters.AddWithValue("@tc", reservation.UserTC);
                    currentReservations = (int)cmd.ExecuteScalar();
                }

                if (bookLimit <= (currentBorrows + currentReservations))
                {
                    TempData["ExtensionError"] = $"Kullanıcı maksimum kitap/rezervasyon limitine ({bookLimit}) ulaştı.";
                    return RedirectToAction("Index");
                }

                // 4. Bu ödünç işlemi için zaten bir rezervasyon var mı kontrol et
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [Reservation] WHERE borrowId = @borrowId", conn))
                {
                    cmd.Parameters.AddWithValue("@borrowId", reservation.BorrowId);
                    if ((int)cmd.ExecuteScalar() > 0)
                    {
                        TempData["ExtensionError"] = "Bu kitap için halihazırda aktif bir rezervasyon bulunuyor.";
                        return RedirectToAction("Index");
                    }
                }

                // 5. Kaydet
                if (ModelState.IsValid)
                {
                    string insertSql = "INSERT INTO [Reservation] (borrowId, userTC, reservationDate) VALUES (@borrowId, @tc, @date)";
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@borrowId", reservation.BorrowId);
                        cmd.Parameters.AddWithValue("@tc", reservation.UserTC);
                        cmd.Parameters.AddWithValue("@date", DateTime.Today); // Eski kodda bugündü, uyduk.
                        cmd.ExecuteNonQuery();
                    }
                    return RedirectToAction("Index");
                }

                LoadDropdowns(conn, reservation.BorrowId, reservation.UserTC);
            }

            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic resDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT rs.reservationId, rs.reservationDate, 
                           br.borrowId, bk.bookName, 
                           u.userTC, u.userName, u.userSurname
                    FROM [Reservation] rs
                    INNER JOIN [Borrow] br ON rs.borrowId = br.borrowId
                    INNER JOIN [Book] bk ON br.bookISBN = bk.bookISBN
                    INNER JOIN [User] u ON rs.userTC = u.userTC
                    WHERE rs.reservationId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resDetail = new
                            {
                                ReservationId = Convert.ToInt32(reader["reservationId"]),
                                ReservationDate = Convert.ToDateTime(reader["reservationDate"]),
                                BorrowId = Convert.ToInt32(reader["borrowId"]),
                                BookName = reader["bookName"].ToString(),
                                UserTC = reader["userTC"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString()
                            };
                        }
                    }
                }
            }

            if (resDetail == null) return NotFound();
            return View(resDetail);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // İşlem Bütünlüğü (Loglama ve Silme)
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Log tablosuna kopyala
                        string moveSql = @"
                            INSERT INTO [ReservationLog] (reservationId, borrowId, userTC, reservationDate)
                            SELECT reservationId, borrowId, userTC, reservationDate 
                            FROM [Reservation] 
                            WHERE reservationId = @id";

                        using (SqlCommand cmd = new SqlCommand(moveSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Asıl tablodan sil
                        string deleteSql = "DELETE FROM [Reservation] WHERE reservationId = @id";
                        using (SqlCommand cmd = new SqlCommand(deleteSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "Rezervasyon silinirken bir hata oluştu.";
                        return RedirectToAction("Delete", new { id });
                    }
                }
            }

            return RedirectToAction("Index");
        }
    }
}