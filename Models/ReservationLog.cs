namespace LibraryMVC.Models
{
    public class ReservationLog
    {
        public int ReservationId { get; set; } // PK
        public int BorrowId { get; set; }
        public string UserTC { get; set; }
        public DateTime ReservationDate { get; set; }
    }
}