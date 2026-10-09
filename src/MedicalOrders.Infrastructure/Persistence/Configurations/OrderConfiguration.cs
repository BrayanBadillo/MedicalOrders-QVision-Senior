using MedicalOrders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MedicalOrders.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    // SQLite devuelve DateTime con Kind=Unspecified; se fuerza UTC al leer.
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter =
        new(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.PatientId).IsRequired().HasMaxLength(50);
        builder.Property(o => o.PatientName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.ServiceCode).IsRequired().HasMaxLength(50);
        builder.Property(o => o.ServiceDescription).IsRequired().HasMaxLength(500);

        builder.Property(o => o.Priority).HasConversion<string>().IsRequired().HasMaxLength(20);

        // El estado actúa como token de concurrencia: UPDATE ... WHERE Id = @id AND Status = @estadoOriginal.
        // Así solo un Worker puede mover una orden de Pendiente a EnProceso.
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20)
            .IsConcurrencyToken();

        builder.Property(o => o.CreatedAt).HasConversion(UtcConverter);
        builder.Property(o => o.ProcessingStartedAt).HasConversion(NullableUtcConverter);
        builder.Property(o => o.ProcessedAt).HasConversion(NullableUtcConverter);
        builder.Property(o => o.FailureReason).HasMaxLength(1000);

        builder.HasIndex(o => o.PatientId);
        builder.HasIndex(o => new { o.Status, o.Priority, o.CreatedAt });
    }
}
