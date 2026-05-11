namespace LibraryMVC.Models
{
    public class Borrow
    {
        public int BorrowId { get; set; } // PK
        public string BookISBN { get; set; } // FK -> Book
        public string UserTC { get; set; } // FK -> User
        public DateTime BorrowStartDate { get; set; }
        public DateTime BorrowEndDate { get; set; }
        public int BorrowExtensions { get; set; }
    }
}