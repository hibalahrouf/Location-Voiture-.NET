# 🚗 Location de Voitures — Car Rental Management System

A full-stack car rental management solution built with **.NET**, composed of two interconnected applications: a desktop **Back-office** for administrators and a web **Front-office** for customers.

---

## 📋 Overview

This project provides an end-to-end car rental workflow — from fleet and client management on the admin side, to vehicle browsing and online booking on the customer side — backed by a shared SQL Server database.

| | |
|---|---|
| **Back-office** | WPF desktop app for employees |
| **Front-office** | ASP.NET Core MVC web app for customers |
| **Database** | SQL Server, 10 relational tables |
| **Architecture** | Three-tier (shared Core + two presentation layers) |

---

## ✨ Features

### 🖥️ Back-office (WPF)
- Full CRUD for vehicles, clients, locations, payments, maintenance, and rates
- Real-time dashboard with statistics (fleet size, clients, active rentals, revenue)
- CSV / Excel import and export for client data
- QR code generation for reservations
- Automated confirmation emails (via MailKit)
- Light / dark theme
- Activity logging with Serilog

### 🌐 Front-office (ASP.NET Core MVC)
- Customer account creation with mandatory email verification (ASP.NET Identity)
- Vehicle catalog with filtering and search
- Online booking with automatic total calculation
- Email confirmation flow with a unique confirmation token
- Downloadable PDF reservation voucher (with QR code)
- Customer dashboard with rental history
- Contact form

---

## 🛠️ Tech Stack

- **Language:** C#
- **Desktop:** WPF / XAML
- **Web:** ASP.NET Core MVC, Razor Views, Bootstrap 5
- **Data access:** Entity Framework Core (Code First + Migrations)
- **Auth:** ASP.NET Core Identity
- **Database:** SQL Server
- **Logging:** Serilog
- **Email:** MailKit
- **Other libraries:** QRCoder, ClosedXML

---

## 🏗️ Architecture

```
LocationVoiture/
├── LocationVoiture.Core/        # Shared models + EF Core DbContext + Migrations
├── LocationVoiture.BackOffice/  # WPF desktop app (admin)
└── LocationVoiture.FrontOffice/ # ASP.NET Core MVC web app (customers)
```

The two presentation layers share a single `LocationVoiture.Core` project containing the data models and `ApplicationDbContext`, ensuring both apps stay in sync with the same database schema.

**Database (10 tables):** Clients, Employes, TypesVehicules, Vehicules, VehiculeImages, Tarifs, Locations, Paiements, Entretiens, ContactMessages.



---

## 🚀 Getting Started

### Prerequisites
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) or later
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 (recommended) or VS Code

### Installation

1. **Clone the repo**
   ```bash
   git clone https://github.com/hibalahrouf/Location-Voiture-.NET.git
   cd Location-Voiture-.NET
   ```

2. **Configure the database connection**
   Update the connection string in `appsettings.json` (Front-office) and `App.config` (Back-office) to point to your SQL Server instance.

3. **Apply migrations**
   ```bash
   cd LocationVoiture.Core
   dotnet ef database update
   ```

4. **Run the Front-office (web)**
   ```bash
   cd LocationVoiture.FrontOffice
   dotnet run
   ```

5. **Run the Back-office (desktop)**
   Open `LocationVoiture.BackOffice` in Visual Studio and run it, or:
   ```bash
   cd LocationVoiture.BackOffice
   dotnet run
   ```

> **Note:** In development mode, outgoing emails are saved locally as `.eml` files rather than sent via SMTP. Configure a real SMTP server in `appsettings.json` for production use.

---

## 🗺️ Roadmap

Planned improvements identified during development:
- [ ] Online payment integration (Stripe / PayPal)
- [ ] Real-time notifications (SignalR)
- [ ] Multi-agency support with geolocation
- [ ] REST API for mobile app integration
- [ ] Customizable PDF reports
- [ ] Automated unit / integration tests

---

## 📄 License

This project was developed for academic purposes.
