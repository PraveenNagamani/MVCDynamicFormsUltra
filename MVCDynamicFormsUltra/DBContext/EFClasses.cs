using Microsoft.EntityFrameworkCore;
using MVCDynamicFormsUltra.Models;
using System.Reflection.Emit;


namespace MVCDynamicFormsUltra.DBContext
{
    public class EFClasses : DbContext
    {

        public EFClasses(DbContextOptions<EFClasses> options) : base(options)
        {

        }
      
        public DbSet<Profile> Profiles { get; set; }


        public DbSet<Nform> Nforms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            NformMapping(modelBuilder);
            ProfileMapping(modelBuilder);
        }

        private void NformMapping(ModelBuilder modelBuilder)
        {
            // Map the class to the existing table
            modelBuilder.Entity<Nform>().ToTable("CFGWIZARDPAGE").HasNoKey();

            // Map properties to columns
            modelBuilder.Entity<Nform>().Property(e => e.WIZSTEPKEY).HasColumnName("WIZSTEPKEY").IsRequired();
            modelBuilder.Entity<Nform>().Property(e => e.TABLENAME).HasColumnName("TABLENAME").IsRequired();
            modelBuilder.Entity<Nform>().Property(e => e.COLUMNNAME).HasColumnName("COLUMNNAME").IsRequired();
            modelBuilder.Entity<Nform>().Property(e => e.SQLTEXT).HasColumnName("SQLTEXT");
            modelBuilder.Entity<Nform>().Property(e => e.DISPLAYTEXT).HasColumnName("DISPLAYTEXT");
            
            base.OnModelCreating(modelBuilder);
        }

        private void ProfileMapping(ModelBuilder modelbuilder)
        {
            modelbuilder.Entity<Profile>().ToTable("USERPROFILE");

            modelbuilder.Entity<Profile>().Property(e => e.username).HasColumnName("USERNAME").IsRequired();
            modelbuilder.Entity<Profile>().Property(e => e.emailid).HasColumnName("EMAIL").IsRequired();
            modelbuilder.Entity<Profile>().Property(e => e.Photo).HasColumnName("PHOTO");

            base.OnModelCreating(modelbuilder);
        }

    }
}
