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
    int TotalMonthlyIncome = SelectedTransactions
        .Where(i => i.Category.Type == "Income")
        .Sum(j => j.Amount);
    ViewBag.TotalIncome = TotalMonthlyIncome.ToString("'€'0");

    // Total Expense for the selected month
    int TotalMonthlyExpense = SelectedTransactions
        .Where(i => i.Category.Type == "Expense")
        .Sum(j => j.Amount);
    ViewBag.TotalExpense = TotalMonthlyExpense.ToString("'€'0");

    // Cumulative Transactions for all time for the current user
    List<Transaction> CumulativeTransactions = await _context.Transactions
        .Include(x => x.Category)
        .Where(t => t.UserId == userId)
        .ToListAsync();

    // Total Cumulative Income
    int TotalCumulativeIncome = CumulativeTransactions
        .Where(i => i.Category.Type == "Income")
        .Sum(j => j.Amount);

    // Total Cumulative Expense
    int TotalCumulativeExpense = CumulativeTransactions
        .Where(i => i.Category.Type == "Expense")
        .Sum(j => j.Amount);

    // Balance (all-time)
    int Balance = TotalCumulativeIncome - TotalCumulativeExpense;
    ViewBag.Balance = Balance.ToString("'€'0");

    // 3D Pie chart
    ViewBag.CircularChartData = SelectedTransactions
        .Where(i => i.Category.Type == "Expense")
        .GroupBy(j => j.Category.CategoryId)
        .Select(k => new
        {
            categoryTitle = k.First().Category.Title,
            amount = k.Sum(j => j.Amount),
            formattedAmount = k.Sum(j => j.Amount).ToString("'€'0"),
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
    int cumulativeBalance = 0;

    // Only calculate up to the current month if the selected year is the current year
    int lastMonth = (year == DateTime.Now.Year) ? DateTime.Now.Month : 12;

    for (int mth = 1; mth <= lastMonth; mth++)
    {
        var monthlyTransactions = CumulativeTransactions
            .Where(t => t.DateTime.Year == year && t.DateTime.Month == mth)
            .ToList();

        int monthlyIncome = monthlyTransactions
            .Where(t => t.Category.Type == "Income")
            .Sum(t => t.Amount);

        int monthlyExpense = monthlyTransactions
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
            public int balance { get; set; }
        }

        public class Chart3dData
        {
            public string day { get; set; }
            public int income { get; set; }
            public int expense { get; set; }
        }
    }
}