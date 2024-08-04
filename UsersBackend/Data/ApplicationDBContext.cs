using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using UsersBackend.Models.Entities;

namespace UsersBackend.Data
{
    public class ApplicationDBContext : DbContext
    {

        public ApplicationDBContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

    }
}
