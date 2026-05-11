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
    public class BooksController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public BooksController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: Books
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home", new { area = "" });

            List<Book> books = new List<Book>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                // SQL sorgusundan raf (shelf) ile ilgili her şey çıkarıldı.
                string sql = "SELECT * FROM Book";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                BookISBN = reader["bookISBN"].ToString(),
                                BookName = reader["bookName"].ToString(),
                                BookNumOfPage = Convert.ToInt32(reader["bookNumOfPage"]),
                                BookCount = Convert.ToInt32(reader["bookCount"]),
                                BookPublicationYear = Convert.ToInt32(reader["bookPublicationYear"]),
                                PublisherId = Convert.ToInt32(reader["publisherId"])
                            });
                        }
                    }
                }
            }

            return View(books);
        }

        // Yardımcı Metot: Dropdown verilerini yükler (Raf kısmı tamamen temizlendi)
        private void LoadDropdownData(SqlConnection conn, string selectedPublisher = null, int[] selectedAuthors = null, int[] selectedTypes = null)
        {
            // Yazarlar
            var authors = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand("SELECT authorId, authorName, authorSurname FROM BookAuthor ORDER BY authorName", conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read()) authors.Add(new { Id = reader["authorId"], Display = reader["authorName"] + " " + reader["authorSurname"] });
            }
            ViewBag.Authors = new MultiSelectList(authors, "Id", "Display", selectedAuthors);

            // Yayınevleri
            var publishers = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand("SELECT publisherId, publisherName FROM BookPublisher ORDER BY publisherName", conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read()) publishers.Add(new { Id = reader["publisherId"], Display = reader["publisherName"] });
            }
            ViewBag.Publishers = new SelectList(publishers, "Id", "Display", selectedPublisher);

            // Türler
            var types = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand("SELECT typeId, typeName FROM BookType ORDER BY typeName", conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read()) types.Add(new { Id = reader["typeId"], Display = reader["typeName"] });
            }
            ViewBag.Types = new MultiSelectList(types, "Id", "Display", selectedTypes);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                LoadDropdownData(conn);
            }
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Book book, int[] SelectedAuthorIds, int[] SelectedTypeIds)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // ISBN Kontrolü
                using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Book WHERE bookISBN = @isbn", conn))
                {
                    checkCmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                    if ((int)checkCmd.ExecuteScalar() > 0)
                    {
                        LoadDropdownData(conn);
                        ModelState.AddModelError("BookISBN", "Bu ISBN numarasına sahip bir kitap zaten mevcut.");
                        return View(book);
                    }
                }

                if (ModelState.IsValid)
                {
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Kitabı Ekle (shelfId parametresi tamamen kaldırıldı)
                            string insertBookSql = @"INSERT INTO Book (bookISBN, publisherId, bookName, bookNumOfPage, bookCount, bookPublicationYear)
                                                     VALUES (@isbn, @pubId, @name, @pages, @count, @year)";
                            using (SqlCommand cmd = new SqlCommand(insertBookSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                cmd.Parameters.AddWithValue("@pubId", book.PublisherId);
                                cmd.Parameters.AddWithValue("@name", book.BookName);
                                cmd.Parameters.AddWithValue("@pages", book.BookNumOfPage);
                                cmd.Parameters.AddWithValue("@count", book.BookCount);
                                cmd.Parameters.AddWithValue("@year", book.BookPublicationYear);
                                cmd.ExecuteNonQuery();
                            }

                            // 2. Yazar Bağlantıları
                            if (SelectedAuthorIds != null)
                            {
                                foreach (var authorId in SelectedAuthorIds)
                                {
                                    using (SqlCommand cmd = new SqlCommand("INSERT INTO BookJoinAuthor (bookISBN, authorId) VALUES (@isbn, @aid)", conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                        cmd.Parameters.AddWithValue("@aid", authorId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            // 3. Tür Bağlantıları
                            if (SelectedTypeIds != null)
                            {
                                foreach (var typeId in SelectedTypeIds)
                                {
                                    using (SqlCommand cmd = new SqlCommand("INSERT INTO BookJoinType (bookISBN, typeId) VALUES (@isbn, @tid)", conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                        cmd.Parameters.AddWithValue("@tid", typeId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            return RedirectToAction("Index");
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", "Kayıt sırasında teknik bir hata oluştu.");
                        }
                    }
                }
                LoadDropdownData(conn, book.PublisherId.ToString(), SelectedAuthorIds, SelectedTypeIds);
            }
            return View(book);
        }

        // GET: Books/Edit/ISBN
        public IActionResult Edit(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (string.IsNullOrEmpty(id)) return BadRequest();

            Book book = null;
            List<int> selectedAuthors = new List<int>();
            List<int> selectedTypes = new List<int>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Book WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            book = new Book
                            {
                                BookISBN = reader["bookISBN"].ToString(),
                                BookName = reader["bookName"].ToString(),
                                BookNumOfPage = Convert.ToInt32(reader["bookNumOfPage"]),
                                BookCount = Convert.ToInt32(reader["bookCount"]),
                                BookPublicationYear = Convert.ToInt32(reader["bookPublicationYear"]),
                                PublisherId = Convert.ToInt32(reader["publisherId"])
                            };
                        }
                    }
                }

                if (book == null) return NotFound();

                using (SqlCommand cmd = new SqlCommand("SELECT authorId FROM BookJoinAuthor WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) selectedAuthors.Add(Convert.ToInt32(reader["authorId"]));
                    }
                }

                using (SqlCommand cmd = new SqlCommand("SELECT typeId FROM BookJoinType WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) selectedTypes.Add(Convert.ToInt32(reader["typeId"]));
                    }
                }

                LoadDropdownData(conn, book.PublisherId.ToString(), selectedAuthors.ToArray(), selectedTypes.ToArray());
            }

            return View(book);
        }

        // POST: Books/Edit/ISBN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Book book, int[] SelectedAuthorIds, int[] SelectedTypeIds)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            
                            string updateSql = @"UPDATE Book 
                                                 SET publisherId = @pubId, bookName = @name, bookNumOfPage = @pages, 
                                                     bookCount = @count, bookPublicationYear = @year
                                                 WHERE bookISBN = @isbn";
                            using (SqlCommand cmd = new SqlCommand(updateSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@pubId", book.PublisherId);
                                cmd.Parameters.AddWithValue("@name", book.BookName);
                                cmd.Parameters.AddWithValue("@pages", book.BookNumOfPage);
                                cmd.Parameters.AddWithValue("@count", book.BookCount);
                                cmd.Parameters.AddWithValue("@year", book.BookPublicationYear);
                                cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                cmd.ExecuteNonQuery();
                            }

                            // 2. Bağlantıları Yenile
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM BookJoinAuthor WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                cmd.ExecuteNonQuery();
                            }
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM BookJoinType WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                cmd.ExecuteNonQuery();
                            }

                            if (SelectedAuthorIds != null)
                            {
                                foreach (var authorId in SelectedAuthorIds)
                                {
                                    using (SqlCommand cmd = new SqlCommand("INSERT INTO BookJoinAuthor (bookISBN, authorId) VALUES (@isbn, @aid)", conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                        cmd.Parameters.AddWithValue("@aid", authorId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            if (SelectedTypeIds != null)
                            {
                                foreach (var typeId in SelectedTypeIds)
                                {
                                    using (SqlCommand cmd = new SqlCommand("INSERT INTO BookJoinType (bookISBN, typeId) VALUES (@isbn, @tid)", conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@isbn", book.BookISBN);
                                        cmd.Parameters.AddWithValue("@tid", typeId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            return RedirectToAction("Index");
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu.");
                        }
                    }
                }
            }

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                LoadDropdownData(conn, book.PublisherId.ToString(), SelectedAuthorIds, SelectedTypeIds);
            }
            return View(book);
        }

        // GET: Books/Delete/ISBN
        public IActionResult Delete(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (string.IsNullOrEmpty(id)) return BadRequest();

            Book book = null;
            List<string> authors = new List<string>();
            List<string> types = new List<string>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Book WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            book = new Book
                            {
                                BookISBN = reader["bookISBN"].ToString(),
                                BookName = reader["bookName"].ToString(),
                                BookCount = Convert.ToInt32(reader["bookCount"])
                            };
                        }
                    }
                }

                if (book == null) return NotFound();

                using (SqlCommand cmd = new SqlCommand(@"SELECT a.authorName, a.authorSurname FROM BookJoinAuthor bja 
                                                         JOIN BookAuthor a ON bja.authorId = a.authorId WHERE bja.bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) authors.Add(reader["authorName"].ToString() + " " + reader["authorSurname"].ToString());
                    }
                }

                using (SqlCommand cmd = new SqlCommand(@"SELECT t.typeName FROM BookJoinType bjt 
                                                         JOIN BookType t ON bjt.typeId = t.typeId WHERE bjt.bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) types.Add(reader["typeName"].ToString());
                    }
                }
            }

            ViewBag.Authors = authors;
            ViewBag.Types = types;
            return View(book);
        }

        // POST: Books/Delete/ISBN
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id, bool deleteAll = false)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // Kitap ödünçte mi?
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Borrow WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    if ((int)cmd.ExecuteScalar() > 0)
                    {
                        TempData["ErrorMessage"] = "Bu kitap hala ödünçte. Önce teslim alınmalıdır.";
                        return RedirectToAction("Delete", new { id });
                    }
                }

                int currentCount = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT bookCount FROM Book WHERE bookISBN = @isbn", conn))
                {
                    cmd.Parameters.AddWithValue("@isbn", id);
                    object result = cmd.ExecuteScalar();
                    if (result != null) currentCount = Convert.ToInt32(result);
                }

                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        if (deleteAll || currentCount <= 1)
                        {
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM BookJoinAuthor WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", id);
                                cmd.ExecuteNonQuery();
                            }
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM BookJoinType WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", id);
                                cmd.ExecuteNonQuery();
                            }
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM Book WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (SqlCommand cmd = new SqlCommand("UPDATE Book SET bookCount = bookCount - 1 WHERE bookISBN = @isbn", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@isbn", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "Silme işlemi sırasında teknik bir hata oluştu.";
                        return RedirectToAction("Delete", new { id });
                    }
                }
            }
            return RedirectToAction("Index");
        }
    }
}