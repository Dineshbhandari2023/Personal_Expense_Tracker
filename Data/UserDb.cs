using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Models;

public class UserDb : DbContext
{
    public DbSet<User> Users { get; set; }

    public UserDb(DbContextOptions<UserDb> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=users.db");
    }
}
