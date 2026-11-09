using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Security;

namespace WorkShiftManagement.DAL.Data;

public partial class AppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        var identityNoConverter = new ValueConverter<string, string>(
            plain => IdentityNoEncryption.Protect(plain),
            stored => IdentityNoEncryption.Unprotect(stored));

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.IdentityNo)
                .HasMaxLength(255)
                .HasConversion(identityNoConverter);
        });
    }
}

