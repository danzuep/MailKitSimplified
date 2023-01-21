using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace EmailWpfApp.DataModel
{
    public class EmailDbContext : DbContext
    {
        #region Contructor
        public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        #endregion

        #region Public properties
        public DbSet<Email> Emails { get; set; }
        #endregion

        #region Overrides
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Email>().HasData(GetEmails());
            base.OnModelCreating(modelBuilder);
        }
        #endregion

        #region Private methods
        private List<Email> GetEmails()
        {
            return new List<Email>
            {
                new Email {Id = 100, FirstName ="John", LastName = "Doe"},
                new Email {Id = 101, FirstName ="Nicole", LastName = "Martha"},
                new Email {Id = 102, FirstName ="Steve", LastName = "Johnson"},
                new Email {Id = 103, FirstName ="Thomas", LastName = "Bond"},
            };
        }
        #endregion
    }
}
