using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryMVC.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
namespace LibraryMVC.Controllers
{
    public class BookController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookController> _logger;
        public List<Book> books = new List<Book>();
        public BookController(ILogger<BookController> logger,AppDbContext context)
        {
            _context = context;
            _logger = logger;
            
        }
        [Authorize]
        public IActionResult AddBook()
        {
            if (User.IsInRole("Admin"))
            {
                return View();
            }
                return BadRequest();
        }
        [Authorize]
        public IActionResult ManageBook()
        {
            if (User.IsInRole("Admin"))
            {
                var books = _context.Book.ToList();
                return View(books);
            }
            return BadRequest();
        }
        [HttpPost]
        public IActionResult ManageBook(Book book)
        {
            var books = _context.Book.ToList();
            _context.Book.Add(book);
            _context.SaveChanges();
            return RedirectToAction("ManageBook", "Book");
        }
        [Authorize]
        public IActionResult DeleteBook(int id)
        {
            if (User.IsInRole("Admin"))
            {
                var book = _context.Book.Find(id);
                _context.Book.Remove(book);
                _context.SaveChanges();
            return RedirectToAction("ManageBook","Book");
            }
            return BadRequest();
        }
        [Authorize]
        public IActionResult UpdateBook(int id)
        {
            if (User.IsInRole("Admin"))
            {
                var updatedbook = _context.Book.Find(id);
                return View(updatedbook);
            }
            return BadRequest();
        }
        [HttpPost]
        public IActionResult UpdateBook(Book book)
        {
            var abook = _context.Book.FirstOrDefault(x => x.BookId == book.BookId);
            abook.Title = book.Title;
            abook.Author = book.Author;
            abook.Year = book.Year;
            _context.SaveChanges();
            return RedirectToAction("ManageBook", "Book"); 
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}