using LocationVoiture.Core.Data;
using LocationVoiture.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic; // Pour List<>
using Serilog;
// --- AJOUTS POUR L'EXPORT CSV ---
using CsvHelper;
using System.IO;
using System.Globalization;
using Microsoft.Win32;

// --- AJOUTS POUR LE PDF (QuestPDF) ---
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;

// --- AJOUTS POUR LE QR CODE ---
using QRCoder;

// --- EMAIL SERVICE ---
using LocationVoiture.BackOffice.Services;

namespace LocationVoiture.BackOffice
{
    // Ajouter 'INotifyPropertyChanged' pour les graphiques
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        // ======================================================================
        // || DÉCLARATIONS DES VARIABLES
        // ======================================================================

        private readonly ApplicationDbContext _context;
        private readonly BackOfficeEmailService _emailService;

        // Variables pour les onglets
        private TypeVehicule? _selectedTypeVehicule = null;
        private Tarif? _selectedTarif = null;
        private Vehicule? _selectedVehicule = null;
        private Client? _selectedClient = null;
        private Employe? _selectedEmploye = null;
        private Location? _selectedLocation = null;
        private Location? _selectedLocationForPaiement = null;
        private Entretien? _selectedEntretien = null;

        // ==========================================================
        // || PROPRIÉTÉS POUR LES GRAPHIQUES ET ALERTES
        // ==========================================================
        // CHARTS REMOVED FOR STABILITY

        // --- NOUVEAU : Propriété pour la grille d'alertes ---
        public List<AlerteEntretienViewModel> VehiculesAlerteEntretien { get; set; }

        // Événement requis pour INotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        // ==========================================================


        // ======================================================================
        // || CONSTRUCTEUR ET CHARGEMENT
        // ======================================================================

        public MainWindow(ApplicationDbContext context, BackOfficeEmailService emailService)
        {
            InitializeComponent();
            _context = context;
            _emailService = emailService;

            QuestPDF.Settings.License = LicenseType.Community;

            VehiculesAlerteEntretien = new List<AlerteEntretienViewModel>(); // Initialiser la liste

            this.DataContext = this;
        }

