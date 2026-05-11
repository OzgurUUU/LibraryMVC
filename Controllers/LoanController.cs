using LibraryMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;

namespace LibraryMVC.Controllers
{
    public class LoanController : Controller
    {
        private AppDbContext _context ;
        public List<Book> books = new List<Book>();
        public List<Loan> loans = new List<Loan>();
        public LoanController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Index()
        {
            loans = _context.Loan.Include(x => x.Book).ThenInclude(x => x.Author).Include(y => y.Student).ToList();
            return View(loans);
        }

        public IActionResult LoanForStudent()
        {
            var Userid = HttpContext.Session.GetInt32("StudentId");



            if (HttpContext.Session.GetString("UserRole") == "Student")
            {
                var student = _context.Students.Find(Userid);

                if (student == null)
                {
                    return NotFound("Öğrenci bulunamadı.");
                }

                ViewBag.Student = student;

                var books = _context.Book.Include(x => x.Author).ToList();

                return View(books);
            }
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                var books = _context.Book.ToList();
                var student = _context.Students.Find(Userid);

                ViewBag.Message = $"Hoş geldin : {student?.FullName}";
                ViewBag.Student = student;
                return RedirectToAction("Index", "Loan");
            }

            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public IActionResult LoanForStudent(Loan newLoan)
        {
            if (HttpContext.Session.GetString("UserRole") == "Student")
            {
                
                _context.Loan.Add(newLoan);
                _context.SaveChanges();
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
