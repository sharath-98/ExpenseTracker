using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.PivotView;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        //Last 7 days transacations
        DateTime startDate = DateTime.Today.AddDays(-6);
        DateTime endDate = DateTime.Today;

        public async Task<ActionResult> Index()
        {
            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= startDate && y.Date <= endDate)
                .ToListAsync();


            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;

            //total income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(x => x.Amount);
            ViewBag.TotalIncome = String.Format(culture, "{0:C0}", TotalIncome);

            // Total Expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(x => x.Amount);
            ViewBag.TotalExpense = String.Format(culture, "{0:C0}", TotalExpense);

            // Balance
            int Balance = TotalIncome - TotalExpense;
            ViewBag.Balance = String.Format(culture,"{0:C0}", Balance);

            //Donut Chart
            ViewBag.DonutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    CategoryWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    Amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString()

                }).OrderByDescending(l => l.Amount).ToList();

            //SplineChart -- Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString(),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();


            //SplineChart -- Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(j => j.Amount)
                }).ToList();


            // Combine income and expense
            string[] last7Days = Enumerable.Range(0, 7)
                .Select(i => startDate.AddDays(i).ToString())
                .ToArray();

            ViewBag.SplineChartData = from day in last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null? 0: income.income,
                                          expense = expense == null ? 0 : expense.expense
                                      };

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
