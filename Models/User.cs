namespace LibraryMVC.Models
{
    public class User
    {
        public string UserTC { get; set; } // PK
        public string UserName { get; set; }
        public string UserSurname { get; set; }
        public string UserMail { get; set; }
        public string UserTel { get; set; }
        public int TypeId { get; set; } // FK -> UserType
    }
}