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

namespace LocationVoiture.BackOffice
{
    // Ajouter 'INotifyPropertyChanged' pour les graphiques
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        // ======================================================================
        // || DÉCLARATIONS DES VARIABLES
        // ======================================================================

        private readonly ApplicationDbContext _context;

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

        public MainWindow(ApplicationDbContext context)
        {
            InitializeComponent();
            _context = context;

            QuestPDF.Settings.License = LicenseType.Community;

            VehiculesAlerteEntretien = new List<AlerteEntretienViewModel>(); // Initialiser la liste

            this.DataContext = this;
        }

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
            /* CHARTS LOGIC REMOVED FOR STABILITY */

            // ==========================================================
            // || NOUVEAU : ALERTE ENTRETIEN
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
                chkDisponible.IsChecked = _selectedVehicule.Disponible;
                cmbTypeVehicule.SelectedValue = _selectedVehicule.TypeVehiculeID;
            }
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
                    Disponible = chkDisponible.IsChecked ?? false,
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
                    vehiculeToUpdate.Disponible = chkDisponible.IsChecked ?? false;
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
            chkDisponible.IsChecked = false;
            cmbTypeVehicule.SelectedValue = null; _selectedVehicule = null;
            VehiculesGrid.SelectedItem = null;
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
                    if (vehicule != null) { vehicule.Disponible = true; }
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
                .Where(l => l.Statut == LocationStatut.Confirmee || l.Statut == LocationStatut.Terminee)
                .Include(l => l.Client)
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
                txtPaiementMontant.Text = "";
                cmbMethodePaiement.SelectedIndex = 0;
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