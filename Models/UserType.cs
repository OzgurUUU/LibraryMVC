namespace LibraryMVC.Models
{
    public class UserType
    {
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public int BookLimit { get; set; }
        public int BorrowPeriod { get; set; }
        public int ExtensionLimit { get; set; }
    }

    
}