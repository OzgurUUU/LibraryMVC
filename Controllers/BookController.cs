using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryMVC.Models;
namespace LibraryMVC.Controllers
{
    public class BookController : Controller
    {
        private readonly ILogger<BookController> _logger;

        public BookController(ILogger<BookController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Loan()
        {
            return View();
        }
        public IActionResult ManageBook()
        {
            var book = new Book
            {
                Title = "Masumiyet Muzesi",
                Author = "Orhan Pamuk",
                Year = 2000,
                Genre = "Roman"
            };
            return View(book);
        }
        public IActionResult ViewBook()
        {
            var book = new Book
            {
                Title = "Masumiyet Muzesi",
                Author = "Orhan Pamuk",
                Year = 2000,
                Genre = "Roman"
            };
            return Json(book);
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}