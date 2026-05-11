using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class BookTypesController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public BookTypesController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // GET: BookTypes
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            List<BookType> bookTypes = new List<BookType>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT typeId, typeName FROM [BookType]";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bookTypes.Add(new BookType
                        {
                            TypeId = Convert.ToInt32(reader["typeId"]),
                            TypeName = reader["typeName"].ToString()
                        });
                    }
                }
            }

            return View(bookTypes);
        }

        // GET: BookTypes/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        // POST: BookTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BookType bookType)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "INSERT INTO [BookType] (typeName) VALUES (@name)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", bookType.TypeName);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            return View(bookType);
        }

        // GET: BookTypes/Edit/5
        public IActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            BookType bookType = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT typeId, typeName FROM [BookType] WHERE typeId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bookType = new BookType
                            {
                                TypeId = Convert.ToInt32(reader["typeId"]),
                                TypeName = reader["typeName"].ToString()
                            };
                        }
                    }
                }
            }

            if (bookType == null) return NotFound();
            return View(bookType);
        }

        // POST: BookTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BookType bookType)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE [BookType] SET typeName = @name WHERE typeId = @id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", bookType.TypeName);
                        cmd.Parameters.AddWithValue("@id", bookType.TypeId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(bookType);
        }

        // GET: BookTypes/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (id == null) return BadRequest();

            BookType bookType = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT typeId, typeName FROM [BookType] WHERE typeId = @id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bookType = new BookType
                            {
                                TypeId = Convert.ToInt32(reader["typeId"]),
                                TypeName = reader["typeName"].ToString()
                            };
                        }
                    }
                }
            }

            if (bookType == null) return NotFound();
            return View(bookType);
        }

        // POST: BookTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // 1. Bu türe ait kitaplardan herhangi biri şu anda ödünçte mi? (Performanslı ve güvenli tek sorgu)
                string checkBorrowSql = @"
                    SELECT COUNT(*) 
                    FROM [Borrow] br
                    INNER JOIN [BookJoinType] bjt ON br.bookISBN = bjt.bookISBN
                    WHERE bjt.typeId = @id";

                using (SqlCommand cmd = new SqlCommand(checkBorrowSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int borrowedCount = (int)cmd.ExecuteScalar();

                    if (borrowedCount > 0)
                    {
                        ModelState.AddModelError("", "Silmek istediğiniz türe ait bazı kitaplar şu anda ödünçte. Önce teslim alınmaları gerekmektedir.");
                        // View'ı tekrar yükleyebilmek için modeli tekrar doldurup gönderiyoruz
                        return View(new BookType { TypeId = id });
                    }
                }

                // 2. İşlem Bütünlüğü (Transaction) Başlat
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 3. Bu türe ait tüm kitapların ISBN'lerini çek
                        List<string> booksToDelete = new List<string>();
                        string getBooksSql = "SELECT bookISBN FROM [BookJoinType] WHERE typeId = @id";

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

                        // 4. Kitapları ve tüm bağlantılarını döngü ile sil
                        if (booksToDelete.Count > 0)
                        {
                            foreach (var isbn in booksToDelete)
                            {
                                // Yazar bağlantılarını sil
                                using (SqlCommand cmd = new SqlCommand("DELETE FROM [BookJoinAuthor] WHERE bookISBN = @isbn", conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@isbn", isbn);
                                    cmd.ExecuteNonQuery();
                                }

                                // Tür bağlantılarını sil
                                using (SqlCommand cmd = new SqlCommand("DELETE FROM [BookJoinType] WHERE bookISBN = @isbn", conn, transaction))
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
                        }

                        // 5. Son olarak BookType (Tür) kaydını sil
                        string deleteTypeSql = "DELETE FROM [BookType] WHERE typeId = @id";
                        using (SqlCommand cmd = new SqlCommand(deleteTypeSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Her şey başarılıysa işlemleri onayla
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        // Hata çıkarsa hiçbir şeyi silme, geri al
                        transaction.Rollback();
                        ModelState.AddModelError("", "Silme işlemi sırasında veritabanında bir hata oluştu.");
                        return View(new BookType { TypeId = id });
                    }
                }
            }

            return RedirectToAction("Index");
        }
    }
}