namespace LibraryMVC.Models
{
    public class Penalty
    {
        public int PenaltyId { get; set; } // PK
        public string UserTC { get; set; } // FK -> User
        public int BorrowId { get; set; } // FK -> Borrow
        public decimal PenaltyAmount { get; set; }
    }
}