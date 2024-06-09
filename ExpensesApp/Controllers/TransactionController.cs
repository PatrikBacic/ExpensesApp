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
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ExpensesApp.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(ApplicationDbContext context, ILogger<TransactionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Transaction
        public async Task<IActionResult> Index()
        {
            // Retrieve transactions associated with the current user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId) // Filter transactions by UserId
                .ToListAsync();

            return View(transactions);
        }

        // GET: Transaction/CreateOrEdit
        public IActionResult CreateOrEdit(int id = 0)
        {
            PopulateCategories();
            if (id == 0)
                return View(new Transaction());
            else
                return View(_context.Transactions.Find(id));
        }

        // POST: Transaction/CreateOrEdit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit([Bind("TransactionId,CategoryId,Amount,Description,DateTime")] Transaction transaction)
        {
            _logger.LogInformation("CreateOrEdit POST accessed with transaction: {@Transaction}", transaction);

            if (ModelState.IsValid)
            {
                try
                {
                    // Associate the transaction with the current user
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    transaction.UserId = userId;

                    // Save the transaction
                    if (transaction.TransactionId == 0)
                    {
                        _context.Add(transaction);
                    }
                    else
                    {
                        _context.Update(transaction);
                    }
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Handle exception
                    _logger.LogError(ex, "Error occurred while saving transaction.");
                    ModelState.AddModelError("", "An error occurred while saving the transaction.");
                }
            }

            // Log ModelState errors
            foreach (var state in ModelState.Values)
            {
                foreach (var error in state.Errors)
                {
                    _logger.LogError("ModelState error: {ErrorMessage}", error.ErrorMessage);
                }
            }

            PopulateCategories();
            return View(transaction);
        }

        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Transactions == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Transactions'  is null.");
            }
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [NonAction]
        public void PopulateCategories()
        {
            // Retrieve the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Retrieve categories associated with the current user
            var categoryCollection = _context.Categories
                .Where(c => c.UserId == userId || c.UserId == null) // Include categories with no specified user
                .ToList();

            // Add a default category
            var defaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            categoryCollection.Insert(0, defaultCategory);

            // Pass the filtered categories to the view
            ViewBag.Categories = categoryCollection;
        }
    }
}
