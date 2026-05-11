namespace LibraryMVC.Models
{
    public class BookJoinType
    {
        public int Id { get; set; } // PK
        public string BookISBN { get; set; } // FK -> Book
        public int TypeId { get; set; } // FK -> BookType
    }
}