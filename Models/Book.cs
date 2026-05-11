namespace LibraryMVC.Models
{
    public class Book
    {
        public string BookISBN { get; set; } // PK
        public string BookName { get; set; }
        public int BookNumOfPage { get; set; }
        public int BookCount { get; set; }
        public int BookPublicationYear { get; set; }
        public int PublisherId { get; set; } // FK -> BookPublisher
    }

}