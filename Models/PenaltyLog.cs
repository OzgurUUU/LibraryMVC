namespace LibraryMVC.Models
{
    public class PenaltyLog
    {
        public int PenaltyId { get; set; } // PK
        public int BorrowId { get; set; }
        public string UserTC { get; set; }
        public decimal PenaltyAmount { get; set; }
    }
}