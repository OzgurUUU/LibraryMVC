using LibraryMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace LibraryMVC.Controllers
{
    public class UserController : Controller
    {
        private AppDbContext _context;
        private List<Student> _students = new List<Student>();
        public UserController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            _students = _context.Students.Include(x => x.Book).ToList();
            if(_students != null)
            {
                return View(_students);
            }
            return NotFound();
        }
    }
}
