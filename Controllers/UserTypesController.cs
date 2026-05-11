using LibraryMVC.Helpers;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace LibraryMVC.Controllers
{
    public class UserTypesController : Controller
    {
        private readonly DatabaseHelper _dbHelper;
        public UserTypesController(DatabaseHelper dbHelper) => _dbHelper = dbHelper;

        private bool IsAdmin() => HttpContext.Session.GetString("admin") == "admin";

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            List<UserType> types = new List<UserType>();
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM UserType", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        types.Add(new UserType
                        {
                            TypeId = (int)reader["typeId"],
                            TypeName = reader["typeName"].ToString(),
                            BookLimit = (int)reader["bookLimit"],
                            BorrowPeriod = (int)reader["borrowPeriod"],
                            ExtensionLimit = (int)reader["extensionLimit"]
                        });
                    }
                }
            }
            return View(types);
        }

        [HttpPost]
        public IActionResult Edit(UserType ut)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                conn.Open();
                string sql = "UPDATE UserType SET typeName=@n, bookLimit=@l, borrowPeriod=@p, extensionLimit=@e WHERE typeId=@id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@n", ut.TypeName);
                    cmd.Parameters.AddWithValue("@l", ut.BookLimit);
                    cmd.Parameters.AddWithValue("@p", ut.BorrowPeriod);
                    cmd.Parameters.AddWithValue("@e", ut.ExtensionLimit);
                    cmd.Parameters.AddWithValue("@id", ut.TypeId);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
    }
}