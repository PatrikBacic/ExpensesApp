using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ExpensesApp.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Amount should be greater than 0.")]
        public int Amount { get; set; }
        [Column(TypeName = "nvarchar(75)")]
        public string? Description { get; set; }

        public DateTime DateTime { get; set; } = DateTime.Now;

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        [NotMapped]
        public string? CategoryTitle
        {
            get
            {
                return Category == null ? "" : Category.Title;
            }
        }

        [NotMapped]
        public string? FormattedAmount
        {
            get
            {
                return ((Category == null || Category.Type == "Expense")? "- " : "+ ") + Amount.ToString("'€'0");
            }
        }
    }
}
