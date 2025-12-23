using LibraryMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models;

namespace LibraryMVC.Controllers
{
    public class LoanController : Controller
    {
        private AppDbContext _context ;
        public List<Book> books = new List<Book>();
        public LoanController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Index()
        {
            books = _context.Book.ToList();
            return View(books);
        }
        [Authorize] 
        public IActionResult LoanForStudent()
        {
            var Userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            

            if (User.IsInRole("Student"))
            {
                var books = _context.Book.ToList();
                var student = _context.Students.Find(int.Parse(Userid));

                ViewBag.Message = $"Hoş geldin : {student?.FullName}";
                ViewBag.Student = student;
                return View(books);
            }
            if (User.IsInRole("Admin"))
            {
                var books = _context.Book.ToList();
                var student = _context.Students.Find(int.Parse(Userid));

                ViewBag.Message = $"Hoş geldin : {student?.FullName}";
                ViewBag.Student = student;
                return RedirectToAction("Index", "Loan");
            }

            return RedirectToAction("Index", "Home");
        }

    }
}
