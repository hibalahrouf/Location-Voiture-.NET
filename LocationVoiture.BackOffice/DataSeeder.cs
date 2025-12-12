using LocationVoiture.Core.Data;
using LocationVoiture.Core.Models;
using System.Linq;

namespace LocationVoiture.BackOffice
{
    public static class DataSeeder
    {
        public static void SeedAdmin(ApplicationDbContext context)
        {
            try
            {
                // Ensure the database is created (optional, but good for safety)
                // context.Database.EnsureCreated(); 

                string adminEmail = "admin@admin.com";
                string adminPass = "Admin123!";

                var existingAdmin = context.Employes.FirstOrDefault(e => e.Email == adminEmail);

                if (existingAdmin != null)
                {
                    // Update existing admin password
                    existingAdmin.MotDePasseHash = adminPass;
                    existingAdmin.Role = "Admin"; // Ensure role is Admin
                    context.Employes.Update(existingAdmin);
                    context.SaveChanges();
                }
                else
                {
                    // Create new admin
                    var newAdmin = new Employe
                    {
                        Nom = "System",
                        Prenom = "Admin",
                        Email = adminEmail,
                        MotDePasseHash = adminPass,
                        Role = "Admin"
                    };
                    context.Employes.Add(newAdmin);
                    context.SaveChanges();
                }
            }
            catch (System.Exception)
            {
                // Log or swallow exception to avoid crashing startup if DB is unreachable
                // Ideally log this.
            }
        }
    }
}
