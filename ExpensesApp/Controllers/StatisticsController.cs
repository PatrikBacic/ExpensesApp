using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExpensesApp.Data;
using ExpensesApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Security.Claims;

namespace ExpensesApp.Controllers
{
    [Authorize]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index(int? selectedYear, int? selectedMonth)
{
    // This Month
    DateTime today = DateTime.Today;
    int year = selectedYear ?? today.Year;
    int month = selectedMonth ?? today.Month;
    DateTime StartDate = new DateTime(year, month, 1);
    DateTime EndDate = StartDate.AddMonths(1).AddDays(-1);

    // Get the current user ID
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Transactions for the selected month for the current user
    List<Transaction> SelectedTransactions = await _context.Transactions
        .Include(x => x.Category)
        .Where(y => y.UserId == userId && y.DateTime >= StartDate && y.DateTime <= EndDate)
        .ToListAsync();

    // Total Income for the selected month
    decimal TotalMonthlyIncome = SelectedTransactions
        .Where(i => i.Category.Type == "Income")
        .Sum(j => j.Amount);
    ViewBag.TotalIncome = string.Format("€{0:F2}", TotalMonthlyIncome);

            // Total Expense for the selected month
            decimal TotalMonthlyExpense = SelectedTransactions
        .Where(i => i.Category.Type == "Expense")
        .Sum(j => j.Amount);
    ViewBag.TotalExpense = string.Format("€{0:F2}", TotalMonthlyExpense);

            // Cumulative Transactions for all time for the current user
            List<Transaction> CumulativeTransactions = await _context.Transactions
        .Include(x => x.Category)
        .Where(t => t.UserId == userId)
        .ToListAsync();

    // Total Cumulative Income
    decimal TotalCumulativeIncome = CumulativeTransactions
        .Where(i => i.Category.Type == "Income")
        .Sum(j => j.Amount);

    // Total Cumulative Expense
    decimal TotalCumulativeExpense = CumulativeTransactions
        .Where(i => i.Category.Type == "Expense")
        .Sum(j => j.Amount);

    // Balance (all-time)
    decimal Balance = TotalCumulativeIncome - TotalCumulativeExpense;
            ViewBag.Balance = string.Format("€{0:F2}", Balance);

    // 3D Pie chart
    ViewBag.CircularChartData = SelectedTransactions
        .Where(i => i.Category.Type == "Expense")
        .GroupBy(j => j.Category.CategoryId)
        .Select(k => new
        {
            categoryTitle = k.First().Category.Title,
            amount = k.Sum(j => j.Amount),
            formattedAmount = k.Sum(j => j.Amount).ToString("€0.00"),
        })
        .OrderByDescending(l => l.amount)
        .ToList();

    // Income vs Expense graph
    List<Chart3dData> IncomeSum = SelectedTransactions
        .Where(i => i.Category.Type == "Income")
        .GroupBy(j => j.DateTime)
        .Select(k => new Chart3dData()
        {
            day = k.First().DateTime.ToString("dd-MMM"),
            income = k.Sum(l => l.Amount),
            expense = 0 // Initialize expense with 0
        })
        .ToList();

    List<Chart3dData> ExpenseSum = SelectedTransactions
        .Where(i => i.Category.Type == "Expense")
        .GroupBy(j => j.DateTime)
        .Select(k => new Chart3dData()
        {
            day = k.First().DateTime.ToString("dd-MMM"),
            income = 0, // Initialize income with 0
            expense = k.Sum(l => l.Amount)
        })
        .ToList();

    var mergedData = IncomeSum.Concat(ExpenseSum)
        .GroupBy(x => x.day)
        .Select(g => new Chart3dData
        {
            day = g.Key,
            income = g.Sum(x => x.income),
            expense = g.Sum(x => x.expense)
        })
        .ToList();

    ViewBag.Chart3dData = mergedData;

    // For the dropdown lists
    int curmth = DateTime.Now.Month;
    List<int> mths = Enumerable.Range(1, 12).ToList();
    mths.Remove(curmth);
    mths.Insert(11, curmth);
    ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 5, 6);
    ViewBag.Months = mths.Select(m => new SelectListItem
    {
        Value = m.ToString(),
        Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
    });
    ViewBag.SelectedYear = year;
    ViewBag.SelectedMonth = month;

    // Cumulative balance for each month of the current year
    var cumulativeBalanceData = new List<SplineChartData>();
    decimal cumulativeBalance = 0;

    // Only calculate up to the current month if the selected year is the current year
    int lastMonth = (year == DateTime.Now.Year) ? DateTime.Now.Month : 12;

    for (int mth = 1; mth <= lastMonth; mth++)
    {
        var monthlyTransactions = CumulativeTransactions
            .Where(t => t.DateTime.Year == year && t.DateTime.Month == mth)
            .ToList();

        decimal monthlyIncome = monthlyTransactions
            .Where(t => t.Category.Type == "Income")
            .Sum(t => t.Amount);

        decimal monthlyExpense = monthlyTransactions
            .Where(t => t.Category.Type == "Expense")
            .Sum(t => t.Amount);

        cumulativeBalance += (monthlyIncome - monthlyExpense);
        cumulativeBalanceData.Add(new SplineChartData
        {
            month = new DateTime(year, mth, 1).ToString("MMM"),
            balance = cumulativeBalance
        });
    }

    ViewBag.CumulativeBalanceData = cumulativeBalanceData;

    return View();
}
        public class SplineChartData
        {
            public string month { get; set; }
            public decimal balance { get; set; }
        }

        public class Chart3dData
        {
            public string day { get; set; }
            public decimal income { get; set; }
            public decimal expense { get; set; }
        }
    }
}