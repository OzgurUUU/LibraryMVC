using LibraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<Book> Book { get; set; } 
        public DbSet<Student> Students { get; set; }
    }
}
