using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LocationVoiture.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LocationVoiture.BackOffice
{
    public partial class LoginWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly MainWindow _mainWindow;

        // Constructor for dependency injection if needed, 
        // but here we might need to manually resolve or pass it.
        // Simplified approach: Passed from App.xaml.cs
        public LoginWindow(ApplicationDbContext context, MainWindow mainWindow)
        {
            InitializeComponent();
            _context = context;
            _mainWindow = mainWindow;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Verify credentials against Employe table
                // NOTE: In production, use hashed passwords! Here we check plain text or hash if simple.
                // Assuming simple plain text for this demo unless we see hashing util.
                
                var employe = _context.Employes
                    .FirstOrDefault(u => u.Email == email);

                if (employe != null && employe.MotDePasseHash == password) 
                {
                    // Success!
                    // Hide Login, Show Main
                    this.Hide();
                    _mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Email ou mot de passe incorrect.", "Échec de connexion", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de connexion base de données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Allow moving the window by dragging content
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
