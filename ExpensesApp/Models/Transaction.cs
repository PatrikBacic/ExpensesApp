using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ExpensesApp.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int Amount { get; set; }

        public string? Description { get; set; }

        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