        private bool _isDarkMode = false;

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = ThemeToggle.IsChecked == true;
            ApplyTheme(_isDarkMode);
        }

        private void ApplyTheme(bool isDark)
        {
            var nightBlue = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0A1A2F");
            var nightBlueDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#051222");
            var nightBlueLight = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#132B4A");
            var gold = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#C9A44C");
            var textLight = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8FAFC");
            var mutedDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8");
            
            if (isDark)
            {
                // Dark theme - FrontOffice Style
                MainGrid.Background = new System.Windows.Media.SolidColorBrush(nightBlue);
                ContentGrid.Background = new System.Windows.Media.SolidColorBrush(nightBlue);
                HeaderBar.Background = new System.Windows.Media.SolidColorBrush(nightBlueLight);
                HeaderBar.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e3a5f"));
                PageTitle.Foreground = new System.Windows.Media.SolidColorBrush(textLight);
                ToggleSidebarBtn.Foreground = new System.Windows.Media.SolidColorBrush(textLight);
                
                // Update Resources for dark mode
                Resources["PrimaryBackground"] = new System.Windows.Media.SolidColorBrush(nightBlue);
                Resources["SecondaryBackground"] = new System.Windows.Media.SolidColorBrush(nightBlueLight);
                Resources["TextDark"] = new System.Windows.Media.SolidColorBrush(textLight);
                Resources["TextMuted"] = new System.Windows.Media.SolidColorBrush(mutedDark);
                Resources["BorderColor"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e3a5f"));
                Resources["RowAlt"] = new System.Windows.Media.SolidColorBrush(nightBlueDark);
                Resources["RowHover"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a3a5c"));
            }
            else
            {
                // Light theme - Default
                MainGrid.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f4f6f8"));
                ContentGrid.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f4f6f8"));
                HeaderBar.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff"));
                HeaderBar.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e2e8f0"));
                PageTitle.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0B1F3F"));
                ToggleSidebarBtn.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0B1F3F"));
                    
                // Update Resources for light mode
                Resources["PrimaryBackground"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f4f6f8"));
                Resources["SecondaryBackground"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff"));
                Resources["TextDark"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a202c"));
                Resources["TextMuted"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748b"));
                Resources["BorderColor"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e2e8f0"));
                Resources["RowAlt"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f8fafc"));
                Resources["RowHover"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fef3c7"));
            }
        }


        // ======================================================================
        // || NAVIGATION SIDEBAR
        // ======================================================================
        private bool _sidebarCollapsed = false;
        
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            SidebarColumn.Width = _sidebarCollapsed ? new GridLength(0) : new GridLength(260);
            SidebarBorder.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 0; PageTitle.Text = "Tableau de Bord"; }
        private void NavMessages_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 1; PageTitle.Text = "Messages Contact"; }
        private void NavClients_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 2; PageTitle.Text = "Gestion des Clients"; }
        private void NavVehicules_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 3; PageTitle.Text = "Gestion des Véhicules"; }
        private void NavLocations_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 4; PageTitle.Text = "Gestion des Locations"; }
        private void NavTypes_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 5; PageTitle.Text = "Gestion des Types"; }
        private void NavTarifs_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 6; PageTitle.Text = "Gestion des Tarifs"; }
        private void NavEmployes_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 7; PageTitle.Text = "Gestion des Employés"; }
        private void NavEntretiens_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 8; PageTitle.Text = "Gestion des Entretiens"; }
        private void NavPaiements_Click(object sender, RoutedEventArgs e) { MainTabControl.SelectedIndex = 9; PageTitle.Text = "Gestion des Paiements"; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Application BackOffice démarrée.");
                LoadDashboard();
                LoadTypesVehicules();
                LoadTarifs();
                cmbTarifTypeVehicule.ItemsSource = _context.TypesVehicules.AsNoTracking().ToList();
                LoadVehicules();
                LoadTypesVehiculesComboBox();
                LoadClients();
                LoadEmployes();
                LoadLocationsTab();
                LoadStatutComboBox();
                LoadLocationsForPaiementTab();
                LoadEntretiens();
                cmbEntretienVehicule.ItemsSource = _context.Vehicules.AsNoTracking().ToList();

                // Load Messages (New)
                LoadContactMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la connexion à la base de données : \n\n{ex.Message}", "Erreur Critique");
            }
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET : MESSAGES CONTACT
        // ======================================================================
        private void LoadContactMessages()
        {
            try
            {
                MessagesGrid.ItemsSource = _context.ContactMessages.AsNoTracking().OrderByDescending(m => m.DateSent).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement messages: {ex.Message}");
            }
        }
        
        private void MessagesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Placeholder for future actions (e.g. Reply)
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET 1 : TABLEAU DE BORD (Mis à jour)
        // ======================================================================
        private void LoadDashboard()
        {
            // ==========================================================
            // || STATISTIQUES DASHBOARD
            // ==========================================================
            try
            {
                // Basic counts
                var totalClients = _context.Clients.Count();
                var totalVehicules = _context.Vehicules.Count();
                var totalLocations = _context.Locations.Count();
                var totalRevenu = _context.Locations.Sum(l => (decimal?)l.MontantTotal) ?? 0;
                
                txtTotalClients.Text = totalClients.ToString();
                txtTotalVehicules.Text = totalVehicules.ToString();
                txtTotalLocations.Text = totalLocations.ToString();
                txtTotalRevenu.Text = totalRevenu.ToString("N0") + " €";
                
                // Circular Charts Data
                // Active reservations (Confirmee status)
                var activeReservations = _context.Locations.Count(l => l.Statut == LocationStatut.Confirmee);
                var pendingReservations = _context.Locations.Count(l => l.Statut == LocationStatut.EnAttente);
                txtActiveReservations.Text = activeReservations.ToString();
                txtReservationsPending.Text = pendingReservations + " en attente";
                
                // Available vehicles (not currently rented)
                var rentedVehicleIds = _context.Locations
                    .Where(l => l.Statut == LocationStatut.Confirmee)
                    .Select(l => l.VehiculeID)
                    .Distinct()
                    .ToList();
                var availableVehicles = totalVehicules - rentedVehicleIds.Count;
                txtVehiclesAvailable.Text = availableVehicles.ToString();
                txtVehiclesRented.Text = rentedVehicleIds.Count + " en location";
                
                // Monthly revenue
                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var monthlyRevenue = _context.Paiements
                    .Where(p => p.DatePaiement >= startOfMonth)
                    .Sum(p => (decimal?)p.Montant) ?? 0;
                var monthlyPaymentsCount = _context.Paiements
                    .Count(p => p.DatePaiement >= startOfMonth);
                txtMonthlyRevenue.Text = monthlyRevenue.ToString("N0") + "€";
                txtPaymentsPending.Text = monthlyPaymentsCount + " paiements";
            }
            catch
            {
                // Fallback values if database not available
                txtTotalClients.Text = "0";
                txtTotalVehicules.Text = "0";
                txtTotalLocations.Text = "0";
                txtTotalRevenu.Text = "0 €";
                txtActiveReservations.Text = "0";
                txtReservationsPending.Text = "0 en attente";
                txtVehiclesAvailable.Text = "0";
                txtVehiclesRented.Text = "0 en location";
                txtMonthlyRevenue.Text = "0€";
                txtPaymentsPending.Text = "0 paiements";
            }

            // ==========================================================
            // || ALERTE ENTRETIEN
            // ==========================================================

            var dateLimite = DateTime.Now.AddMonths(-6); // 6 mois en arrière

            // 1. Récupérer tous les véhicules avec leur dernier entretien
            var vehiculesAvecEntretien = _context.Vehicules
                .Select(v => new AlerteEntretienViewModel
                {
                    VehiculeID = v.VehiculeID,
                    Immatriculation = v.Immatriculation,
                    Modele = v.Modele,
                    // Récupère la date la plus récente, ou 'null' s'il n'y en a pas
                    DateDernierEntretien = v.Entretiens.Any() ? v.Entretiens.Max(e => e.DateEntretien) : (DateTime?)null
                })
                .ToList(); // Exécute la requête

            // 2. Filtrer en C#
            VehiculesAlerteEntretien = vehiculesAvecEntretien
                .Where(v => v.DateDernierEntretien == null || v.DateDernierEntretien < dateLimite) // 6 mois
                .ToList();

            // 3. Mettre à jour la grille
            AlertesEntretienGrid.ItemsSource = VehiculesAlerteEntretien;
        }



        // ======================================================================
        // || LOGIQUE DE L'ONGLET 2 : TYPES
        // ======================================================================

        private void LoadTypesVehicules()
        {
            TypesVehiculesGrid.ItemsSource = _context.TypesVehicules.AsNoTracking().ToList();
        }
        private void TypesVehiculesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedTypeVehicule = TypesVehiculesGrid.SelectedItem as TypeVehicule;
            if (_selectedTypeVehicule != null)
            {
                txtNom.Text = _selectedTypeVehicule.Nom;
                txtDescription.Text = _selectedTypeVehicule.Description;
            }
        }
        private void btnAjouter_Click(object sender, RoutedEventArgs e)
        {
            var newType = new TypeVehicule { Nom = txtNom.Text, Description = txtDescription.Text };
            _context.TypesVehicules.Add(newType);
            _context.SaveChanges();
            LoadTypesVehicules(); LoadTypesVehiculesComboBox(); cmbTarifTypeVehicule.ItemsSource = _context.TypesVehicules.AsNoTracking().ToList(); LoadDashboard();
            btnEffacer_Click(sender, e);
        }
        private void btnMettreAJour_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTypeVehicule == null) return;
            var typeToUpdate = _context.TypesVehicules.Find(_selectedTypeVehicule.TypeVehiculeID);
            if (typeToUpdate != null)
            {
                typeToUpdate.Nom = txtNom.Text;
                typeToUpdate.Description = txtDescription.Text;
                _context.SaveChanges();
                LoadTypesVehicules(); LoadTypesVehiculesComboBox(); cmbTarifTypeVehicule.ItemsSource = _context.TypesVehicules.AsNoTracking().ToList(); LoadDashboard();
                btnEffacer_Click(sender, e);
            }
        }
        private void btnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTypeVehicule == null) return;
            var typeToDelete = _context.TypesVehicules.Find(_selectedTypeVehicule.TypeVehiculeID);
            if (typeToDelete != null)
            {
                _context.TypesVehicules.Remove(typeToDelete);
                _context.SaveChanges();
                LoadTypesVehicules(); LoadTypesVehiculesComboBox(); cmbTarifTypeVehicule.ItemsSource = _context.TypesVehicules.AsNoTracking().ToList(); LoadDashboard();
                btnEffacer_Click(sender, e);
            }
        }
        private void btnEffacer_Click(object sender, RoutedEventArgs e)
        {
            txtNom.Text = ""; txtDescription.Text = "";
            _selectedTypeVehicule = null; TypesVehiculesGrid.SelectedItem = null;
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET 3 : TARIFS
        // ======================================================================

        private void LoadTarifs()
        {
            TarifsGrid.ItemsSource = _context.Tarifs.Include(t => t.TypeVehicule).AsNoTracking().ToList();
        }
        private void TarifsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedTarif = TarifsGrid.SelectedItem as Tarif;
            if (_selectedTarif != null)
            {
                cmbTarifTypeVehicule.SelectedValue = _selectedTarif.TypeVehiculeID;
                txtTarifPrix.Text = _selectedTarif.PrixParJour.ToString();
            }
        }
        private void btnAjouterTarif_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbTarifTypeVehicule.SelectedValue == null) { MessageBox.Show("Veuillez sélectionner un type."); return; }
                var newTarif = new Tarif
                {
                    TypeVehiculeID = (int)cmbTarifTypeVehicule.SelectedValue,
                    PrixParJour = decimal.Parse(txtTarifPrix.Text)
                };
                _context.Tarifs.Add(newTarif);
                _context.SaveChanges();
                LoadTarifs();
                btnEffacerTarif_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
        }
        private void btnMettreAJourTarif_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTarif == null) return;
            var tarifToUpdate = _context.Tarifs.Find(_selectedTarif.TarifID);
            if (tarifToUpdate != null)
            {
                try
                {
                    if (cmbTarifTypeVehicule.SelectedValue == null) { MessageBox.Show("Veuillez sélectionner un type."); return; }
                    tarifToUpdate.TypeVehiculeID = (int)cmbTarifTypeVehicule.SelectedValue;
                    tarifToUpdate.PrixParJour = decimal.Parse(txtTarifPrix.Text);
                    _context.SaveChanges();
                    LoadTarifs();
                    btnEffacerTarif_Click(sender, e);
                }
                catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
            }
        }
        private void btnSupprimerTarif_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTarif == null) return;
            var tarifToDelete = _context.Tarifs.Find(_selectedTarif.TarifID);
            if (tarifToDelete != null)
            {
                _context.Tarifs.Remove(tarifToDelete);
                _context.SaveChanges();
                LoadTarifs();
                btnEffacerTarif_Click(sender, e);
            }
        }
        private void btnEffacerTarif_Click(object sender, RoutedEventArgs e)
        {
            cmbTarifTypeVehicule.SelectedValue = null;
            txtTarifPrix.Text = "";
            _selectedTarif = null; TarifsGrid.SelectedItem = null;
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET 4 : VÉHICULES
        // ======================================================================

        private void LoadTypesVehiculesComboBox()
        {
            cmbTypeVehicule.ItemsSource = _context.TypesVehicules.AsNoTracking().ToList();
        }
        private void LoadVehicules()
        {
            VehiculesGrid.ItemsSource = _context.Vehicules.Include(v => v.TypeVehicule).AsNoTracking().ToList();
        }
        private void VehiculesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedVehicule = VehiculesGrid.SelectedItem as Vehicule;
            if (_selectedVehicule != null)
            {
                txtImmatriculation.Text = _selectedVehicule.Immatriculation;
                txtMarque.Text = _selectedVehicule.Marque;
                txtModele.Text = _selectedVehicule.Modele;
                txtAnnee.Text = _selectedVehicule.Annee.ToString();
                txtImageURL.Text = _selectedVehicule.ImageURL;
                txtQuantiteTotal.Text = _selectedVehicule.QuantiteTotal.ToString();
                cmbTypeVehicule.SelectedValue = _selectedVehicule.TypeVehiculeID;
                
                // Load images for this vehicle
                LoadVehiculeImages(_selectedVehicule.VehiculeID);
            }
            else
            {
                lstVehiculeImages.ItemsSource = null;
            }
        }
        
        private void LoadVehiculeImages(int vehiculeId)
        {
            var images = _context.Set<VehiculeImage>()
                .Where(i => i.VehiculeID == vehiculeId)
                .AsNoTracking()
                .ToList();
            lstVehiculeImages.ItemsSource = images;
        }
        private void btnAjouterVehicule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbTypeVehicule.SelectedValue == null) { MessageBox.Show("Veuillez sélectionner un type."); return; }
                var newVehicule = new Vehicule
                {
                    Immatriculation = txtImmatriculation.Text,
                    Marque = txtMarque.Text,
                    Modele = txtModele.Text,
                    Annee = int.Parse(txtAnnee.Text),
                    ImageURL = txtImageURL.Text,
                    QuantiteTotal = int.TryParse(txtQuantiteTotal.Text, out int q) ? q : 1,
                    QuantiteDisponible = int.TryParse(txtQuantiteTotal.Text, out int qd) ? qd : 1,
                    TypeVehiculeID = (int)cmbTypeVehicule.SelectedValue
                };
                _context.Vehicules.Add(newVehicule);
                _context.SaveChanges();
                LoadVehicules(); LoadDashboard();
                cmbEntretienVehicule.ItemsSource = _context.Vehicules.AsNoTracking().ToList();
                btnEffacerVehicule_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
        }
        private void btnMettreAJourVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVehicule == null) return;
            var vehiculeToUpdate = _context.Vehicules.Find(_selectedVehicule.VehiculeID);
            if (vehiculeToUpdate != null)
            {
                try
                {
                    if (cmbTypeVehicule.SelectedValue == null) { MessageBox.Show("Veuillez sélectionner un type."); return; }
                    vehiculeToUpdate.Immatriculation = txtImmatriculation.Text;
                    vehiculeToUpdate.Marque = txtMarque.Text;
                    vehiculeToUpdate.Modele = txtModele.Text;
                    vehiculeToUpdate.Annee = int.Parse(txtAnnee.Text);
                    vehiculeToUpdate.ImageURL = txtImageURL.Text;
                    
                    if (int.TryParse(txtQuantiteTotal.Text, out int newTotal))
                    {
                         int diff = newTotal - vehiculeToUpdate.QuantiteTotal;
                         vehiculeToUpdate.QuantiteTotal = newTotal;
                         vehiculeToUpdate.QuantiteDisponible = Math.Max(0, vehiculeToUpdate.QuantiteDisponible + diff);
                    }
                    
                    vehiculeToUpdate.TypeVehiculeID = (int)cmbTypeVehicule.SelectedValue;
                    _context.SaveChanges();
                    LoadVehicules(); LoadDashboard();
                    cmbEntretienVehicule.ItemsSource = _context.Vehicules.AsNoTracking().ToList();
                    btnEffacerVehicule_Click(sender, e);
                }
                catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
            }
        }
        private void btnSupprimerVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVehicule == null) return;
            var vehiculeToDelete = _context.Vehicules.Find(_selectedVehicule.VehiculeID);
            if (vehiculeToDelete != null)
            {
                _context.Vehicules.Remove(vehiculeToDelete);
                _context.SaveChanges();
                LoadVehicules(); LoadDashboard();
                cmbEntretienVehicule.ItemsSource = _context.Vehicules.AsNoTracking().ToList();
                btnEffacerVehicule_Click(sender, e);
            }
        }
        private void btnEffacerVehicule_Click(object sender, RoutedEventArgs e)
        {
            txtImmatriculation.Text = ""; txtMarque.Text = ""; txtModele.Text = "";
            txtAnnee.Text = ""; txtImageURL.Text = "";
            txtQuantiteTotal.Text = "";
            cmbTypeVehicule.SelectedValue = null; _selectedVehicule = null;
            VehiculesGrid.SelectedItem = null;
        }

        private void btnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*",
                Title = "Sélectionner une image pour le véhicule"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Copy to FrontOffice wwwroot/images folder
                    var frontOfficeImages = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "LocationVoiture.FrontOffice", "wwwroot", "images", "vehicules"
                    );
                    frontOfficeImages = System.IO.Path.GetFullPath(frontOfficeImages);

                    if (!System.IO.Directory.Exists(frontOfficeImages))
                    {
                        System.IO.Directory.CreateDirectory(frontOfficeImages);
                    }

                    var fileName = $"{Guid.NewGuid()}{System.IO.Path.GetExtension(openFileDialog.FileName)}";
                    var destPath = System.IO.Path.Combine(frontOfficeImages, fileName);

                    System.IO.File.Copy(openFileDialog.FileName, destPath, true);

                    // Set relative URL for web access
                    txtImageURL.Text = $"/images/vehicules/{fileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la copie de l'image: {ex.Message}", "Erreur");
                }
            }
        }

        private void btnAjouterVehiculeImage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVehicule == null)
            {
                MessageBox.Show("Veuillez sélectionner un véhicule d'abord.", "Attention");
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*",
                Title = "Ajouter une photo au véhicule",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var frontOfficeImages = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "LocationVoiture.FrontOffice", "wwwroot", "images", "vehicules"
                    );
                    frontOfficeImages = System.IO.Path.GetFullPath(frontOfficeImages);

                    if (!System.IO.Directory.Exists(frontOfficeImages))
                    {
                        System.IO.Directory.CreateDirectory(frontOfficeImages);
                    }

                    foreach (var filePath in openFileDialog.FileNames)
                    {
                        var fileName = $"{Guid.NewGuid()}{System.IO.Path.GetExtension(filePath)}";
                        var destPath = System.IO.Path.Combine(frontOfficeImages, fileName);
                        System.IO.File.Copy(filePath, destPath, true);

                        var newImage = new VehiculeImage
                        {
                            ImagePath = $"/images/vehicules/{fileName}",
                            VehiculeID = _selectedVehicule.VehiculeID,
                            IsPrimary = false
                        };
                        _context.Set<VehiculeImage>().Add(newImage);
                    }

                    _context.SaveChanges();
                    LoadVehiculeImages(_selectedVehicule.VehiculeID);
                    MessageBox.Show($"{openFileDialog.FileNames.Length} image(s) ajoutée(s)!", "Succès");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur");
                }
            }
        }

        private void btnSupprimerVehiculeImage_Click(object sender, RoutedEventArgs e)
        {
            var selectedImage = lstVehiculeImages.SelectedItem as VehiculeImage;
            if (selectedImage == null)
            {
                MessageBox.Show("Veuillez sélectionner une image à supprimer.", "Attention");
                return;
            }

            if (MessageBox.Show("Supprimer cette image?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    var imageToRemove = _context.Set<VehiculeImage>().Find(selectedImage.ImageID);
                    if (imageToRemove != null)
                    {
                        _context.Set<VehiculeImage>().Remove(imageToRemove);
                        _context.SaveChanges();
                        LoadVehiculeImages(_selectedVehicule!.VehiculeID);
                        MessageBox.Show("Image supprimée!", "Succès");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur");
                }
            }
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET 5 : CLIENTS
        // ======================================================================

        private void LoadClients()
        {
            ClientsGrid.ItemsSource = _context.Clients.AsNoTracking().ToList();
        }
        private void ClientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedClient = ClientsGrid.SelectedItem as Client;
            if (_selectedClient != null)
            {
                txtPrenomClient.Text = _selectedClient.Prenom;
                txtNomClient.Text = _selectedClient.Nom;
                txtEmailClient.Text = _selectedClient.Email;
                txtTelephoneClient.Text = _selectedClient.Telephone;
                txtAdresseClient.Text = _selectedClient.Adresse;
            }
        }
        private void btnAjouterClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newClient = new Client
                {
                    Prenom = txtPrenomClient.Text,
                    Nom = txtNomClient.Text,
                    Email = txtEmailClient.Text,
                    Telephone = txtTelephoneClient.Text,
                    Adresse = txtAdresseClient.Text,
                    MotDePasseHash = Guid.NewGuid().ToString()
                };
                _context.Clients.Add(newClient);
                _context.SaveChanges();
                LoadClients();
                btnEffacerClient_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
        }
        private void btnMettreAJourClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null) return;
            var clientToUpdate = _context.Clients.Find(_selectedClient.ClientID);
            if (clientToUpdate != null)
            {
                clientToUpdate.Prenom = txtPrenomClient.Text;
                clientToUpdate.Nom = txtNomClient.Text;
                clientToUpdate.Email = txtEmailClient.Text;
                clientToUpdate.Telephone = txtTelephoneClient.Text;
                clientToUpdate.Adresse = txtAdresseClient.Text;
                _context.SaveChanges();
                LoadClients();
                btnEffacerClient_Click(sender, e);
            }
        }
        private void btnSupprimerClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null) return;
            var clientToDelete = _context.Clients.Find(_selectedClient.ClientID);
            if (clientToDelete != null)
            {
                _context.Clients.Remove(clientToDelete);
                _context.SaveChanges();
                LoadClients();
                btnEffacerClient_Click(sender, e);
            }
        }
        private void btnEffacerClient_Click(object sender, RoutedEventArgs e)
        {
            txtPrenomClient.Text = ""; txtNomClient.Text = ""; txtEmailClient.Text = "";
            txtTelephoneClient.Text = ""; txtAdresseClient.Text = "";
            _selectedClient = null; ClientsGrid.SelectedItem = null;
        }

        private void btnExportClientsCSV_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Fichier CSV (*.csv)|*.csv";
            saveFileDialog.FileName = $"Export_Clients_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var clients = _context.Clients.AsNoTracking().ToList();
                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(clients);
                    }
                    MessageBox.Show($"Exportation réussie !\nFichier sauvegardé : {saveFileDialog.FileName}", "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'exportation : {ex.Message}", "Erreur d'exportation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || EXPORT EXCEL - CLIENTS
        // ======================================================================
        private void btnExportClientsExcel_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
            saveFileDialog.FileName = $"Export_Clients_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var clients = _context.Clients.AsNoTracking().ToList();
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Clients");
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Prénom";
                        worksheet.Cell(1, 3).Value = "Nom";
                        worksheet.Cell(1, 4).Value = "Email";
                        worksheet.Cell(1, 5).Value = "Téléphone";
                        worksheet.Cell(1, 6).Value = "Adresse";
                        
                        // Style headers
                        var headerRange = worksheet.Range(1, 1, 1, 6);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0B1F3F");
                        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                        
                        // Data
                        int row = 2;
                        foreach (var client in clients)
                        {
                            worksheet.Cell(row, 1).Value = client.ClientID;
                            worksheet.Cell(row, 2).Value = client.Prenom;
                            worksheet.Cell(row, 3).Value = client.Nom;
                            worksheet.Cell(row, 4).Value = client.Email;
                            worksheet.Cell(row, 5).Value = client.Telephone;
                            worksheet.Cell(row, 6).Value = client.Adresse;
                            row++;
                        }
                        
                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);
                    }
                    MessageBox.Show($"Exportation réussie !\nFichier sauvegardé : {saveFileDialog.FileName}", "Export Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'exportation : {ex.Message}", "Erreur d'exportation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || IMPORT CSV - CLIENTS
        // ======================================================================
        private void btnImportClientsCSV_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier CSV (*.csv)|*.csv";
            openFileDialog.Title = "Sélectionner un fichier CSV à importer";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    int imported = 0;
                    int skipped = 0;
                    var existingEmails = _context.Clients.Select(c => c.Email.ToLower()).ToHashSet();

                    using (var reader = new StreamReader(openFileDialog.FileName))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<dynamic>().ToList();
                        foreach (var record in records)
                        {
                            var dict = (IDictionary<string, object>)record;
                            string email = dict.ContainsKey("Email") ? dict["Email"]?.ToString() ?? "" : "";
                            
                            if (string.IsNullOrWhiteSpace(email) || existingEmails.Contains(email.ToLower()))
                            {
                                skipped++;
                                continue;
                            }

                            var newClient = new Client
                            {
                                Prenom = dict.ContainsKey("Prenom") ? dict["Prenom"]?.ToString() ?? "" : "",
                                Nom = dict.ContainsKey("Nom") ? dict["Nom"]?.ToString() ?? "" : "",
                                Email = email,
                                Telephone = dict.ContainsKey("Telephone") ? dict["Telephone"]?.ToString() : null,
                                Adresse = dict.ContainsKey("Adresse") ? dict["Adresse"]?.ToString() : null,
                                MotDePasseHash = Guid.NewGuid().ToString()
                            };

                            if (!string.IsNullOrWhiteSpace(newClient.Email) && !string.IsNullOrWhiteSpace(newClient.Nom))
                            {
                                _context.Clients.Add(newClient);
                                existingEmails.Add(email.ToLower());
                                imported++;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                    }
                    _context.SaveChanges();
                    LoadClients();
                    MessageBox.Show($"Importation terminée !\n{imported} clients importés\n{skipped} ignorés (duplicates ou invalides)", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'importation : {ex.Message}", "Erreur d'importation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || IMPORT EXCEL - CLIENTS
        // ======================================================================
        private void btnImportClientsExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
            openFileDialog.Title = "Sélectionner un fichier Excel à importer";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    int imported = 0;
                    int skipped = 0;
                    var existingEmails = _context.Clients.Select(c => c.Email.ToLower()).ToHashSet();

                    using (var workbook = new ClosedXML.Excel.XLWorkbook(openFileDialog.FileName))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // Skip header row
                        
                        if (rows != null)
                        {
                            foreach (var row in rows)
                            {
                                string email = row.Cell(4).GetString(); // Column D = Email
                                
                                if (string.IsNullOrWhiteSpace(email) || existingEmails.Contains(email.ToLower()))
                                {
                                    skipped++;
                                    continue;
                                }

                                var newClient = new Client
                                {
                                    Prenom = row.Cell(2).GetString(),  // Column B
                                    Nom = row.Cell(3).GetString(),     // Column C
                                    Email = email,
                                    Telephone = row.Cell(5).GetString(), // Column E
                                    Adresse = row.Cell(6).GetString(),   // Column F
                                    MotDePasseHash = Guid.NewGuid().ToString()
                                };

                                if (!string.IsNullOrWhiteSpace(newClient.Email) && !string.IsNullOrWhiteSpace(newClient.Nom))
                                {
                                    _context.Clients.Add(newClient);
                                    existingEmails.Add(email.ToLower());
                                    imported++;
                                }
                                else
                                {
                                    skipped++;
                                }
                            }
                        }
                    }
                    _context.SaveChanges();
                    LoadClients();
                    MessageBox.Show($"Importation terminée !\n{imported} clients importés\n{skipped} ignorés (duplicates ou invalides)", "Import Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'importation : {ex.Message}", "Erreur d'importation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || EXPORT EXCEL - VEHICULES
        // ======================================================================
        private void btnExportVehiculesExcel_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
            saveFileDialog.FileName = $"Export_Vehicules_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var vehicules = _context.Vehicules.Include(v => v.TypeVehicule).AsNoTracking().ToList();
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Vehicules");
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Immatriculation";
                        worksheet.Cell(1, 3).Value = "Marque";
                        worksheet.Cell(1, 4).Value = "Modèle";
                        worksheet.Cell(1, 5).Value = "Année";
                        worksheet.Cell(1, 6).Value = "Type";
                        worksheet.Cell(1, 7).Value = "Disponible";
                        worksheet.Cell(1, 8).Value = "Prix/Jour";
                        
                        // Style headers
                        var headerRange = worksheet.Range(1, 1, 1, 8);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0B1F3F");
                        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                        
                        // Data
                        int row = 2;
                        foreach (var v in vehicules)
                        {
                            worksheet.Cell(row, 1).Value = v.VehiculeID;
                            worksheet.Cell(row, 2).Value = v.Immatriculation;
                            worksheet.Cell(row, 3).Value = v.Marque;
                            worksheet.Cell(row, 4).Value = v.Modele;
                            worksheet.Cell(row, 5).Value = v.Annee;
                            worksheet.Cell(row, 6).Value = v.TypeVehicule?.Nom ?? "";
                            worksheet.Cell(row, 7).Value = v.QuantiteDisponible > 0 ? $"Oui ({v.QuantiteDisponible})" : "Non";
                            worksheet.Cell(row, 8).Value = v.PrixJournee;
                            row++;
                        }
                        
                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);
                    }
                    MessageBox.Show($"Exportation réussie !\nFichier sauvegardé : {saveFileDialog.FileName}", "Export Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'exportation : {ex.Message}", "Erreur d'exportation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || IMPORT CSV - VEHICULES
        // ======================================================================
        private void btnImportVehiculesCSV_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier CSV (*.csv)|*.csv";
            openFileDialog.Title = "Sélectionner un fichier CSV à importer";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    int imported = 0;
                    int skipped = 0;
                    var existingImmat = _context.Vehicules.Select(v => v.Immatriculation.ToUpper()).ToHashSet();
                    var types = _context.TypesVehicules.ToList();

                    using (var reader = new StreamReader(openFileDialog.FileName))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<dynamic>().ToList();
                        foreach (var record in records)
                        {
                            var dict = (IDictionary<string, object>)record;
                            string immat = dict.ContainsKey("Immatriculation") ? dict["Immatriculation"]?.ToString()?.ToUpper() ?? "" : "";
                            
                            if (string.IsNullOrWhiteSpace(immat) || existingImmat.Contains(immat))
                            {
                                skipped++;
                                continue;
                            }

                            int typeId = types.FirstOrDefault()?.TypeVehiculeID ?? 1;
                            if (dict.ContainsKey("TypeVehiculeID") && int.TryParse(dict["TypeVehiculeID"]?.ToString(), out int parsedTypeId))
                            {
                                if (types.Any(t => t.TypeVehiculeID == parsedTypeId))
                                    typeId = parsedTypeId;
                            }

                            var newVehicule = new Vehicule
                            {
                                Immatriculation = immat,
                                Marque = dict.ContainsKey("Marque") ? dict["Marque"]?.ToString() ?? "" : "",
                                Modele = dict.ContainsKey("Modele") ? dict["Modele"]?.ToString() ?? "" : "",
                                Annee = dict.ContainsKey("Annee") && int.TryParse(dict["Annee"]?.ToString(), out int annee) ? annee : DateTime.Now.Year,
                                QuantiteTotal = 1,
                                QuantiteDisponible = 1,
                                TypeVehiculeID = typeId
                            };

                            if (!string.IsNullOrWhiteSpace(newVehicule.Marque) && !string.IsNullOrWhiteSpace(newVehicule.Modele))
                            {
                                _context.Vehicules.Add(newVehicule);
                                existingImmat.Add(immat);
                                imported++;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                    }
                    _context.SaveChanges();
                    LoadVehicules();
                    LoadDashboard();
                    MessageBox.Show($"Importation terminée !\n{imported} véhicules importés\n{skipped} ignorés (duplicates ou invalides)", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'importation : {ex.Message}", "Erreur d'importation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || IMPORT EXCEL - VEHICULES
        // ======================================================================
        private void btnImportVehiculesExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
            openFileDialog.Title = "Sélectionner un fichier Excel à importer";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    int imported = 0;
                    int skipped = 0;
                    var existingImmat = _context.Vehicules.Select(v => v.Immatriculation.ToUpper()).ToHashSet();
                    var types = _context.TypesVehicules.ToList();

                    using (var workbook = new ClosedXML.Excel.XLWorkbook(openFileDialog.FileName))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // Skip header
                        
                        if (rows != null)
                        {
                            foreach (var row in rows)
                            {
                                string immat = row.Cell(2).GetString().ToUpper(); // Column B
                                
                                if (string.IsNullOrWhiteSpace(immat) || existingImmat.Contains(immat))
                                {
                                    skipped++;
                                    continue;
                                }

                                int typeId = types.FirstOrDefault()?.TypeVehiculeID ?? 1;

                                var newVehicule = new Vehicule
                                {
                                    Immatriculation = immat,
                                    Marque = row.Cell(3).GetString(),  // Column C
                                    Modele = row.Cell(4).GetString(),  // Column D
                                    Annee = int.TryParse(row.Cell(5).GetString(), out int annee) ? annee : DateTime.Now.Year,
                                    QuantiteTotal = 1,
                                    QuantiteDisponible = 1,
                                    TypeVehiculeID = typeId
                                };

                                if (!string.IsNullOrWhiteSpace(newVehicule.Marque) && !string.IsNullOrWhiteSpace(newVehicule.Modele))
                                {
                                    _context.Vehicules.Add(newVehicule);
                                    existingImmat.Add(immat);
                                    imported++;
                                }
                                else
                                {
                                    skipped++;
                                }
                            }
                        }
                    }
                    _context.SaveChanges();
                    LoadVehicules();
                    LoadDashboard();
                    MessageBox.Show($"Importation terminée !\n{imported} véhicules importés\n{skipped} ignorés (duplicates ou invalides)", "Import Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'importation : {ex.Message}", "Erreur d'importation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || EXPORT EXCEL - LOCATIONS
        // ======================================================================
        private void btnExportLocationsExcel_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
            saveFileDialog.FileName = $"Export_Locations_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var locations = _context.Locations
                        .Include(l => l.Client)
                        .Include(l => l.Vehicule)
                        .AsNoTracking()
                        .ToList();
                        
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Locations");
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Client";
                        worksheet.Cell(1, 3).Value = "Véhicule";
                        worksheet.Cell(1, 4).Value = "Date Début";
                        worksheet.Cell(1, 5).Value = "Date Fin";
                        worksheet.Cell(1, 6).Value = "Montant Total";
                        worksheet.Cell(1, 7).Value = "Statut";
                        worksheet.Cell(1, 8).Value = "Date Retour Réelle";
                        
                        // Style headers
                        var headerRange = worksheet.Range(1, 1, 1, 8);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0B1F3F");
                        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                        
                        // Data
                        int row = 2;
                        foreach (var loc in locations)
                        {
                            worksheet.Cell(row, 1).Value = loc.LocationID;
                            worksheet.Cell(row, 2).Value = loc.Client?.NomComplet ?? "";
                            worksheet.Cell(row, 3).Value = $"{loc.Vehicule?.Marque} {loc.Vehicule?.Modele}";
                            worksheet.Cell(row, 4).Value = loc.DateDebut.ToString("dd/MM/yyyy");
                            worksheet.Cell(row, 5).Value = loc.DateFin.ToString("dd/MM/yyyy");
                            worksheet.Cell(row, 6).Value = loc.MontantTotal;
                            worksheet.Cell(row, 7).Value = loc.Statut.ToString();
                            worksheet.Cell(row, 8).Value = loc.DateRetourReelle?.ToString("dd/MM/yyyy") ?? "";
                            row++;
                        }
                        
                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);
                    }
                    MessageBox.Show($"Exportation réussie !\nFichier sauvegardé : {saveFileDialog.FileName}", "Export Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'exportation : {ex.Message}", "Erreur d'exportation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET 6 : EMPLOYÉS
        // ======================================================================

        private void LoadEmployes()
        {
            EmployesGrid.ItemsSource = _context.Employes.AsNoTracking().ToList();
        }
        private void EmployesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedEmploye = EmployesGrid.SelectedItem as Employe;
            if (_selectedEmploye != null)
            {
                txtPrenomEmploye.Text = _selectedEmploye.Prenom;
                txtNomEmploye.Text = _selectedEmploye.Nom;
                txtEmailEmploye.Text = _selectedEmploye.Email;
                txtRoleEmploye.Text = _selectedEmploye.Role;
                txtMotDePasseEmploye.Text = "";
            }
        }
        private void btnAjouterEmploye_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newEmploye = new Employe
                {
                    Prenom = txtPrenomEmploye.Text,
                    Nom = txtNomEmploye.Text,
                    Email = txtEmailEmploye.Text,
                    Role = txtRoleEmploye.Text,
                    MotDePasseHash = txtMotDePasseEmploye.Text
                };
                _context.Employes.Add(newEmploye);
                _context.SaveChanges();
                LoadEmployes();
                btnEffacerEmploye_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
        }
        private void btnMettreAJourEmploye_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmploye == null) return;
            var employeToUpdate = _context.Employes.Find(_selectedEmploye.EmployeID);
            if (employeToUpdate != null)
            {
                employeToUpdate.Prenom = txtPrenomEmploye.Text;
                employeToUpdate.Nom = txtNomEmploye.Text;
                employeToUpdate.Email = txtEmailEmploye.Text;
                employeToUpdate.Role = txtRoleEmploye.Text;
                if (!string.IsNullOrWhiteSpace(txtMotDePasseEmploye.Text))
                {
                    employeToUpdate.MotDePasseHash = txtMotDePasseEmploye.Text;
                }
                _context.SaveChanges();
                LoadEmployes();
                btnEffacerEmploye_Click(sender, e);
            }
        }
        private void btnSupprimerEmploye_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmploye == null) return;
            var employeToDelete = _context.Employes.Find(_selectedEmploye.EmployeID);
            if (employeToDelete != null)
            {
                _context.Employes.Remove(employeToDelete);
                _context.SaveChanges();
                LoadEmployes();
                btnEffacerEmploye_Click(sender, e);
            }
        }
        private void btnEffacerEmploye_Click(object sender, RoutedEventArgs e)
        {
            txtPrenomEmploye.Text = ""; txtNomEmploye.Text = ""; txtEmailEmploye.Text = "";
            txtRoleEmploye.Text = ""; txtMotDePasseEmploye.Text = "";
            _selectedEmploye = null; EmployesGrid.SelectedItem = null;
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET 7 : LOCATIONS
        // ======================================================================
        private void LoadStatutComboBox()
        {
            cmbLocationStatut.ItemsSource = Enum.GetValues(typeof(LocationStatut));
        }
        private void LoadLocationsTab()
        {
            LocationsGrid.ItemsSource = _context.Locations
                                                .Include(loc => loc.Client)
                                                .Include(loc => loc.Vehicule)
                                                .AsNoTracking()
                                                .OrderByDescending(loc => loc.DateDebut)
                                                .ToList();
        }
        private void LocationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLocation = LocationsGrid.SelectedItem as Location;
            if (_selectedLocation != null)
            {
                txtLocationClient.Text = _selectedLocation.Client.NomComplet;
                txtLocationVehicule.Text = $"{_selectedLocation.Vehicule.Marque} {_selectedLocation.Vehicule.Modele}";
                txtLocationMontant.Text = _selectedLocation.MontantTotal.ToString("C");
                cmbLocationStatut.SelectedItem = _selectedLocation.Statut;
                dpDateRetourReelle.SelectedDate = _selectedLocation.DateRetourReelle;
            }
            else
            {
                txtLocationClient.Text = ""; txtLocationVehicule.Text = ""; txtLocationMontant.Text = "";
                cmbLocationStatut.SelectedItem = null; dpDateRetourReelle.SelectedDate = null;
            }
        }
        private void btnMettreAJourLocation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLocation == null) { MessageBox.Show("Veuillez sélectionner une location."); return; }

            var locationToUpdate = _context.Locations.Find(_selectedLocation.LocationID);
            if (locationToUpdate != null)
            {
                LocationStatut newStatut = (LocationStatut)cmbLocationStatut.SelectedItem;
                locationToUpdate.Statut = newStatut;
                locationToUpdate.DateRetourReelle = dpDateRetourReelle.SelectedDate;

                if (newStatut == LocationStatut.Annulee || newStatut == LocationStatut.Terminee)
                {
                    var vehicule = _context.Vehicules.Find(locationToUpdate.VehiculeID);
                    if (vehicule != null) { vehicule.QuantiteDisponible = vehicule.QuantiteTotal; }
                }

                _context.SaveChanges();
                LoadLocationsTab();
                LoadVehicules();
                LoadDashboard(); // Mettre à jour les graphiques
            }
        }

        private void btnGenererPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLocation == null)
            {
                MessageBox.Show("Veuillez sélectionner une location pour générer un PDF.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var locationPourPDF = _context.Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .ThenInclude(v => v.TypeVehicule)
                .FirstOrDefault(l => l.LocationID == _selectedLocation.LocationID);

            if (locationPourPDF == null)
            {
                MessageBox.Show("Impossible de trouver les détails de la location.", "Erreur");
                return;
            }

            byte[] qrCodeImageBytes;
            try
            {
                string qrContent = $"ReservationID:{locationPourPDF.LocationID};Client:{locationPourPDF.Client.NomComplet};Vehicule:{locationPourPDF.Vehicule.Immatriculation}";
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                qrCodeImageBytes = qrCode.GetGraphic(20);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération du QR Code : {ex.Message}", "Erreur QR Code");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Fichier PDF (*.pdf)|*.pdf";
            saveFileDialog.FileName = $"Bon_Reservation_{locationPourPDF.LocationID}.pdf";

            if (saveFileDialog.ShowDialog() == true)
            {
                var filePath = saveFileDialog.FileName;
                try
                {
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);

                            page.Header()
                                .Text("BON DE RÉSERVATION")
                                .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                            page.Content()
                                .Column(col =>
                                {
                                    col.Spacing(20);

                                    col.Item().Row(row =>
                                    {
                                        row.RelativeItem(1).Column(colClient =>
                                        {
                                            colClient.Item().Text("Informations Client").Bold();
                                            colClient.Item().Text($"Client: {locationPourPDF.Client.NomComplet}");
                                            colClient.Item().Text($"Email: {locationPourPDF.Client.Email}");
                                            colClient.Item().Text($"Téléphone: {locationPourPDF.Client.Telephone}");
                                        });
                                        row.RelativeItem(1).AlignRight().Width(100, Unit.Point).Image(qrCodeImageBytes);
                                    });

                                    col.Item().PaddingTop(1, Unit.Centimetre).Text("Informations Véhicule").Bold();
                                    col.Item().Text($"Modèle: {locationPourPDF.Vehicule.Marque} {locationPourPDF.Vehicule.Modele}");
                                    col.Item().Text($"Immatriculation: {locationPourPDF.Vehicule.Immatriculation}");
                                    col.Item().Text($"Type: {locationPourPDF.Vehicule.TypeVehicule.Nom}");

                                    col.Item().PaddingTop(1, Unit.Centimetre).Text("Détails de la réservation").Bold();
                                    col.Item().Text($"Du: {locationPourPDF.DateDebut:dd/MM/yyyy}");
                                    col.Item().Text($"Au: {locationPourPDF.DateFin:dd/MM/yyyy}");

                                    col.Item().AlignRight().Text($"{locationPourPDF.MontantTotal:C}").Bold().FontSize(16);
                                });

                            page.Footer()
                                .AlignCenter()
                                .Text(x =>
                                {
                                    x.Span("Page ");
                                    x.CurrentPageNumber();
                                });
                        });
                    }).GeneratePdf(filePath);

                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la génération du PDF : {ex.Message}", "Erreur PDF", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ======================================================================
        // || LOGIQUE DE L'ONGLET 8 : PAIEMENTS
        // ======================================================================

        private void LoadLocationsForPaiementTab()
        {
            LocationsPaiementGrid.ItemsSource = _context.Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .AsNoTracking()
                .OrderByDescending(l => l.DateDebut)
                .ToList();
        }
        private void LocationsPaiementGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLocationForPaiement = LocationsPaiementGrid.SelectedItem as Location;
            if (_selectedLocationForPaiement != null)
            {
                FormPaiement.IsEnabled = true;
                LoadPaiementsForLocation(_selectedLocationForPaiement.LocationID);
            }
            else
            {
                FormPaiement.IsEnabled = false;
                PaiementsGrid.ItemsSource = null;
            }
        }
        private void LoadPaiementsForLocation(int locationId)
        {
            PaiementsGrid.ItemsSource = _context.Paiements
                .Where(p => p.LocationID == locationId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToList();
        }
        private void btnAjouterPaiement_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLocationForPaiement == null) { MessageBox.Show("Veuillez sélectionner une location."); return; }
            try
            {
                var newPaiement = new Paiement
                {
                    LocationID = _selectedLocationForPaiement.LocationID,
                    Montant = decimal.Parse(txtPaiementMontant.Text),
                    MethodePaiement = ((ComboBoxItem)cmbMethodePaiement.SelectedItem)?.Content.ToString(),
                    DatePaiement = DateTime.Now
                };
                _context.Paiements.Add(newPaiement);
                _context.SaveChanges();
                LoadPaiementsForLocation(_selectedLocationForPaiement.LocationID);

                // Send email notification (non-blocking)
                var location = _context.Locations
                    .Include(l => l.Client)
                    .Include(l => l.Vehicule)
                    .FirstOrDefault(l => l.LocationID == _selectedLocationForPaiement.LocationID);

                if (location?.Client != null && !string.IsNullOrEmpty(location.Client.Email))
                {
                    var vehicleInfo = $"{location.Vehicule?.Marque} {location.Vehicule?.Modele}";
                    Task.Run(() => _emailService.SendPaymentConfirmationAsync(
                        location.Client.Email,
                        $"{location.Client.Prenom} {location.Client.Nom}",
                        vehicleInfo,
                        newPaiement.Montant,
                        newPaiement.MethodePaiement ?? "Non spécifié",
                        newPaiement.DatePaiement
                    ));
                    Log.Information("Email de confirmation paiement envoyé à {Email}", location.Client.Email);
                }

                txtPaiementMontant.Text = "";
                cmbMethodePaiement.SelectedIndex = 0;
                MessageBox.Show("Paiement ajouté avec succès! Email de confirmation envoyé.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
        }


        // ======================================================================
        // || LOGIQUE DE L'ONGLET 9 : ENTRETIENS
        // ======================================================================

        private void LoadEntretiens()
        {
            EntretiensGrid.ItemsSource = _context.Entretiens.Include(e => e.Vehicule).AsNoTracking().ToList();
        }
        private void EntretiensGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedEntretien = EntretiensGrid.SelectedItem as Entretien;
            if (_selectedEntretien != null)
            {
                cmbEntretienVehicule.SelectedValue = _selectedEntretien.VehiculeID;
                dpEntretienDate.SelectedDate = _selectedEntretien.DateEntretien;
                txtEntretienCout.Text = _selectedEntretien.Cout.ToString();
                txtEntretienDescription.Text = _selectedEntretien.Description;
            }
        }
        private void btnAjouterEntretien_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbEntretienVehicule.SelectedValue == null) { MessageBox.Show("Veuillez sélectionner un véhicule."); return; }
                if (dpEntretienDate.SelectedDate == null) { MessageBox.Show("Veuillez sélectionner une date."); return; }

                var newEntretien = new Entretien
                {
                    VehiculeID = (int)cmbEntretienVehicule.SelectedValue,
                    DateEntretien = dpEntretienDate.SelectedDate.Value,
                    Cout = decimal.Parse(txtEntretienCout.Text),
                    Description = txtEntretienDescription.Text
                };
                _context.Entretiens.Add(newEntretien);
                _context.SaveChanges();
                LoadEntretiens();
                LoadDashboard(); // Mettre à jour les alertes
                btnEffacerEntretien_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
        }
        private void btnMettreAJourEntretien_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntretien == null) return;
            var entretienToUpdate = _context.Entretiens.Find(_selectedEntretien.EntretienID);
            if (entretienToUpdate != null)
            {
                try
                {
                    if (cmbEntretienVehicule.SelectedValue == null) { MessageBox.Show("Veuillez sélectionner un véhicule."); return; }
                    if (dpEntretienDate.SelectedDate == null) { MessageBox.Show("Veuillez sélectionner une date."); return; }

                    entretienToUpdate.VehiculeID = (int)cmbEntretienVehicule.SelectedValue;
                    entretienToUpdate.DateEntretien = dpEntretienDate.SelectedDate.Value;
                    entretienToUpdate.Cout = decimal.Parse(txtEntretienCout.Text);
                    entretienToUpdate.Description = txtEntretienDescription.Text;

                    _context.SaveChanges();
                    LoadEntretiens();
                    LoadDashboard(); // Mettre à jour les alertes
                    btnEffacerEntretien_Click(sender, e);
                }
                catch (Exception ex) { MessageBox.Show($"Erreur : {ex.Message}", "Erreur"); }
            }
        }
        private void btnSupprimerEntretien_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntretien == null) return;
            var entretienToDelete = _context.Entretiens.Find(_selectedEntretien.EntretienID);
            if (entretienToDelete != null)
            {
                _context.Entretiens.Remove(entretienToDelete);
                _context.SaveChanges();
                LoadEntretiens();
                LoadDashboard(); // Mettre à jour les alertes
                btnEffacerEntretien_Click(sender, e);
            }
        }
        private void btnEffacerEntretien_Click(object sender, RoutedEventArgs e)
        {
            cmbEntretienVehicule.SelectedValue = null;
            dpEntretienDate.SelectedDate = null;
            txtEntretienCout.Text = "";
            txtEntretienDescription.Text = "";
            _selectedEntretien = null; EntretiensGrid.SelectedItem = null;
        }
    }

    // ======================================================================
    // || NOUVEAU : CLASSE POUR LES ALERTES (ViewModel)
    // ======================================================================
    public class AlerteEntretienViewModel
    {
        public int VehiculeID { get; set; }
        public string Immatriculation { get; set; }
        public string Modele { get; set; }
        public DateTime? DateDernierEntretien { get; set; }

        // Propriété calculée pour la grille
        public string JoursDepuisEntretien
        {
            get
            {
                if (DateDernierEntretien == null)
                {
                    return "Jamais";
                }
                return (DateTime.Now - DateDernierEntretien.Value).Days.ToString();
            }
        }
    }
}