namespace LibraryMVC.Models
{
    public class BorrowLog
    {
        public int BorrowId { get; set; } // PK
        public string BookISBN { get; set; }
        public string UserTC { get; set; }
        public DateTime BorrowStartDate { get; set; }
        public DateTime BorrowEndDate { get; set; }
        public int BorrowExtension { get; set; }
    }
}