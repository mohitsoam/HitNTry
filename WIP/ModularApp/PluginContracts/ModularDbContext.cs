namespace MainHostApp
{
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;

    public class ModularDbContext : DbContext
    {
        public ModularDbContext(DbContextOptions<ModularDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
