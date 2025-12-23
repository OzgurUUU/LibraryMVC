using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LibraryMVC.Models
{
    [Table("Book")]
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Kitap adı boş bırakılamaz")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Yazar adı boş bırakılamaz")]
        public string Author { get; set; }

        [Required(ErrorMessage = "Kitap yılı boş bırakılamaz")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Tür alanı zorunludur")]
        public string Genre { get; set; }
    }

}