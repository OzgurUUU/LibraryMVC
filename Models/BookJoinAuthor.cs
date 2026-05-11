namespace LibraryMVC.Models
{
    public class BookJoinAuthor
    {
        public int Id { get; set; } // PK
        public string BookISBN { get; set; } // FK -> Book
        public int AuthorId { get; set; } // FK -> BookAuthor
    }
}