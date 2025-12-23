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
            _students = _context.Students.ToList();
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(Student student) // async ekledik
        {
            // 1. Önce kullanýcýyý bul
            var existStudent = _context.Students.FirstOrDefault(x => x.Id == student.Id); // Genelde Gmail ile aranýr

            // 2. Kontrol Et: Kullanýcý yoksa veya þifre yanlýþsa direkt hata dön
            if (existStudent == null || existStudent.Password != student.Password)
            {
                TempData["ErrorMessage"] = "Öðrenci numarasý veya þifre hatalý!";
                return View(student);
            }

            // 3. Baþarýlýysa: Claim'leri oluþtur
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, existStudent.Id.ToString()),
        new Claim(ClaimTypes.Email, existStudent.Gmail ?? ""),
        new Claim(ClaimTypes.Role, existStudent.Permissions ?? "Student"), // "Admin" veya "Student"
        new Claim("StudentId", existStudent.Id.ToString()) // ID'yi saklamak iþe yarar
    };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // 4. KRÝTÝK ADIM: Cookie'yi tarayýcýya gönder (Oturumu aç)
            HttpContext.SignInAsync("MyCookieAuth", principal);

            return RedirectToAction("Index", "Home");
        }
        public IActionResult Logout()
        {
            // "MyCookieAuth" isimli þemayý temizler (Cookie'yi siler)
            HttpContext.SignOutAsync("MyCookieAuth");

            // Çýkýþ yaptýktan sonra ana sayfaya yönlendir
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Contact()
        {

            return View();
        }
        [HttpPost]
        public IActionResult Contact(string Ad,int PhoneNumber, string Email, string Mesaj)
        {
            var UserMessage = new
            {
                Name = Ad,
                Gmail = Email,
                Number = PhoneNumber,
                Message = Mesaj,
            };
            return Json(UserMessage);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
