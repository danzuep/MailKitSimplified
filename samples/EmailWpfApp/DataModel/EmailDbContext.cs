using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using EmailWpfApp.Models;

namespace EmailWpfApp.DataModel
{
    public class EmailDbContext : DbContext
    {
        #region Contructor
        public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
        {
            // SqlLiteException when data doesn't match schema.
            //Database.EnsureDeleted();
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
            var email = Email.Write.From("admin@localhost", "Admin");
            return new List<Email>
            {
                email.Copy()
                    .To("john.doe@example.com", "John Doe")
                    .Subject("Email to John Doe")
                    .AsEmail,
                email.Copy()
                    .To("jane.smith@example.com", "Jane Smith")
                    .Subject("Email to Jane Smith")
                    .AsEmail,
                email.Copy()
                    .To("steve.tait@example.com", "Steve Tait")
                    .Subject("Email to Steve Tait")
                    .AsEmail,
                email.Copy()
                    .To("sally.johnson@example.com", "Sally Johnson")
                    .Subject("Email to Sally Johnson")
                    .AsEmail,
            };
        }
        #endregion
    }
}
