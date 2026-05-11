namespace LibraryMVC.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; } // PK
        public int BorrowId { get; set; } // FK -> Borrow
        public string UserTC { get; set; } // FK -> User
        public DateTime ReservationDate { get; set; }
    }
}