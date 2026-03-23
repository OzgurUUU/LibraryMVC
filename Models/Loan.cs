using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMVC.Models
{
    [Table("Loan")]
    public class Loan
    {
        [Key]
        public int LoanId { get; set; }


        [Required(ErrorMessage = "Kitap seçimi zorunludur")]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }


        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }


        [DataType(DataType.Date)]
        public DateTime LoanDate { get; set; } = DateTime.Now;


        [Required(ErrorMessage = "İade tarihi seçilmelidir")]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }

        public string? AdditionalNotes { get; set; }


        public bool IsReturned { get; set; } = false;
    }
}