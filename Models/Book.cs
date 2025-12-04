using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryMVC.Models
{
    public class Book
    {
        public int Id { get; init; }
        public string Title { get; init; }
        public string Author { get; init; }
        public int Year { get; init; }
        public string Genre { get; init; }
    }
}