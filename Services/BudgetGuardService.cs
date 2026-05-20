using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;

namespace ProVMSIT15.Services;

public class BudgetGuardService
{
    private readonly ApplicationDbContext _db;

    public BudgetGuardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Approved, string Message)> CheckBudgetAsync(string departmentCode, decimal amount)
    {
        var budget = await _db.DepartmentBudgets
            .FirstOrDefaultAsync(b => b.DepartmentCode == departmentCode && b.FiscalYear == DateTime.UtcNow.Year);

        if (budget == null)
            return (false, $"No budget allocation found for department '{departmentCode}' in FY{DateTime.UtcNow.Year}.");

        if (budget.RemainingBudget < amount)
            return (false, $"HTTP 400: Department Budget Exceeded. Requested: ₱{amount:N2}, Available: ₱{budget.RemainingBudget:N2}.");

        return (true, "Budget check passed.");
    }

    public async Task<(bool Approved, string Message)> EncumberAsync(string departmentCode, decimal amount)
    {
        var budget = await _db.DepartmentBudgets
            .FirstOrDefaultAsync(b => b.DepartmentCode == departmentCode && b.FiscalYear == DateTime.UtcNow.Year);

        if (budget == null)
            return (false, $"No budget allocation found for department '{departmentCode}' in FY{DateTime.UtcNow.Year}.");

        if (budget.RemainingBudget < amount)
            return (false, $"HTTP 400: Department Budget Exceeded. Requested: ₱{amount:N2}, Available: ₱{budget.RemainingBudget:N2}.");

        budget.SpentAmount += amount;
        budget.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, $"₱{amount:N2} pre-encumbered from {departmentCode}.");
    }

    public async Task DeductBudgetAsync(string departmentCode, decimal amount)
    {
        var budget = await _db.DepartmentBudgets
            .FirstOrDefaultAsync(b => b.DepartmentCode == departmentCode && b.FiscalYear == DateTime.UtcNow.Year);

        if (budget != null)
        {
            budget.SpentAmount += amount;
            budget.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RestoreBudgetAsync(string departmentCode, decimal amount)
    {
        var budget = await _db.DepartmentBudgets
            .FirstOrDefaultAsync(b => b.DepartmentCode == departmentCode && b.FiscalYear == DateTime.UtcNow.Year);

        if (budget != null)
        {
            budget.SpentAmount = Math.Max(0, budget.SpentAmount - amount);
            budget.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
