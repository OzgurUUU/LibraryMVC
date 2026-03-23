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
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public IActionResult AddBook()
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                ViewBag.Authors = new SelectList(_context.Author.ToList(), "AuthorId", "Name");
                return View();
            }
                return BadRequest();
        }
        [HttpPost]
        public IActionResult AddBook(Book book)
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                _context.Book.Add(book);
                _context.SaveChanges();
                return RedirectToAction("ManageBook", "Book");
            }
            return View(book);
        }

        public IActionResult ManageBook()
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
           {
                
                var books = _context.Book.Include(x => x.Author).ToList();
                return View(books);
           }
            return BadRequest();
        }


        public IActionResult DeleteBook(int id)
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                var book = _context.Book.Find(id);
                _context.Book.Remove(book);
                _context.SaveChanges();
            return RedirectToAction("ManageBook","Book");
            }
            return BadRequest();
        }

        public IActionResult UpdateBook(int id)
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                ViewBag.Authors = new SelectList(_context.Author.ToList(), "AuthorId", "Name");
                var updatedbook = _context.Book.Include(x => x.Author).FirstOrDefault(x => x.BookId == id);
                return View(updatedbook);
            }
            return BadRequest();
        }
        [HttpPost]
        public IActionResult UpdateBook(Book book)
        {
            var existingBook = _context.Book.FirstOrDefault(x => x.BookId == book.BookId);
            existingBook.Title = book.Title;
            existingBook.Year = book.Year;
            existingBook.Genre = book.Genre;
            existingBook.AuthorId = book.AuthorId;
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