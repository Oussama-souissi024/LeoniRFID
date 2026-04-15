# 📡 LEONI RFID — Suivi et Traçabilité Industrielle en Production

![.NET MAUI](https://img.shields.io/badge/.NET_MAUI_9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Supabase](https://img.shields.io/badge/Supabase-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)
![Android](https://img.shields.io/badge/Android-3DDC84?style=for-the-badge&logo=android&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Zebra](https://img.shields.io/badge/Zebra_MC3300x-000000?style=for-the-badge&logo=zebra-technologies&logoColor=white)
![Production](https://img.shields.io/badge/🏭_EN_PRODUCTION-FF6B00?style=for-the-badge)

> **Projet de Fin d'Études (PFE) — Ingénierie Logiciel / Développement Mobile Industriel**
>
> *Application conçue et développée pour le compte de **LEONI Wiring Systems**.*

---

## ⚠️ Application en Production Réelle

> **IMPORTANT** : Cette application **n'est pas un prototype académique**. Elle est **directement déployée et utilisée en production** sur les chaînes de fabrication de l'usine LEONI.
>
> Le projet répond à un **besoin réel et urgent** de l'usine : remplacer les processus manuels de suivi des équipements (fiches papier, fichiers Excel éparpillés, saisies manuelles sujettes aux erreurs) par une solution digitale fiable exploitant la **technologie RFID UHF**.
>
> Les techniciens et agents de maintenance sur le terrain utilisent cette application **quotidiennement** sur des terminaux durcis Zebra MC3300x pour scanner les machines, signaler les pannes, gérer les interventions de maintenance et générer des rapports d'activité.
>
> Chaque fonctionnalité développée a été validée en conditions réelles de production avant d'être intégrée, et le code a été conçu avec les exigences de fiabilité et de robustesse qu'impose un environnement industriel opérationnel.

---

## 📖 Contexte du Projet

Ce projet s'inscrit dans le cadre d'un **Projet de Fin d'Études (PFE)**, mais sa portée dépasse largement le cadre académique. Son objectif principal est de **digitaliser et fiabiliser** la gestion, l'installation et la maintenance des équipements industriels (machines, moules) sur les chaînes de production de LEONI.

### Problématique Industrielle

L'usine LEONI fait face à des défis concrets de gestion de son parc machines :

- **Traçabilité inexistante** : aucun historique fiable des interventions de maintenance, des installations, ou des déplacements d'équipements.
- **Saisies manuelles** fastidieuses et sources d'erreurs humaines (fiches papier, tableurs Excel non synchronisés).
- **Temps de réponse maintenance** non mesurable — impossible de connaître la durée réelle des interventions.
- **Visibilité limitée** pour les responsables de production sur l'état réel du parc machines en temps réel.
- **Perte de productivité** liée à l'absence d'outil centralisé de suivi.

### Solution Déployée

L'application remplace ces processus manuels par une **identification instantanée via RFID UHF**. Déployée sur des **terminaux durcis Zebra MC3300x** équipés d'antennes RFID, elle permet aux opérateurs de :

- Scanner un équipement en approchant le lecteur RFID (précision élevée, sans contact visuel nécessaire).
- Mettre à jour le statut de la machine en temps réel (En Marche → Panne → Maintenance → Réparé).
- Mesurer avec précision le temps d'intervention grâce à un **chronomètre persistant** (sauvegardé en base de données).
- Assurer une **traçabilité complète** de chaque action effectuée (qui, quoi, quand).

---

## 🎯 Fonctionnalités Complètes

### 🔐 Authentification & Sécurité
- **Connexion sécurisée** via Supabase Auth (session persistante, tokens JWT).
- **Système Zero-Knowledge Password** : l'administrateur crée un compte avec un mot de passe temporaire aléatoire qu'il ne connaît pas. L'utilisateur définit lui-même son mot de passe à la première connexion.
- **Verrouillage post-configuration** : une fois le mot de passe défini (`must_change_password = false`), le mécanisme de première connexion est verrouillé.
- **Désactivation de compte** : un administrateur peut désactiver un compte sans le supprimer.

### 👥 Gestion des Rôles (RBAC — Role-Based Access Control)

Trois rôles distincts avec des permissions différenciées, contrôlées **côté client** (visibilité des menus) **et côté serveur** (Row Level Security PostgreSQL) :

| Rôle | Permissions |
|------|-------------|
| **👷 Technicien** | Scanner les machines, signaler les pannes (Running → Broken), consulter le dashboard et les rapports. |
| **🔧 Agent Maintenance** | Tout ce qu'un Technicien fait + démarrer/terminer les interventions de maintenance (Broken → InMaintenance → Running) avec chronomètre. |
| **👑 Administrateur** | Accès complet : gestion des utilisateurs, import/export Excel, page d'administration, toutes les fonctions Maintenance. |

### 📡 Intégration Matérielle Native (Zebra DataWedge)
- Exploitation directe du module **DataWedge** des PDA Zebra via les **Intents Android** (`BroadcastReceiver`).
- **Aucun SDK lourd** requis — communication légère via le système d'intents natif Android.
- Compilation conditionnelle `#if ANDROID` pour le code spécifique à la plateforme.
- Mode **Mock/Simulation** intégré pour le développement et les tests sans matériel physique.

### 📊 Scanner RFID Intelligent
- Analyse automatisée des codes **EPC (Electronic Product Code)**.
- Recherche instantanée de la machine correspondante dans Supabase.
- **Enregistrement de nouvelles machines** à la volée si le tag est inconnu.
- Workflow de statuts complet avec traçabilité de chaque changement :
  ```
  Running (✅ En Marche)
      ↓ [Technicien : Signaler Panne]
  Broken (🔴 En Panne)
      ↓ [Agent Maintenance : Commencer]
  InMaintenance (🔧 Maintenance en cours) ← Chronomètre activé
      ↓ [Agent Maintenance : Terminer]
  Running (✅ En Marche) ← Durée enregistrée
  ```

### 🔧 Module Maintenance Dédié
- Page exclusive pour les agents de maintenance et administrateurs.
- **Vue consolidée** : liste des machines en panne + machines en cours de maintenance.
- **Chronomètre persistant** : le timer est calculé depuis `started_at` en base de données. Si l'application est fermée et rouverte, le timer reprend exactement là où il en était.
- **Enregistrement automatique** de chaque session : heure de début, heure de fin, durée en minutes, technicien responsable.

### 📈 Dashboard Analytique Temps Réel
- Statistiques globales : total machines, répartition par statut (Running, Broken, InMaintenance, Paused, Removed).
- Répartition par département (LTN1, LTN2, LTN3).
- Journal des **10 derniers événements RFID** avec horodatage.
- Informations utilisateur connecté (nom, rôle, initiales).
- Indicateur de synchronisation cloud.

### 📋 Reporting & Export Excel
- **Rapport Machines** : filtrage multicritère (département, statut, plage de dates).
- **Rapport Maintenance** : historique complet des interventions avec durées calculées.
- **Statistiques agrégées** : nombre total d'interventions, durée moyenne, durée cumulée.
- **Export Excel natif** via `ClosedXML` : fichier professionnel à 2 onglets (Machines + Événements RFID) avec coloration automatique des statuts.
- **Partage système** : le fichier exporté est partageable via l'API Share native d'Android.

### 📥 Import Excel (Administration)
- Import massif de machines depuis un fichier Excel fourni par LEONI.
- Normalisation automatique des statuts et des départements.
- Génération de fichier Excel de test pour la validation.

### 👤 Gestion des Utilisateurs (Administration)
- Création de nouveaux comptes via l'API Admin de Supabase.
- Modification des rôles (Technician ↔ Maintenance ↔ Admin).
- Activation/Désactivation de comptes.

---

## 🛠️ Stack Technologique & Architecture

Le code du projet a été conçu avec une approche **Clean Architecture** stricte, documenté de manière pédagogique pour la soutenance.

### Technologies

| Composant | Technologie | Version | Rôle |
|-----------|-------------|---------|------|
| **Framework Mobile** | .NET MAUI | 9.0.30 | UI multiplateforme (ciblé Android) |
| **Backend (BaaS)** | Supabase | — | Auth, PostgreSQL, RLS, API REST |
| **Langage** | C# | .NET 9 | Logique métier, ViewModels, Services |
| **UI** | XAML | — | Interfaces déclaratives avec DataBinding |
| **ORM** | Postgrest C# | via supabase-csharp 0.16.2 | Mapping objet-relationnel |
| **MVVM Toolkit** | CommunityToolkit.Mvvm | 8.3.2 | ObservableProperty, RelayCommand |
| **UI Toolkit** | CommunityToolkit.Maui | 9.1.1 | Animations, Converters, Behaviours |
| **Excel** | ClosedXML | 0.104.2 | Import/Export de fichiers Excel |
| **JSON** | Newtonsoft.Json | 13.0.3 | Sérialisation (requis par Supabase) |
| **Matériel** | Zebra DataWedge | — | Intégration RFID via Intents Android |

### Design Pattern — MVVM (Model-View-ViewModel)

Séparation rigoureuse entre l'UI XAML et la logique métier C# :

```
┌──────────────────────────────────────────────────────────┐
│  View (XAML)                                             │
│  "Dumb Views" — Aucune logique métier.                   │
│  Uniquement du DataBinding bidirectionnel.                │
├──────────────────────────────────────────────────────────┤
│  ViewModel (C#)                                          │
│  Le "cerveau" de chaque écran.                           │
│  ObservableProperties, RelayCommands, validation.        │
├──────────────────────────────────────────────────────────┤
│  Model (C#)                                              │
│  Représentation des données + mapping ORM Postgrest.     │
│  Propriétés calculées [JsonIgnore] pour l'affichage.     │
├──────────────────────────────────────────────────────────┤
│  Service (C#)                                            │
│  Accès réseau (SupabaseService), abstraction matérielle  │
│  (IRfidService), utilitaires (ExcelService).             │
└──────────────────────────────────────────────────────────┘
```

### Injection de Dépendances (DI)

Utilisation du container natif Microsoft (enregistré dans `MauiProgram.cs`) :

- **Singleton** : `SupabaseService`, `IRfidService`, `ExcelService` — une seule instance partagée (connexion DB, session utilisateur).
- **Transient** : Tous les ViewModels et Pages — une nouvelle instance vierge à chaque navigation (pas de données résiduelles).

### Sécurité (Double Couche)

1. **Sécurité Front-End (RBAC)** : Visibilité dynamique des menus dans `AppShell.xaml.cs` via `UpdateAdminVisibility()`. Les onglets Admin et Maintenance sont masqués/affichés selon le rôle.
2. **Sécurité Back-End (RLS)** : Politiques Row Level Security dans PostgreSQL — même si un menu est visible, les données sont protégées côté serveur.

---

## 📂 Structure du Répertoire

```text
LeoniRFID/
├── LeoniRFID.sln                      # Solution Visual Studio
├── README.md                          # Ce fichier
├── READY_TO_TEST.txt                  # Guide de test rapide
├── run-app.bat                        # Script de lancement automatisé
├── run-emulator.ps1                   # Script de lancement émulateur
│
├── LeoniRFID/                         # Projet .NET MAUI principal
│   ├── MauiProgram.cs                 # Point d'entrée DI (Singleton/Transient)
│   ├── App.xaml(.cs)                  # Configuration globale de l'app
│   ├── AppShell.xaml(.cs)             # Navigation Shell + RBAC dynamique
│   │
│   ├── Models/                        # Représentation des données (ORM Postgrest)
│   │   ├── Machine.cs                 # Machine industrielle (tag, statut, dept)
│   │   ├── Profile.cs                 # Profil utilisateur (rôle, mot de passe)
│   │   ├── ScanEvent.cs               # Événement de scan RFID (traçabilité)
│   │   ├── MaintenanceSession.cs      # Session de maintenance (chrono, durée)
│   │   └── Department.cs              # Département (LTN1, LTN2, LTN3)
│   │
│   ├── ViewModels/                    # Logique métier de chaque écran
│   │   ├── BaseViewModel.cs           # Classe de base (IsBusy, ErrorMessage)
│   │   ├── LoginViewModel.cs          # Auth + Zero-Knowledge Password
│   │   ├── DashboardViewModel.cs      # Statistiques temps réel
│   │   ├── ScanViewModel.cs           # Cœur métier : scan RFID + workflow
│   │   ├── MaintenanceViewModel.cs    # Module maintenance dédié + chrono
│   │   ├── MachineListViewModel.cs    # Liste des machines par département
│   │   ├── MachineDetailViewModel.cs  # Détail d'une machine + historique
│   │   ├── ReportViewModel.cs         # Rapports filtrés + export Excel
│   │   ├── AdminViewModel.cs          # Import Excel + administration
│   │   └── UserManagementViewModel.cs # Gestion des comptes utilisateurs
│   │
│   ├── Views/                         # Interfaces XAML (9 pages)
│   │   ├── LoginPage.xaml(.cs)
│   │   ├── DashboardPage.xaml(.cs)
│   │   ├── ScanPage.xaml(.cs)
│   │   ├── MaintenancePage.xaml(.cs)
│   │   ├── MachineListPage.xaml(.cs)
│   │   ├── MachineDetailPage.xaml(.cs)
│   │   ├── ReportPage.xaml(.cs)
│   │   ├── AdminPage.xaml(.cs)
│   │   └── UserManagementPage.xaml(.cs)
│   │
│   ├── Services/                      # Couche d'accès aux données et matériel
│   │   ├── SupabaseService.cs         # CRUD Supabase (Auth, Machines, Events, Maintenance)
│   │   ├── IRfidService.cs            # Interface d'abstraction RFID
│   │   ├── RfidService.cs             # Implémentation (DataWedge + Mock)
│   │   └── ExcelService.cs            # Import/Export Excel (ClosedXML)
│   │
│   ├── Helpers/                       # Utilitaires transversaux
│   │   ├── Constants.cs               # URLs Supabase, rôles, statuts, intents Zebra
│   │   └── Converters.cs              # ValueConverters XAML (Bool→Visibility, etc.)
│   │
│   ├── Platforms/Android/             # Code natif Android uniquement
│   │   └── DataWedgeIntentReceiver    # BroadcastReceiver pour les scans Zebra
│   │
│   └── Resources/                     # Assets visuels
│       ├── Styles/                    # Colors.xaml, Styles.xaml (Design System LEONI)
│       ├── Fonts/                     # OpenSans, Roboto
│       └── Images/                    # Icônes et images
│
└── Oussama modification/             # Documentation pédagogique détaillée
    ├── GuideImplementation.md         # Guide d'implémentation pas-à-pas
    ├── IntegrationZebraRfid.md        # Documentation intégration matérielle Zebra
    ├── PassagePhase1Phase2.md         # Évolution Phase 1 → Phase 2
    ├── Philosophie-de-mot-de-passe.md # Explication du système Zero-Knowledge
    ├── Mecanisme.md                   # Mécanismes internes de l'application
    ├── architecture.md                # Documentation architecturale
    ├── implementation.md              # Détails d'implémentation
    ├── amélioration.md                # Pistes d'amélioration futures
    ├── ProjectDocumentation.md        # Documentation projet globale
    └── ChangementAppliquerParOussama.md # Journal des modifications
```

---

## 🏭 Workflow de Production (Usage Réel en Usine)

Voici le workflow quotidien tel qu'il est utilisé par les équipes de LEONI :

### 1. Début de Service
```
Technicien/Agent → Ouvre l'app sur le Zebra → Connexion
                                                ↓
                                          Dashboard avec
                                        état du parc machines
```

### 2. Scan d'une Machine (Technicien)
```
Approche le Zebra MC3300x → DataWedge capture l'EPC → Intent Android
    ↓
BroadcastReceiver → RfidService.TagScanned → ScanViewModel.ProcessEpcAsync()
    ↓
Recherche Supabase → Machine trouvée/non trouvée
    ↓
Affichage fiche machine + boutons contextuels selon le rôle
```

### 3. Signalement de Panne (Technicien)
```
Machine "Running" → Technicien clique "Signaler Panne"
    ↓
Statut → "Broken" (enregistré en DB + ScanEvent de traçabilité)
    ↓
La machine apparaît dans le module Maintenance
```

### 4. Intervention (Agent Maintenance)
```
Machine "Broken" → Agent clique "Commencer Maintenance"
    ↓
Statut → "InMaintenance" + Chronomètre démarré (started_at enregistré en DB)
    ↓
Agent effectue la réparation physique
    ↓
Agent clique "Terminer Maintenance"
    ↓
Statut → "Running" + Durée calculée + Session archivée
```

### 5. Reporting (Fin de Service / Hebdomadaire)
```
Admin/Technicien → Page Rapports → Filtres (département, dates, statut)
    ↓
Rapport Machines + Rapport Maintenance avec statistiques
    ↓
Export Excel → Fichier partagé via WhatsApp/Email/Drive
```

---

## 🚀 Installation & Prérequis

### Matériel / Logiciels Nécessaires

| Composant | Requis | Détails |
|-----------|--------|---------|
| **IDE** | [Visual Studio 2022](https://visualstudio.microsoft.com/) | Version 17.8+ avec le workflow **.NET MAUI** activé |
| **SDK** | .NET 9 SDK | Installé automatiquement avec Visual Studio |
| **Terminal Production** | PDA Zebra MC3300x | Avec module RFID UHF + DataWedge configuré |
| **Terminal Test** | Smartphone Android (API 21+) | L'app propose un mode Mock pour les tests sans matériel RFID |
| **Émulateur** | Android Emulator | Pour le développement sans appareil physique |
| **Cloud** | Projet Supabase | PostgreSQL + Auth + RLS (script SQL fourni) |

### Configuration Locale

1. **Cloner** le repository :
   ```bash
   git clone <repository-url>
   ```

2. **Ouvrir** la solution `LeoniRFID.sln` dans Visual Studio.

3. **Vérifier les clés Supabase** dans `Helpers/Constants.cs` :
   - `SupabaseUrl` : URL du projet Supabase.
   - `SupabaseAnonKey` : Clé publique (client-side).
   - `SupabaseServiceRoleKey` : Clé de service (administrative, utilisée pour le Zero-Knowledge Password et la gestion des utilisateurs).

4. **Compiler et lancer** :
   - Mode **Debug — Android Local Device** pour un appareil physique connecté.
   - Mode **Debug — Android Emulator** pour le développement.

5. **Configuration DataWedge (Zebra uniquement)** :
   - Créer un profil DataWedge pour l'app `com.leoni.rfid.production`.
   - Activer le plugin RFID et configurer l'output en Intent Broadcast.
   - Voir la documentation détaillée dans `Oussama modification/IntegrationZebraRfid.md`.

---

## 📊 Base de Données (Schéma Supabase / PostgreSQL)

```sql
-- Tables principales
profiles             -- Utilisateurs (id UUID, full_name, role, is_active, must_change_password)
machines             -- Parc machines (id, tag_id, name, department, status, dates)
scan_events          -- Journal de traçabilité (tag_id, machine_id, user_id, event_type, timestamp)
maintenance_sessions -- Sessions de maintenance (machine_id, technician_id, started_at, ended_at, duration_minutes)
departments          -- Départements (LTN1, LTN2, LTN3)
```

### Politiques RLS (Row Level Security)
- Les techniciens peuvent lire toutes les machines mais ne peuvent modifier que via les endpoints autorisés.
- La clé `ServiceRoleKey` est utilisée pour les opérations administratives (création de comptes, bypass RLS pour le check de première connexion).

---

## 🎓 Note Académique & Pédagogique

Dans le cadre de la formation et de la validation des acquis, **l'intégralité du code source a été minutieusement commentée** grâce à des balises d'en-têtes et des commentaires in-line marqués `🎓 Pédagogie PFE`.

Ces notes expliquent en situation réelle tous les concepts avancés manipulés par le projet :

- **Cycle de vie** des composants MAUI et Android.
- **MVVM** : séparation Model-View-ViewModel avec CommunityToolkit.
- **Injection de Dépendances** : Singleton vs Transient, container natif Microsoft.
- **Data Binding** : liaison bidirectionnelle XAML ↔ ViewModel.
- **Converters XAML** : transformation de données pour l'affichage.
- **BroadcastReceivers natifs** : réception des intents Android (DataWedge Zebra).
- **Compilation conditionnelle** : `#if ANDROID` pour le code spécifique plateforme.
- **Appels REST directs** : bypass RLS via HttpClient avec ServiceRoleKey.
- **ORM Postgrest** : mapping C# ↔ PostgreSQL avec attributs `[Table]`, `[Column]`, `[PrimaryKey]`.

Le dossier `Oussama modification/` contient **10 documents pédagogiques** détaillés couvrant l'architecture, l'implémentation, l'intégration matérielle, et la philosophie de sécurité du projet.

---

## 📈 Métriques du Projet

| Métrique | Valeur |
|----------|--------|
| **Pages/Écrans** | 9 (Login, Dashboard, Scan, Maintenance, MachineList, MachineDetail, Report, Admin, UserManagement) |
| **ViewModels** | 10 (dont BaseViewModel partagé) |
| **Models** | 5 (Machine, Profile, ScanEvent, MaintenanceSession, Department) |
| **Services** | 4 (SupabaseService, RfidService, IRfidService, ExcelService) |
| **Tables Supabase** | 5 |
| **Rôles RBAC** | 3 (Technician, Maintenance, Admin) |
| **Statuts Machine** | 5 (Running, Broken, InMaintenance, Paused, Removed) |
| **Documents pédagogiques** | 10 |
| **Framework** | .NET MAUI 9.0 ciblé Android (arm64-v8a, armeabi-v7a) |

---

**🏭 Développé dans le cadre d'un Projet de Fin d'Études — Déployé en production chez LEONI Wiring Systems.**
*Encadré par : Oussama Souissi*
