using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;

namespace HealthcareManagementSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var dashboardData = new DashboardViewModel
            {
                TotalPatients = await _context.Patients.CountAsync(),
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                TotalPrescriptions = await _context.Prescriptions.CountAsync(),
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime.Date == DateTime.Today),
                ActivePrescriptions = await _context.Prescriptions
                    .CountAsync(p => p.IsActive),
                RecentPatients = await _context.Patients
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                UpcomingAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.AppointmentDateTime >= DateTime.Now)
                    .OrderBy(a => a.AppointmentDateTime)
                    .Take(5)
                    .ToListAsync()
            };

            return View(dashboardData);
        }

        // GET: Reports/PatientStatistics
        public async Task<IActionResult> PatientStatistics()
        {
            var patientStats = new PatientStatisticsViewModel
            {
                TotalPatients = await _context.Patients.CountAsync(),
                MalePatients = await _context.Patients.CountAsync(p => p.Gender == "Male"),
                FemalePatients = await _context.Patients.CountAsync(p => p.Gender == "Female"),
                AgeGroups = new Dictionary<string, int>
                {
                    ["0-18"] = await _context.Patients.CountAsync(p => DateTime.Now.Year - p.DateOfBirth.Year <= 18),
                    ["19-35"] = await _context.Patients.CountAsync(p => DateTime.Now.Year - p.DateOfBirth.Year >= 19 && DateTime.Now.Year - p.DateOfBirth.Year <= 35),
                    ["36-55"] = await _context.Patients.CountAsync(p => DateTime.Now.Year - p.DateOfBirth.Year >= 36 && DateTime.Now.Year - p.DateOfBirth.Year <= 55),
                    ["56+"] = await _context.Patients.CountAsync(p => DateTime.Now.Year - p.DateOfBirth.Year > 55)
                },
                BloodTypeDistribution = await _context.Patients
                    .Where(p => !string.IsNullOrEmpty(p.BloodType))
                    .GroupBy(p => p.BloodType)
                    .Select(g => new { BloodType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.BloodType, x => x.Count)
            };

            return View(patientStats);
        }

        // GET: Reports/AppointmentStatistics
        public async Task<IActionResult> AppointmentStatistics()
        {
            var appointmentStats = new AppointmentStatisticsViewModel
            {
                TotalAppointments = await _context.Appointments.CountAsync(),
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime.Date == DateTime.Today),
                ThisWeekAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime >= DateTime.Today.AddDays(-7)),
                ThisMonthAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime >= DateTime.Today.AddMonths(-1)),
                StatusDistribution = await _context.Appointments
                    .GroupBy(a => a.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                MonthlyAppointments = await GetMonthlyAppointmentData()
            };

            return View(appointmentStats);
        }

        // GET: Reports/DoctorStatistics
        public async Task<IActionResult> DoctorStatistics()
        {
            var doctorStats = new DoctorStatisticsViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                SpecializationDistribution = await _context.Doctors
                    .GroupBy(d => d.Specialization)
                    .Select(g => new { Specialization = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Specialization, x => x.Count),
                DepartmentDistribution = await _context.Doctors
                    .GroupBy(d => d.Department)
                    .Select(g => new { Department = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Department, x => x.Count),
                DoctorAppointmentCounts = await _context.Doctors
                    .Include(d => d.Appointments)
                    .Select(d => new { 
                        DoctorName = d.FirstName + " " + d.LastName,
                        AppointmentCount = d.Appointments.Count
                    })
                    .OrderByDescending(x => x.AppointmentCount)
                    .Take(10)
                    .ToDictionaryAsync(x => x.DoctorName, x => x.AppointmentCount)
            };

            return View(doctorStats);
        }

        private async Task<Dictionary<string, int>> GetMonthlyAppointmentData()
        {
            var monthlyData = new Dictionary<string, int>();
            var startDate = DateTime.Today.AddMonths(-11);

            for (int i = 0; i < 12; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);
                var count = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime >= monthStart && a.AppointmentDateTime < monthEnd);
                
                monthlyData[monthStart.ToString("MMM yyyy")] = count;
            }

            return monthlyData;
        }
    }

    // View Models for Reports
    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalPrescriptions { get; set; }
        public int TodayAppointments { get; set; }
        public int ActivePrescriptions { get; set; }
        public List<Patient> RecentPatients { get; set; } = new();
        public List<Appointment> UpcomingAppointments { get; set; } = new();
    }

    public class PatientStatisticsViewModel
    {
        public int TotalPatients { get; set; }
        public int MalePatients { get; set; }
        public int FemalePatients { get; set; }
        public Dictionary<string, int> AgeGroups { get; set; } = new();
        public Dictionary<string, int> BloodTypeDistribution { get; set; } = new();
    }

    public class AppointmentStatisticsViewModel
    {
        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public int ThisWeekAppointments { get; set; }
        public int ThisMonthAppointments { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        public Dictionary<string, int> MonthlyAppointments { get; set; } = new();
    }

    public class DoctorStatisticsViewModel
    {
        public int TotalDoctors { get; set; }
        public Dictionary<string, int> SpecializationDistribution { get; set; } = new();
        public Dictionary<string, int> DepartmentDistribution { get; set; } = new();
        public Dictionary<string, int> DoctorAppointmentCounts { get; set; } = new();
    }
}
