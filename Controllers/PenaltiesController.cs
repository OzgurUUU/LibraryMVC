using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class PenaltiesController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public PenaltiesController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: Penalties
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<dynamic> penalties = new List<dynamic>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // Ceza listesini, kullanıcı bilgileri ve ödünç alınan kitabın adıyla birlikte getiriyoruz (daha iyi görünüm için)
                string sql = @"
                    SELECT p.penaltyId, p.penaltyAmount, 
                           u.userTC, u.userName, u.userSurname,
                           br.borrowId, bk.bookName
                    FROM [Penalty] p
                    INNER JOIN [User] u ON p.userTC = u.userTC
                    LEFT JOIN [Borrow] br ON p.borrowId = br.borrowId
                    LEFT JOIN [Book] bk ON br.bookISBN = bk.bookISBN";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        penalties.Add(new
                        {
                            PenaltyId = Convert.ToInt32(reader["penaltyId"]),
                            PenaltyAmount = Convert.ToDecimal(reader["penaltyAmount"]),
                            UserTC = reader["userTC"].ToString(),
                            UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                            BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                            BookName = reader["bookName"] != DBNull.Value ? reader["bookName"].ToString() : "Bilinmeyen Kitap"
                        });
                    }
                }
            }

            return View(penalties);
        }

        // GET: Penalties/Delete/5 (Cezayı Öde / Sil Görüntüleme)
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            dynamic penaltyDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT p.penaltyId, p.penaltyAmount, 
                           u.userTC, u.userName, u.userSurname,
                           br.borrowId, bk.bookName
                    FROM [Penalty] p
                    INNER JOIN [User] u ON p.userTC = u.userTC
                    LEFT JOIN [Borrow] br ON p.borrowId = br.borrowId
                    LEFT JOIN [Book] bk ON br.bookISBN = bk.bookISBN
                    WHERE p.penaltyId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            penaltyDetail = new
                            {
                                PenaltyId = Convert.ToInt32(reader["penaltyId"]),
                                PenaltyAmount = Convert.ToDecimal(reader["penaltyAmount"]),
                                UserTC = reader["userTC"].ToString(),
                                UserFullName = reader["userName"].ToString() + " " + reader["userSurname"].ToString(),
                                BorrowId = reader["borrowId"] != DBNull.Value ? Convert.ToInt32(reader["borrowId"]) : 0,
                                BookName = reader["bookName"] != DBNull.Value ? reader["bookName"].ToString() : "Bilinmiyor"
                            };
                        }
                    }
                }
            }

            if (penaltyDetail == null) return NotFound();
            return View(penaltyDetail);
        }

        // POST: Penalties/Delete/5 (Cezayı Sil ve Loga Aktar)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // İşlem Bütünlüğü (Transaction) - Taşıma ve Silme aynı anda yapılmalı
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Cezayı PenaltyLog tablosuna taşı (C# tarafına hiç veri çekmeden doğrudan SQL ile çok daha hızlı)
                        string logSql = @"
                            INSERT INTO [PenaltyLog] (penaltyId, borrowId, userTC, penaltyAmount)
                            SELECT penaltyId, borrowId, userTC, penaltyAmount 
                            FROM [Penalty] 
                            WHERE penaltyId = @id";

                        using (SqlCommand cmd = new SqlCommand(logSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            // Eğer aktarılacak kayıt bulunamazsa işlemi iptal et
                            if (rowsAffected == 0)
                            {
                                transaction.Rollback();
                                return NotFound();
                            }
                        }

                        // 2. Asıl tablodan cezayı sil
                        string deleteSql = "DELETE FROM [Penalty] WHERE penaltyId = @id";
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
                        TempData["ErrorMessage"] = "Ceza ödeme/silme işlemi sırasında bir hata oluştu.";
                        return RedirectToAction("Delete", new { id });
                    }
                }
            }

            return RedirectToAction("Index");
        }
    }
}