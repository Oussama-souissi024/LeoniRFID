# Le Mécanisme Interne de LEONI RFID (Moteur .NET MAUI)

Ce document explique chronologiquement et techniquement le fonctionnement de votre application `LeoniRFID`, en retraçant la chaîne de responsabilité de chaque fichier, du moment où l'utilisateur lance l'application jusqu'au moment où une page s'affiche.

---

## 1. Le Moteur de Démarrage (Bootstrapping)
Quand l'utilisateur lance l'application sur le scanner Android industriel, le code MAUI ne démarre pas par magie. Voici la suite exacte des opérations déclenchées par le système :

1. **`Platforms/Android/MainActivity.cs` & `MainApplication.cs`**
   * **Rôle** : Ce sont les points d'entrée "Natifs". Android utilise spécifiquement `MainActivity` comme première fenêtre d'exécution.
   * **Mécanisme** : `MainApplication` indique au système Android : *"Réveille le moteur .NET et demande-lui de créer le constructeur d'application en appelant `MauiProgram.CreateMauiApp()`"*.

2. **`MauiProgram.cs`**
   * **Rôle** : C'est le **Conteneur d'Injection de Dépendances (DI)**. C'est le chef de gare du projet.
   * **Mécanisme** : Il enregistre toutes les "pièces détachées" du projet en mémoire. Il utilise `AddSingleton` pour les Services (créés une seule fois, comme base de données) et `AddTransient` pour les ViewModels et les Pages (à détruire après chaque affichage). Il initialise aussi les polices d'écriture industrielles (Roboto).

3. **`App.xaml` et `App.xaml.cs`**
   * **Rôle** : L'initialiseur global des ressources UI.
   * **Mécanisme** : `App.xaml` fusionne de grands dictionnaires (`Styles.xaml` et `Colors.xaml`). Dès lors, tous les boutons et textes de l'application sauront quelle définition de couleur `LeoniOrange` utiliser. Ensuite, `App.xaml.cs` prend la main et dit : "La toute première fenêtre de navigation sera `AppShell`".

4. **`AppShell.xaml` et `AppShell.xaml.cs`**
   * **Rôle** : Le moteur de Routage et de Navigation.
   * **Mécanisme** : Il définit le menu latéral, l'arborescence des vues, et le système de route racine (`//login` vers `//dashboard`). Son backend (`.cs`) contient d'ailleurs la sécurité "Role-Based" pour effacer le bouton d'accès Admin si l'utilisateur n'est qu'un technicien.

---

## 2. Le Mécanisme MVVM (Couche Applicative)
Pour tout écran de l'application, l'architecture implique toujours un trio de fichiers (La Vue, le ViewModel, et le Modèle). Prenons la création d'une ligne dans le tableau du Dashboard :

1. **Le Modèle de Données : `Models/ScanEvent.cs`**
   * **Rôle** : Une simple classe C# décrivant purement les données.
   * **Mécanisme** : Elle possède des propriétés métier (ex: date du scan, ID de la machine) et utilise des annotations spéciales utilisées par Supabase (ORM) pour savoir correspondre avec les colonnes de la Table PostgreSQL.

2. **L'Intelligence : `ViewModels/DashboardViewModel.cs`**
   * **Rôle** : Orchestrer les Actions. Il n'a aucune idée de la couleur de l'écran ou de sa taille, mais c'est lui qui fait transiter la donnée.
   * **Mécanisme** : Il implémente `ObservableObject`. Quand on appelle sa fonction `RefreshDataAsync()`, il contacte la base de données. Il stocke les résultats dans une liste spéciale : `ObservableCollection<ScanEvent>`.

3. **L'Interface Graphique (UI) : `Views/DashboardPage.xaml`**
   * **Rôle** : Le design visuel.
   * **Mécanisme** : La vue XML écoute le ViewModel au travers du texte `{Binding}`. Elle utilise, par exemple, un composant `<CollectionView ItemsSource="{Binding RecentEvents}" />`. Lorsque `RecentEvents` change dans le ViewModel, le CollectionView génère dynamiquement des petits composants visuels (DataTemplates) pour dessiner chaque Item à l'écran.

4. **Le Liant : `Views/DashboardPage.xaml.cs` (Code-Behind)**
   * **Rôle** : Coller la page visuelle au "cerveau" (le ViewModel).
   * **Mécanisme** : En injectant le `DashboardViewModel` dans son constructeur, le code-behind exécute cette ligne critique : `BindingContext = viewModel`. C'est le "câblage" final du tuyau de données entre le code C# et le fichier XAML.

---

## 3. La Machinerie de Données (Couches "Services")
Les ViewModels ne connaissent pas la mécanique physique ou réseau ; ils délèguent toujours le "vrai" travail complexe à des `Classes Services`.

1. **`Services/SupabaseService.cs`**
   * **Rôle** : Agent de liaison sécurisé Client/Cloud.
   * **Mécanisme** : Il génère de manière asynchrone les requêtes API (GET, POST). Il gère le fait de basculer la `ServiceRoleKey` uniquement lorsque l'on veut forcer la création d'un utilisateur sans enfreindre la sécurité RLS.

2. **`Services/RfidService.cs` & `Platforms/Android/DataWedgeIntentReceiver.cs`**
   * **Rôle** : Piloter le terminal industriel Zebra.
   * **Mécanisme** : C'est le point d'innovation majeur. Plutôt qu'un vieux SDK lourd, LEONI_RFID fait appel aux écouteurs natifs Android (`BroadcastReceiver` de niveau bas dans `DataWedgeIntentReceiver`). Android gère le scan Laser/UHF matériel et "pousse" sa réponse dans ce BroadcastReceiver. Ce dernier transforme ce scan Brut en un "Événement Système" (C# Action) que le `ScanViewModel` écoute en temps réel en toile de fond. 

3. **`Services/ExcelService.cs`**
   * **Rôle** : Le moteur d'export.
   * **Mécanisme** : Il interagit avec les flux filaires IO internes à Android et les parse via ClosedXML pour livrer un document .xlsx valide aux API système "Share" utilisées par l'utilisateur.

---

## 4. Outils de Rendu Graphique Transversaux (Helpers)
Enfin, pour éviter un code surchargé dans l'UI (le "Spaghetti Code"), le mécanisme s'appuie sur :

* **`Helpers/Converters.cs`**
   * **Mécanisme XAML conditionnel** : L'interface visuelle n'accepte pas de clauses `if/else`. Les Converters agissent comme une "Moulinette". Ils reçoivent la donnée (ex: Statut = `Installed`) et la transforment immédiatement au moment de peindre l'écran (ex: Statut converti en `Color.Green`). Cela permet l'utilisation du paramètre `{Binding Status, Converter={StaticResource StatusToColor}}` en XAML.

* **`Helpers/Constants.cs`**
   * **Mécanisme de Globalisation** : Évite les conflits. Toutes les URLs, les rôles (Admin/Technicien) et clés de déchiffrement y sont conservés en `public const`. Le programme entier pointe donc vers cette unique source de vérité.
