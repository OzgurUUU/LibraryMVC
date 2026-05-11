
using LibraryMVC.Helpers;
namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // 1. DatabaseHelper sýnýfýný sisteme tanýtýyoruz (Dependency Injection için þart)
            builder.Services.AddScoped<DatabaseHelper>();

            // 2. Session (Oturum Yönetimi) kullanabilmek için gerekli servisleri ekliyoruz
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60); // Oturum süresi
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 3. Session kullanýmýný aktif et (UYARI: UseAuthorization'dan ÖNCE yazýlmalýdýr!)
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Login}/{id?}"); // Sistem ilk açýldýðýnda Login ekranýna gitsin

            app.Run();
        }
    }
}
