using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class BookAuthorsController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public BookAuthorsController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: BookAuthors
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<BookAuthor> authors = new List<BookAuthor>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT authorId, authorName, authorSurname FROM [BookAuthor]";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        authors.Add(new BookAuthor
                        {
                            AuthorId = Convert.ToInt32(reader["authorId"]),
                            AuthorName = reader["authorName"].ToString(),
                            AuthorSurname = reader["authorSurname"].ToString()
                        });
                    }
                }
            }

            return View(authors);
        }

        // GET: BookAuthors/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        // POST: BookAuthors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BookAuthor bookAuthor)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "INSERT INTO [BookAuthor] (authorName, authorSurname) VALUES (@name, @surname)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", bookAuthor.AuthorName);
                        cmd.Parameters.AddWithValue("@surname", bookAuthor.AuthorSurname);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            return View(bookAuthor);
        }

        // GET: BookAuthors/Edit/5
        public IActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            BookAuthor bookAuthor = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT authorId, authorName, authorSurname FROM [BookAuthor] WHERE authorId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bookAuthor = new BookAuthor
                            {
                                AuthorId = Convert.ToInt32(reader["authorId"]),
                                AuthorName = reader["authorName"].ToString(),
                                AuthorSurname = reader["authorSurname"].ToString()
                            };
                        }
                    }
                }
            }

            if (bookAuthor == null) return NotFound();
            return View(bookAuthor);
        }

        // POST: BookAuthors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BookAuthor bookAuthor)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE [BookAuthor] SET authorName = @name, authorSurname = @surname WHERE authorId = @id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", bookAuthor.AuthorName);
                        cmd.Parameters.AddWithValue("@surname", bookAuthor.AuthorSurname);
                        cmd.Parameters.AddWithValue("@id", bookAuthor.AuthorId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(bookAuthor);
        }

        // GET: BookAuthors/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            BookAuthor bookAuthor = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT authorId, authorName, authorSurname FROM [BookAuthor] WHERE authorId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bookAuthor = new BookAuthor
                            {
                                AuthorId = Convert.ToInt32(reader["authorId"]),
                                AuthorName = reader["authorName"].ToString(),
                                AuthorSurname = reader["authorSurname"].ToString()
                            };
                        }
                    }
                }
            }

            if (bookAuthor == null) return NotFound();
            return View(bookAuthor);
        }

        // POST: BookAuthors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // 1. Bu yazara ait kitaplardan herhangi biri şu anda ödünçte mi? (Alt sorgu ile tek seferde güvenli kontrol)
                string checkBorrowSql = @"
                    SELECT COUNT(*) 
                    FROM [Borrow] 
                    WHERE bookISBN IN (SELECT bookISBN FROM [BookJoinAuthor] WHERE authorId = @id)";

                using (SqlCommand cmd = new SqlCommand(checkBorrowSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int borrowedCount = (int)cmd.ExecuteScalar();

                    if (borrowedCount > 0)
                    {
                        ModelState.AddModelError("", "Silmek istediğiniz yazara ait bazı kitaplar şu anda ödünçte. Önce teslim alınmaları gerekmektedir.");
                        return View(new BookAuthor { AuthorId = id }); // View'ı geri döndür
                    }
                }

                // 2. İşlem Bütünlüğü (Transaction) Başlat
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 3. Yazara ait kitapların ISBN'lerini listeye çekiyoruz
                        List<string> booksToDelete = new List<string>();
                        string getBooksSql = "SELECT bookISBN FROM [BookJoinAuthor] WHERE authorId = @id";

                        using (SqlCommand cmd = new SqlCommand(getBooksSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    booksToDelete.Add(reader["bookISBN"].ToString());
                                }
                            }
                        }

                        // 4. Kitapları ve kitaplara ait tüm bağlantıları (ortak yazarlar, türler vb.) güvenli bir şekilde sil
                        foreach (var isbn in booksToDelete)
                        {
                            // Kitabın tür bağlantılarını sil
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM [BookJoinType] WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", isbn);
                                cmd.ExecuteNonQuery();
                            }

                            // Kitabın tüm yazar bağlantılarını sil (ortak yazarlar da dâhil olmak üzere kitap tamamen kalktığı için)
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM [BookJoinAuthor] WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", isbn);
                                cmd.ExecuteNonQuery();
                            }

                            // Kitabın kendisini sil
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM [Book] WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", isbn);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 5. Son olarak Yazar kaydının kendisini sil (Eğer hiçbir kitabı yoksa doğrudan burası çalışır)
                        string deleteAuthorSql = "DELETE FROM [BookAuthor] WHERE authorId = @id";
                        using (SqlCommand cmd = new SqlCommand(deleteAuthorSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit(); // Tüm silmeler başarılıysa onayla
                    }
                    catch (Exception)
                    {
                        transaction.Rollback(); // Hata olursa hiçbirini silme (Veritabanı korunur)
                        ModelState.AddModelError("", "Silme işlemi sırasında veritabanında bir hata oluştu.");
                        return View(new BookAuthor { AuthorId = id });
                    }
                }
            }

            return RedirectToAction("Index");
        }
    }
}