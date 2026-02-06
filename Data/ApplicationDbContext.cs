using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Models;

namespace HealthcareManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<MedicalRecord> MedicalRecords { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<Insurance> Insurances { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
        public DbSet<Laboratory> Laboratories { get; set; } = null!;
        public DbSet<LabTest> LabTests { get; set; } = null!;
        public DbSet<Pharmacy> Pharmacies { get; set; } = null!;
        public DbSet<Medication> Medications { get; set; } = null!;
        public DbSet<PharmacyMedication> PharmacyMedications { get; set; } = null!;
        public DbSet<PrescriptionMedication> PrescriptionMedications { get; set; } = null!;
        public DbSet<PrescriptionFulfillment> PrescriptionFulfillments { get; set; } = null!;
        public DbSet<MedicalDocument> MedicalDocuments { get; set; } = null!;
        public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<EmergencyAlert> EmergencyAlerts { get; set; } = null!;
        public DbSet<EmergencyAlertRecipient> EmergencyAlertRecipients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Patient relationships
            builder.Entity<Patient>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasMany(p => p.MedicalRecords)
                .WithOne(mr => mr.Patient)
                .HasForeignKey(mr => mr.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasMany(p => p.Prescriptions)
                .WithOne(pr => pr.Patient)
                .HasForeignKey(pr => pr.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Doctor relationships
            builder.Entity<Doctor>()
                .HasMany(d => d.Appointments)
                .WithOne(a => a.Doctor)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Doctor>()
                .HasMany(d => d.MedicalRecords)
                .WithOne(mr => mr.Doctor)
                .HasForeignKey(mr => mr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Doctor>()
                .HasMany(d => d.Prescriptions)
                .WithOne(pr => pr.Doctor)
                .HasForeignKey(pr => pr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Appointment relationships
            builder.Entity<Appointment>()
                .HasMany(a => a.MedicalRecords)
                .WithOne(mr => mr.Appointment)
                .HasForeignKey(mr => mr.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure MedicalRecord relationships
            builder.Entity<MedicalRecord>()
                .HasMany(mr => mr.Prescriptions)
                .WithOne(pr => pr.MedicalRecord)
                .HasForeignKey(pr => pr.MedicalRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes for better performance
            builder.Entity<Patient>()
                .HasIndex(p => p.Email)
                .IsUnique();

            builder.Entity<Doctor>()
                .HasIndex(d => d.Email)
                .IsUnique();

            builder.Entity<Doctor>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique();

            builder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDateTime })
                .IsUnique();

            // Configure Patient-Insurance relationship
            builder.Entity<Patient>()
                .HasMany(p => p.Insurances)
                .WithOne(i => i.Patient)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient-Invoice relationship
            builder.Entity<Patient>()
                .HasMany(p => p.Invoices)
                .WithOne(i => i.Patient)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient-LabTest relationship
            builder.Entity<Patient>()
                .HasMany(p => p.LabTests)
                .WithOne(lt => lt.Patient)
                .HasForeignKey(lt => lt.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient-MedicalDocument relationship
            builder.Entity<Patient>()
                .HasMany(p => p.MedicalDocuments)
                .WithOne(md => md.Patient)
                .HasForeignKey(md => md.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Doctor-LabTest relationship
            builder.Entity<Doctor>()
                .HasMany(d => d.LabTests)
                .WithOne(lt => lt.Doctor)
                .HasForeignKey(lt => lt.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Doctor-MedicalDocument relationship
            builder.Entity<Doctor>()
                .HasMany(d => d.MedicalDocuments)
                .WithOne(md => md.Doctor)
                .HasForeignKey(md => md.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure new entity relationships
            // Insurance configurations
            builder.Entity<Insurance>()
                .HasIndex(i => i.PolicyNumber)
                .IsUnique();

            // Invoice configurations
            builder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            builder.Entity<Invoice>()
                .HasMany(i => i.InvoiceItems)
                .WithOne(ii => ii.Invoice)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lab Test configurations
            builder.Entity<LabTest>()
                .HasIndex(lt => lt.TestCode);

            // Pharmacy configurations
            builder.Entity<Pharmacy>()
                .HasIndex(p => p.LicenseNumber)
                .IsUnique();

            builder.Entity<PharmacyMedication>()
                .HasIndex(pm => new { pm.PharmacyId, pm.MedicationId })
                .IsUnique();

            // Medication configurations
            builder.Entity<Medication>()
                .HasIndex(m => m.NDCNumber)
                .IsUnique();

            // Prescription Medication configurations
            builder.Entity<PrescriptionMedication>()
                .HasOne(pm => pm.Prescription)
                .WithMany(p => p.PrescriptionMedications)
                .HasForeignKey(pm => pm.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Medical Document configurations
            builder.Entity<MedicalDocument>()
                .HasIndex(md => md.FilePath)
                .IsUnique();

            // Payment Method configurations
            builder.Entity<PaymentMethod>()
                .HasIndex(pm => pm.Name)
                .IsUnique();

            builder.Entity<PaymentMethod>()
                .HasMany(pm => pm.Payments)
                .WithOne(p => p.PaymentMethod)
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment configurations
            builder.Entity<Payment>()
                .HasIndex(p => p.PaymentNumber)
                .IsUnique();

            builder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Payment>()
                .HasMany(p => p.PaymentTransactions)
                .WithOne(pt => pt.Payment)
                .HasForeignKey(pt => pt.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment Transaction configurations
            builder.Entity<PaymentTransaction>()
                .HasIndex(pt => pt.GatewayTransactionId);

            // Emergency Alert configurations
            builder.Entity<EmergencyAlert>()
                .HasMany(ea => ea.Recipients)
                .WithOne(ear => ear.EmergencyAlert)
                .HasForeignKey(ear => ear.EmergencyAlertId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EmergencyAlert>()
                .HasIndex(ea => ea.CreatedAt);

            builder.Entity<EmergencyAlertRecipient>()
                .HasIndex(ear => new { ear.EmergencyAlertId, ear.RecipientType, ear.RecipientId });

            // Configure EmergencyAlertRecipient-Patient relationship
            builder.Entity<EmergencyAlertRecipient>()
                .HasOne(ear => ear.Patient)
                .WithMany()
                .HasForeignKey(ear => ear.PatientId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed default data
            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            // You can add seed data here if needed
            // For now, we'll keep it empty and add data through the application
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entities = ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    if (entity.Entity.GetType().GetProperty("CreatedAt") != null)
                    {
                        entity.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                    }
                }

                if (entity.Entity.GetType().GetProperty("UpdatedAt") != null)
                {
                    entity.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }
}
