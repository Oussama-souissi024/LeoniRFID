# Documentation Pédagogique du Projet PFE : LEONI RFID

Ce document constitue la référence centrale du projet **LEONI RFID**. Il a été pensé et rédigé pour permettre à l'étudiant de comprendre en profondeur chaque brique technologique, d'expliquer l'architecture, et de défendre ses choix de conception lors de sa soutenance de Projet de Fin d'Études (PFE).

---

## 1. Contexte et Objectif du Projet

L'application **LeoniRFID** est une solution mobile cross-platform (développée avec .NET MAUI) dédiée au suivi, à la traçabilité et à la gestion des équipements industriels et des moules sur les chaînes de production de l'entreprise LEONI. 

Grâce à l'utilisation de terminaux Android industriels (ex: PDA Zebra) équipés de lecteurs RFID UHF, l'application permet aux techniciens et administrateurs d'identifier instantanément une machine, de tracer ses déplacements (installation, retrait) et de consigner les opérations de maintenance.

---

## 2. Architecture Technique Globale

Le projet repose sur le framework **.NET MAUI** (Multi-platform App UI) qui permet de cibler principalement les terminaux Android. 
Pour garantir un code propre, évolutif et facile à tester, l'application suit rigoureusement le modèle architectural **MVVM** (Model-View-ViewModel).

### Le Pattern MVVM
*   **Les Modèles (Models)** : Ce sont les objets qui reflètent la structure de la base de données (ex: `Profile`, `Machine`, `ScanEvent`).
*   **Les Vues (Views)** : Représentées par les fichiers `.xaml` et `.xaml.cs` orientés "design pur". Elles gèrent les couleurs, les dispositions (Layouts comme Grid ou StackLayout).
*   **Les ViewModels** : Le cœur intelligent. Ils interceptent les clics des boutons (via le DataBinding), contactent la base de données via les `Services`, et mettent à jour les champs de la vue dynamiquement via les `ObservableProperty`.

*Explication de soutenance : "J'ai utilisé MVVM car cette séparation permet de valider le comportement algorithmique (ViewModel) sans être bloqué par l'interface graphique (View)."*

---

## 3. L'Injection de Dépendances (Dependency Injection)

Tout le projet repose sur l'injection de dépendances, configurée dans `MauiProgram.cs`. Ce système permet de donner aux ViewModels l'accès aux Services sans avoir à instancier manuellement ces derniers (`new SupabaseService()`).

*   **Services en `Singleton`** : L'application crée une *unique* instance (ex: `SupabaseService`) partagée dans toute l'application. Idéal pour ne pas multiplier les connexions réseau ou perdre le cache d'authentification.
*   **Pages et ViewModels en `Transient`** : Une nouvelle instance est générée à chaque visite. Cela évite, par exemple, qu'un formulaire de saisie ne contienne encore les données du technicien précédent.

---

## 4. Base de Données et Identité (Supabase)

L'application agit comme client graphique (Front-end), le Backend est assuré par **Supabase** (PostgreSQL-as-a-Service). L'accès aux données se fait par des appels d'API REST via la librairie `postgrest-csharp`.

### Sécurité, RLS et Flux "Zero-Knowledge"
Le système de sécurité de notre application a été pensé spécifiquement pour le cycle de vie industriel :
1.  **RBAC (Role-Based Access Control)** : Les profils (`Profile.cs`) définissent un `Role` ("Admin" ou "Technician"). La navigation de l'interface graphique (`AppShell.xaml.cs`) bloque les pages sensibles (comme la gestion d'utilisateurs ou les suppressions) si l'utilisateur n'est pas Admin.
2.  **Row Level Security (RLS)** : Une protection directement au niveau de la base de données qui empêche l'Altération des données si l'utilisateur n'en possède pas le droit explicite.
3.  **Onboarding (Zero-Knowledge)** : 
    *   L'Admin crée un compte technicien (seul l'email et le nom sont requis). 
    *   Lors de sa toute première connexion au terminal, le technicien définit lui-même son mot de passe.
    *   *Technique* : Comme le technicien n'est techniquement pas encore authentifié, le client utilise de manière éphémère un jeton `ServiceRoleKey` (Bypass RLS) encodé pour écrire ce premier mot de passe. Le système ignore donc le vrai mot de passe du technicien => Sécurité maximisée.

---

## 5. Intégration Matérielle RFID (Zebra DataWedge)

Plutôt que d'intégrer des SDK complexes alourdissant l'application pour diverses marques de PDA, le projet utilise les mécaniques natives du système Android.

*   Fichier clé : `Platforms/Android/DataWedgeIntentReceiver.cs`
*   L'appareil Zebra décode la puce RFID via son outil de fond `DataWedge`.
*   DataWedge est configuré pour diffuser les données captées de manière système via un **Broadcast Intent** Android.
*   Notre application C# (.NET MAUI) hérite d'un "BroadcastReceiver" qui "intercepte" ces messages lorsque le projet tourne sous Android, nous donnant directement accès au code EPC numérisé sans interface supplémentaire.

---

## 6. Fonctions Clés et Services Auxiliaires

*   **Le Service RFID (`IRfidService`, `RfidService`)** : Il abstrait le matériel. Sous un PC de développement, il retourne de fausses données (mocks) en un clic. Sur un PDA Android, il s'attache à `DataWedge`. Ceci respecte le principe SOLID "Dependency Inversion".
*   **Le Reporting et Export Excel (`ExcelService`)** : Permet aux administrateurs de filtrer le contenu depuis le `ReportViewModel` et de générer instantanément (via la librairie locale ClosedXML) un fichier `.xlsx` partageable directement depuis l'application via les API natives Android de partage (`Share.Default.RequestAsync`).

---

## Mots clés pour la soutenance (Cheat-sheet)

*   **Cross-platform** : .NET MAUI
*   **MVVM** : Découplage strict Interface/Logique métier
*   **Observable** : Le `Binding` met à jour la liste XAML automatiquement sans rafraîchissement manuel
*   **Injection de dépendance** : Facilité de test, de maintenance et d’évolution du code
*   **BaaS (Backend as a Service)** : Supabase
*   **OR-Mapping (ORM)** : Fait le pont entre les classes C# (`Models`) et les tables (`PostgreSQL`)
*   **Intents Android** : Outil natif de communication inter-processus (IPC) pour le scanner
*   **Singleton** : Une instance unique stockée en mémoire (ex: Base de données connecteur)

---
*Ce document sert de synthèse globale. Des commentaires balisés avec "🎓 Pédagogie PFE" se trouvent directement dans le code source (répartis dans la quasi-totalité des fichiers .xaml et .cs) pour expliquer le rôle exact de chaque ligne lors de l'étude du code source.*
