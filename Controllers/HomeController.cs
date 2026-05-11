using LibraryMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using WebApplication1.Models;

namespace LibraryMVC.Controllers
{
    public class HomeController : Controller
    {
        private List<Student> _students = new List<Student>();
        private AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger,AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {

            ViewBag.OgrenciAdi = HttpContext.Session.GetString("StudentName");
            ViewBag.GirisYapildiMi = HttpContext.Session.GetInt32("StudentId") != null;

            var students = _context.Students.ToList();
            return View(students);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(Student student)
        {
            var existStudent = _context.Students.FirstOrDefault(x => x.Id == student.Id); 

            if (existStudent == null || existStudent.Password != student.Password)
            {
                ViewBag.ErrorMessage = "Hatalý giriþ!";
                return View(student);
            }


            HttpContext.Session.SetInt32("StudentId", existStudent.Id);
            HttpContext.Session.SetString("StudentName", existStudent.FullName);

            HttpContext.Session.SetString("UserRole", existStudent.Permissions ?? "Student");


            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
