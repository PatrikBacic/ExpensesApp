using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;


namespace ExpensesApp.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        [MaxLength(80)]
        [Required(ErrorMessage = "Title is required!")]
        public string Title { get; set; }
        [MaxLength(10)]
        public string Type { get; set; } = "Expense";  //default type

        public string UserId { get; set; }
        public IdentityUser User { get; set; }
    }
}
