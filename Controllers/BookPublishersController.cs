using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class BookPublishersController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public BookPublishersController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: Publishers
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<BookPublisher> bookPublishers = new List<BookPublisher>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT publisherId, publisherName FROM [BookPublisher]";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bookPublishers.Add(new BookPublisher
                        {
                            PublisherId = Convert.ToInt32(reader["publisherId"]),
                            PublisherName = reader["publisherName"].ToString()
                        });
                    }
                }
            }

            return View(bookPublishers);
        }

        // GET: Publishers/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        // POST: Publishers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BookPublisher bookpublisher)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "INSERT INTO [BookPublisher] (publisherName) VALUES (@name)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", bookpublisher.PublisherName);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            return View(bookpublisher);
        }

        // GET: Publishers/Edit/5
        public IActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            BookPublisher bookpublisher = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT publisherId, publisherName FROM [BookPublisher] WHERE publisherId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bookpublisher = new BookPublisher
                            {
                                PublisherId = Convert.ToInt32(reader["publisherId"]),
                                PublisherName = reader["publisherName"].ToString()
                            };
                        }
                    }
                }
            }

            if (bookpublisher == null) return NotFound();
            return View(bookpublisher);
        }

        // POST: Publishers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BookPublisher bookpublisher)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE [BookPublisher] SET publisherName = @name WHERE publisherId = @id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", bookpublisher.PublisherName);
                        cmd.Parameters.AddWithValue("@id", bookpublisher.PublisherId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(bookpublisher);
        }

        // GET: Publishers/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            BookPublisher bookpublisher = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT publisherId, publisherName FROM [BookPublisher] WHERE publisherId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bookpublisher = new BookPublisher
                            {
                                PublisherId = Convert.ToInt32(reader["publisherId"]),
                                PublisherName = reader["publisherName"].ToString()
                            };
                        }
                    }
                }
            }

            if (bookpublisher == null) return NotFound();
            return View(bookpublisher);
        }

        // POST: Publishers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // 1. Bu yayınevine ait ödünçte olan herhangi bir kitap var mı? (Performanslı INNER JOIN)
                string checkBorrowSql = @"
                    SELECT COUNT(*) 
                    FROM [Borrow] br
                    INNER JOIN [Book] bk ON br.bookISBN = bk.bookISBN
                    WHERE bk.publisherId = @id";

                using (SqlCommand cmd = new SqlCommand(checkBorrowSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int borrowedCount = (int)cmd.ExecuteScalar();

                    if (borrowedCount > 0)
                    {
                        ModelState.AddModelError("", "Bu yayınevine ait bazı kitaplar şu anda ödünçte. Silmeden önce teslim alınmaları gerekmektedir.");
                        return View(new BookPublisher { PublisherId = id });
                    }
                }

                // 2. İşlem Bütünlüğü (Transaction) - C# Döngüsü yerine Veritabanı içi toplu (Bulk) silme
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Alt sorgu (Sub-Query) kullanarak yayınevinin tüm kitaplarının yazar bağlantılarını tek seferde sil
                        string delAuthorJoinSql = "DELETE FROM [BookJoinAuthor] WHERE bookISBN IN (SELECT bookISBN FROM [Book] WHERE publisherId = @id)";
                        using (SqlCommand cmd = new SqlCommand(delAuthorJoinSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Yayınevinin tüm kitaplarının tür bağlantılarını tek seferde sil
                        string delTypeJoinSql = "DELETE FROM [BookJoinType] WHERE bookISBN IN (SELECT bookISBN FROM [Book] WHERE publisherId = @id)";
                        using (SqlCommand cmd = new SqlCommand(delTypeJoinSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Yayınevine ait tüm kitapları sil
                        string delBookSql = "DELETE FROM [Book] WHERE publisherId = @id";
                        using (SqlCommand cmd = new SqlCommand(delBookSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Son olarak Yayınevinin kendisini sil
                        string delPublisherSql = "DELETE FROM [BookPublisher] WHERE publisherId = @id";
                        using (SqlCommand cmd = new SqlCommand(delPublisherSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Silme işlemi sırasında veritabanında bir hata oluştu.");
                        return View(new BookPublisher { PublisherId = id });
                    }
                }
            }

            return RedirectToAction("Index");
        }
    }
}