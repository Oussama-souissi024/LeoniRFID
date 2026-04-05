# 📡 LEONI RFID - Suivi et Traçabilité Industrielle (PFE)

![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Supabase](https://img.shields.io/badge/Supabase-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)
![Android](https://img.shields.io/badge/Android-3DDC84?style=for-the-badge&logo=android&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)

> **Projet de Fin d'Études (PFE) - Ingénierie / Développement Logiciel**
> 
> *Application conçue et développée pour le compte de **LEONI Wiring Systems**.*

---

## 📖 Contexte du Projet

Ce projet s'inscrit dans le cadre d'un **Projet de Fin d'Études (PFE)**. Son objectif principal est de digitaliser et fiabiliser la gestion, l'installation et la maintenance des équipements industriels (machines, moules) sur les chaînes de production de LEONI.

L'application remplace les saisies manuelles fastidieuses par une identification instantanée utilisant la **technologie RFID UHF**. Déployée sur des terminaux durcis (ex: PDA industriels Zebra) équipés d'antennes RFID, elle permet aux techniciens de scanner des équipements avec un taux de précision élevé, de mettre à jour leur statut en temps réel, et d'assurer une traçabilité parfaite.

## 🎯 Fonctionnalités Principales

- **Authentification Sécurisée** : Connexion basée sur Supabase avec création de "Zero-Knowledge Password" pour les nouveaux utilisateurs.
- **Gestion des Rôles (RBAC)** : Accès différencié entre *Techniciens* (scans, installation, maintenance) et *Administrateurs* (export, gestion des utilisateurs, import Excel).
- **Intégration Matérielle Native** : Exploitation directe du module `DataWedge` des PDA Zebra via les *Intents Android* (aucun SDK lourd ou instable requis).
- **Scanner RFID Intelligent** : Analyse automatisée des codes EPC, mise à jour instantanée du statut des machines (Installé, Maintenance, Retiré).
- **Dashboard Analytique** : Vue globale des statistiques de production et journaling des événements récents.
- **Imports / Exports Excel** : Génération de rapports natifs via l'intégration de `ClosedXML` et partage système des fichiers (Share).

## 🛠️ Stack Technologique & Architecture

Le code du projet a été conçu avec une approche **Clean Architecture** très stricte et documenté de manière approfondie à vocation pédagogique.

- **Frontend / Mobile Framework** : `.NET MAUI` (axé déploiement Android).
- **Backend-as-a-Service (BaaS)** : `Supabase` (PostgreSQL, Auth, RLS).
- **Design Pattern** : `MVVM (Model-View-ViewModel)` - Séparation rigoureuse entre l'UI XAML et la logique métier C#.
- **Injection de Dépendances** : Utilisation du container natif Microsoft pour l'orchestration des cycles de vie (Singleton pour les Services, Transient pour les Pages).
- **UI/UX** : Moteur de rendu XAML avec utilisation intensive des `DataBinding`, `Converters`, et d'un `ResourceDictionary` centralisé (Design System LEONI).

## 📂 Structure du Répertoire

```text
LeoniRFID/
├── Models/        # Représentation des données et mapping ORM Postgrest.
├── ViewModels/    # Le "cerveau" dynamique de chaque écran (RelayCommands, ObservableProperties).
├── Views/         # L'interface utilisateur XAML ("dumb views" sans logique métier).
├── Services/      # Accès à la DB et abstraction matérielle (IRfidService).
├── Helpers/       # Convertisseurs XAML, Constantes de configuration.
├── Platforms/     # Implémentations natives (ex: DataWedgeIntentReceiver spécifique à Android).
└── Resources/     # Styles (Colors.xaml, Styles.xaml), Fontes, Images.
```

## 🚀 Installation & Prérequis

### Matériel / Logiciels nécessaires
- **IDE** : [Visual Studio 2022](https://visualstudio.microsoft.com/) (version 17.8+ avec le *workflow de développement .NET MAUI* activé).
- **Terminal Cible** : Smartphone Android (API 24+) ou PDA Industriel Zebra (pour l'utilisation RFID réelle). À défaut, un émulateur Android classique peut être utilisé (l'application propose un mécanisme de fausses saisies "Mock" pour le test).
- **Cloud** : Un projet Supabase opérationnel (pour recréer votre environnement, fiez-vous au script SQL fourni).

### Configuration Locale
1. Cloner le repository.
2. Ouvrir la solution `LeoniRFID.sln` dans Visual Studio.
3. Les clés API Supabase (publiques) sont actuellement situées dans `Helpers/Constants.cs`. Ces identifiants gèrent le point d'accès client.
4. Compiler et lancer l'application en mode **Debug - Android Local Device** ou Emulator.

## 🎓 Note Académique & Pédagogique

Dans le cadre de la formation et de la validation des acquis, **l'intégralité du code source a été minutieusement commentée** grâce à des balises d'en-têtes et des commentaires in-line marqués `🎓 Pédagogie PFE`. 

Ces notes expliquent en situation réelle tous les concepts avancés manipulés par le projet (Cycle de vie, MVVM, Injection de dépendances, Converters XAML, BroadcastReceivers natifs), servant ainsi d'outil d'explication de l'architecture pour la soutenance finale.

---

**Développé dans le cadre d'un Projet de Fin d'Études.**  
*Encadré par : Oussama Souissi*
