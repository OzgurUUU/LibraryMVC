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
    public class UsersController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public UsersController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "admin";
        }

        // Dropdown verilerini yüklemek için yardımcı metot
        private void LoadUserTypesDropdown(SqlConnection conn, object selectedTypeId = null)
        {
            var userTypes = new List<dynamic>();
            string sql = "SELECT typeId, typeName FROM [UserType]";
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    userTypes.Add(new
                    {
                        Id = Convert.ToInt32(reader["typeId"]),
                        Display = reader["typeName"].ToString()
                    });
                }
            }
            ViewBag.typeId = new SelectList(userTypes, "Id", "Display", selectedTypeId);
        }

        // GET: Users
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            // Kullanıcıları ve rol isimlerini beraber çekmek için anonim tip veya ViewBag kullanabiliriz.
            // Burada User nesnelerini listeye eklerken TypeName bilgisini ViewBag üzerinden veya Dictionary ile view'a aktarabiliriz.
            // Daha pratik olması adına dinamik bir liste oluşturuyoruz.
            List<dynamic> users = new List<dynamic>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT u.userTC, u.userName, u.userSurname, u.userMail, u.userTel, u.typeId, t.typeName
                    FROM [User] u
                    INNER JOIN [UserType] t ON u.typeId = t.typeId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new
                        {
                            UserTC = reader["userTC"].ToString(),
                            UserName = reader["userName"].ToString(),
                            UserSurname = reader["userSurname"].ToString(),
                            UserMail = reader["userMail"].ToString(),
                            UserTel = reader["userTel"].ToString(),
                            TypeName = reader["typeName"].ToString()
                        });
                    }
                }
            }

            return View(users);
        }

        // GET: Users/Details/5
        public IActionResult Details(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (string.IsNullOrEmpty(id)) return BadRequest();

            dynamic userDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT TOP 1 u.*, t.typeName 
                    FROM [User] u 
                    LEFT JOIN [UserType] t ON u.typeId = t.typeId 
                    WHERE u.userTC = @tc";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tc", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userDetail = new
                            {
                                UserTC = reader["userTC"].ToString(),
                                UserName = reader["userName"].ToString(),
                                UserSurname = reader["userSurname"].ToString(),
                                UserMail = reader["userMail"].ToString(),
                                UserTel = reader["userTel"].ToString(),
                                TypeName = reader["typeName"].ToString()
                            };
                        }
                    }
                }
            }

            if (userDetail == null) return NotFound();
            return View(userDetail);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                LoadUserTypesDropdown(conn);
            }
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User user)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // Eski koddaki mantıksal hata giderildi: TC Kontrolü yapıyoruz.
                string checkSql = "SELECT COUNT(*) FROM [User] WHERE userTC = @tc";
                using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@tc", user.UserTC);
                    if ((int)checkCmd.ExecuteScalar() > 0)
                    {
                        ModelState.AddModelError("UserTC", "Bu TC numarası zaten kayıtlı.");
                    }
                }

                if (ModelState.IsValid)
                {
                    string insertSql = @"
                        INSERT INTO [User] (userTC, userName, userSurname, userMail, userTel, typeId) 
                        VALUES (@tc, @name, @surname, @mail, @tel, @typeId)";

                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tc", user.UserTC);
                        cmd.Parameters.AddWithValue("@name", user.UserName);
                        cmd.Parameters.AddWithValue("@surname", user.UserSurname);
                        cmd.Parameters.AddWithValue("@mail", user.UserMail ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tel", user.UserTel ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@typeId", user.TypeId);

                        cmd.ExecuteNonQuery();
                    }
                    return RedirectToAction("Index");
                }

                LoadUserTypesDropdown(conn, user.TypeId);
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public IActionResult Edit(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (string.IsNullOrEmpty(id)) return BadRequest();

            User user = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT TOP 1 * FROM [User] WHERE userTC = @tc";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tc", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                UserTC = reader["userTC"].ToString(),
                                UserName = reader["userName"].ToString(),
                                UserSurname = reader["userSurname"].ToString(),
                                UserMail = reader["userMail"].ToString(),
                                UserTel = reader["userTel"].ToString(),
                                TypeId = Convert.ToInt32(reader["typeId"])
                            };
                        }
                    }
                }

                if (user == null) return NotFound();

                LoadUserTypesDropdown(conn, user.TypeId);
            }

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User user)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = _dbHelper.GetConnection())
                {
                    conn.Open();
                    string updateSql = @"
                        UPDATE [User] 
                        SET userName = @name, userSurname = @surname, 
                            userMail = @mail, userTel = @tel, typeId = @typeId 
                        WHERE userTC = @tc";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tc", user.UserTC);
                        cmd.Parameters.AddWithValue("@name", user.UserName);
                        cmd.Parameters.AddWithValue("@surname", user.UserSurname);
                        cmd.Parameters.AddWithValue("@mail", user.UserMail ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tel", user.UserTel ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@typeId", user.TypeId);

                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Index");
            }

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                LoadUserTypesDropdown(conn, user.TypeId);
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public IActionResult Delete(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            if (string.IsNullOrEmpty(id)) return BadRequest();

            dynamic userDetail = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT TOP 1 * FROM [User] WHERE userTC = @tc";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tc", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userDetail = new
                            {
                                UserTC = reader["userTC"].ToString(),
                                UserName = reader["userName"].ToString(),
                                UserSurname = reader["userSurname"].ToString()
                            };
                        }
                    }
                }
            }

            if (userDetail == null) return NotFound();
            return View(userDetail);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();

                // Güvenlik Önlemi: Eğer kullanıcının teslim etmediği bir kitap varsa silinmesini engelliyoruz (Foreign Key patlamasın diye)
                string checkBorrowSql = "SELECT COUNT(*) FROM [Borrow] WHERE userTC = @tc";
                using (SqlCommand checkCmd = new SqlCommand(checkBorrowSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@tc", id);
                    if ((int)checkCmd.ExecuteScalar() > 0)
                    {
                        TempData["ErrorMessage"] = "Bu kullanıcının üzerinde hala ödünç kitap var. Kullanıcı silinemez.";
                        return RedirectToAction("Delete", new { id });
                    }
                }

                // Kullanıcıyı Sil
                string deleteSql = "DELETE FROM [User] WHERE userTC = @tc";
                using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@tc", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}