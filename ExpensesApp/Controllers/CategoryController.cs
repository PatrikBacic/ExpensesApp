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
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ExpensesApp.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CategoryController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var categories = await _context.Categories
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            return View(categories);
        }

        // GET: Category/Create
        public IActionResult CreateOrEdit(int id = 0)
        {
            if (id == 0)
            {
                return View(new Category());
            }
            else
            {
                var category = _context.Categories.Find(id);
                return View(category);
            }
        }

        // POST: Category/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit([Bind("CategoryId,Title,Type")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Check if category with the same title already exists for the current user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Title == category.Title && c.UserId == userId);

                if (existingCategory != null && existingCategory.CategoryId != category.CategoryId)
                {
                    // Category with the same title exists, add a model state error
                    ModelState.AddModelError("Title", "A category with this title already exists.");
                    return View(category);
                }

                if (category.CategoryId == 0)
                {
                    // New category creation
                    category.UserId = userId;
                    _context.Add(category);
                }
                else
                {
                    // Update existing category
                    var existingCategoryToUpdate = await _context.Categories.FindAsync(category.CategoryId);
                    if (existingCategoryToUpdate != null)
                    {
                        existingCategoryToUpdate.Title = category.Title;
                        existingCategoryToUpdate.Type = category.Type;
                        _context.Update(existingCategoryToUpdate);
                    }
                    else
                    {
                        return NotFound();
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
            }
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
