using HealthcareManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareManagementSystem.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed Patients if none exist, or update existing ones
            var existingPatients = await context.Patients.ToListAsync();
            if (!existingPatients.Any())
            {
                var patients = new List<Patient>
            {
                new Patient
                {
                    FirstName = "Amit",
                    LastName = "Sharma",
                    Email = "amit.sharma@email.com",
                    PhoneNumber = "555-0123",
                    DateOfBirth = new DateTime(1990, 5, 15),
                    Gender = "Male",
                    Address = "123 Main St, Mumbai, Maharashtra 400001",
                    EmergencyContactName = "Priya Sharma",
                    EmergencyContactPhone = "555-0124",
                    BloodType = "O+",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Patient
                {
                    FirstName = "Priya",
                    LastName = "Patel",
                    Email = "priya.patel@email.com",
                    PhoneNumber = "555-0234",
                    DateOfBirth = new DateTime(1985, 3, 22),
                    Gender = "Female",
                    Address = "456 Oak Ave, Delhi, Delhi 110001",
                    EmergencyContactName = "Raj Patel",
                    EmergencyContactPhone = "555-0235",
                    BloodType = "A-",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Patient
                {
                    FirstName = "Rajesh",
                    LastName = "Kumar",
                    Email = "rajesh.kumar@email.com",
                    PhoneNumber = "555-0345",
                    DateOfBirth = new DateTime(1978, 11, 8),
                    Gender = "Male",
                    Address = "789 Pine St, Bangalore, Karnataka 560001",
                    EmergencyContactName = "Sunita Kumar",
                    EmergencyContactPhone = "555-0346",
                    BloodType = "B+",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

                context.Patients.AddRange(patients);
            }
            else
            {
                // Update existing patients with Indian names
                if (existingPatients.Count >= 1)
                {
                    existingPatients[0].FirstName = "Amit";
                    existingPatients[0].LastName = "Sharma";
                    existingPatients[0].Address = "123 Main St, Mumbai, Maharashtra 400001";
                    existingPatients[0].EmergencyContactName = "Priya Sharma";
                }
                if (existingPatients.Count >= 2)
                {
                    existingPatients[1].FirstName = "Priya";
                    existingPatients[1].LastName = "Patel";
                    existingPatients[1].Address = "456 Oak Ave, Delhi, Delhi 110001";
                    existingPatients[1].EmergencyContactName = "Raj Patel";
                }
                if (existingPatients.Count >= 3)
                {
                    existingPatients[2].FirstName = "Rajesh";
                    existingPatients[2].LastName = "Kumar";
                    existingPatients[2].Address = "789 Pine St, Bangalore, Karnataka 560001";
                    existingPatients[2].EmergencyContactName = "Sunita Kumar";
                }
            }

            // Seed Doctors if none exist, or update existing ones
            var existingDoctors = await context.Doctors.ToListAsync();
            if (!existingDoctors.Any())
            {
                var doctors = new List<Doctor>
            {
                new Doctor
                {
                    FirstName = "Dr. Anjali",
                    LastName = "Verma",
                    Email = "dr.anjali.verma@hospital.com",
                    PhoneNumber = "555-1001",
                    Specialization = "Cardiology",
                    LicenseNumber = "MD12345",
                    Department = "Cardiology",
                    Qualifications = "MD from AIIMS Delhi, Board Certified Cardiologist",
                    YearsOfExperience = 15,
                    ConsultationFee = 2000.00m,
                    IsAvailable = true,
                    Biography = "Dr. Anjali Verma is a highly experienced cardiologist with over 15 years of practice.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Doctor
                {
                    FirstName = "Dr. Vikram",
                    LastName = "Singh",
                    Email = "dr.vikram.singh@hospital.com",
                    PhoneNumber = "555-1002",
                    Specialization = "Pediatrics",
                    LicenseNumber = "MD12346",
                    Department = "Pediatrics",
                    Qualifications = "MD from CMC Vellore, Board Certified Pediatrician",
                    YearsOfExperience = 12,
                    ConsultationFee = 1500.00m,
                    IsAvailable = true,
                    Biography = "Dr. Vikram Singh specializes in pediatric care with a focus on child development.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Doctor
                {
                    FirstName = "Dr. Meera",
                    LastName = "Gupta",
                    Email = "dr.meera.gupta@hospital.com",
                    PhoneNumber = "555-1003",
                    Specialization = "Orthopedics",
                    LicenseNumber = "MD12347",
                    Department = "Orthopedics",
                    Qualifications = "MS Orthopedics from PGI Chandigarh, Orthopedic Surgery Specialist",
                    YearsOfExperience = 8,
                    ConsultationFee = 2500.00m,
                    IsAvailable = true,
                    Biography = "Dr. Meera Gupta is an orthopedic surgeon specializing in sports medicine and joint replacement.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Doctor
                {
                    FirstName = "Dr. Arjun",
                    LastName = "Reddy",
                    Email = "dr.arjun.reddy@hospital.com",
                    PhoneNumber = "555-1004",
                    Specialization = "Internal Medicine",
                    LicenseNumber = "MD12348",
                    Department = "Internal Medicine",
                    Qualifications = "MD from JIPMER, Internal Medicine Specialist",
                    YearsOfExperience = 20,
                    ConsultationFee = 1800.00m,
                    IsAvailable = true,
                    Biography = "Dr. Arjun Reddy has extensive experience in internal medicine and preventive care.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

                context.Doctors.AddRange(doctors);
            }
            else
            {
                // Update existing doctors with Indian names
                if (existingDoctors.Count >= 1)
                {
                    existingDoctors[0].FirstName = "Anjali";
                    existingDoctors[0].LastName = "Verma";
                    existingDoctors[0].LicenseNumber = "MD12345";
                    existingDoctors[0].Qualifications = "MD from AIIMS Delhi, Board Certified Cardiologist";
                    existingDoctors[0].ConsultationFee = 2000.00m;
                    existingDoctors[0].Biography = "Dr. Anjali Verma is a highly experienced cardiologist with over 15 years of practice.";
                }
                if (existingDoctors.Count >= 2)
                {
                    existingDoctors[1].FirstName = "Vikram";
                    existingDoctors[1].LastName = "Singh";
                    existingDoctors[1].LicenseNumber = "MD12346";
                    existingDoctors[1].Qualifications = "MD from CMC Vellore, Board Certified Pediatrician";
                    existingDoctors[1].ConsultationFee = 1500.00m;
                    existingDoctors[1].Biography = "Dr. Vikram Singh specializes in pediatric care with a focus on child development.";
                }
                if (existingDoctors.Count >= 3)
                {
                    existingDoctors[2].FirstName = "Meera";
                    existingDoctors[2].LastName = "Gupta";
                    existingDoctors[2].LicenseNumber = "MD12347";
                    existingDoctors[2].Qualifications = "MS Orthopedics from PGI Chandigarh, Orthopedic Surgery Specialist";
                    existingDoctors[2].ConsultationFee = 2500.00m;
                    existingDoctors[2].Biography = "Dr. Meera Gupta is an orthopedic surgeon specializing in sports medicine and joint replacement.";
                }
                if (existingDoctors.Count >= 4)
                {
                    existingDoctors[3].FirstName = "Arjun";
                    existingDoctors[3].LastName = "Reddy";
                    existingDoctors[3].LicenseNumber = "MD12348";
                    existingDoctors[3].Qualifications = "MD from JIPMER, Internal Medicine Specialist";
                    existingDoctors[3].ConsultationFee = 1800.00m;
                    existingDoctors[3].Biography = "Dr. Arjun Reddy has extensive experience in internal medicine and preventive care.";
                }
            }

            // Save changes
            await context.SaveChangesAsync();
        }
    }
}
