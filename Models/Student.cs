using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMVC.Models
{
    [Table("Student")]
    public class Student
    {
        [Key] 
        public int Id { get; set; }
        public int? BookId { get; set; }

        [Required(ErrorMessage = "Ad Soyad alanı zorunludur")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçersiz e-posta formatı")]
        [StringLength(256)]
        public string Gmail { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        [DataType(DataType.Password)]
        [StringLength(50)]
        public string Password { get; set; }

        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }
        public string ?Permissions {  get; set; }
    }
}