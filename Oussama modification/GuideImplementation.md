# 🔨 Guide d'Implémentation Pas-à-Pas : Reconstruire LEONI RFID (Édition Détaillée)

> **Auteur / Encadrant** : Oussama Souissi  
> **Objectif** : Ce guide permet à l'étudiante de reconstruire le projet **à partir de zéro** (projet .NET MAUI vide) en suivant un ordre logique. Chaque étape explique **pourquoi** on crée le fichier, **son rôle profond** dans l'architecture, et **comment il interagit** avec le reste du système. C'est un véritable cours d'architecture logicielle.

---

## 🏗️ Prérequis et Philosophie de l'Architecture

Avant de commencer, s'assurer que l'environnement est prêt :
- **Visual Studio 2022** (v17.8+) avec le workload **.NET MAUI** installé.
- **.NET 9 SDK** installé.
- Un émulateur Android ou un appareil physique (Zebra) connecté.

💡 **La Règle d'Or de ce Guide** :
En architecture logicielle professionnelle (Clean Architecture / MVVM), on construit toujours un projet "de la fondation vers le toit", c'est-à-dire : **Data ➡️ Intelligence (Services) ➡️ Cerveaux (ViewModels) ➡️ Écrans (Views) ➡️ Câblage**.
Si l'on écrit les écrans en premier, l'application ne compilera pas car les cerveaux et les données n'existeront pas.

---

## PHASE 1 : Créer le Projet et Installer les Dépendances

### Étape 1.1 → Créer le projet .NET MAUI vide
Dans Visual Studio :
1. Fichier → Nouveau → Projet.
2. Chercher **".NET MAUI App"**.
3. Nommer le projet **`LeoniRFID`**.

> **📝 Explication détaillée** :
> .NET MAUI est l'évolution de Xamarin.Forms. Son grand avantage est d'offrir un projet unique (Single Project) pour cibler plusieurs plateformes (Android, iOS, Windows) sans avoir à gérer des dizaines de projets séparés comme autrefois. Quand on génère le projet, MSBuild (le compilateur) prépare déjà la route universelle.

### Étape 1.2 → Configurer le fichier projet (`.csproj`)
📄 **Fichier** : Pointez sur le fichier maître du projet (ex: `LeoniRFID.csproj`)

Ouvrez ce fichier et **remplacez tout son contenu par l'extrait ci-dessous**. Cette action accomplit deux choses essentielles : 
1. Elle supprime le "bruit" des plateformes inutiles (iOS, Mac, Windows) pour se focaliser sur Android.
2. Elle installe automatiquement tous nos paquets NuGet sans devoir utiliser l'interface graphique.

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<!-- 1. Cible : On garde uniquement Android -->
		<TargetFrameworks>net9.0-android</TargetFrameworks>
		<OutputType>Exe</OutputType>
		<!-- (Ici se trouve votre RootNamespace) -->
		<UseMaui>true</UseMaui>
		<SingleProject>true</SingleProject>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		
		<!-- 2. Accélération : Compilation source du XAML -->
		<MauiEnableXamlCBindingWithSourceCompilation>true</MauiEnableXamlCBindingWithSourceCompilation>

		<ApplicationTitle>LEONI RFID</ApplicationTitle>
		<ApplicationId>com.leoni.rfid.production</ApplicationId>
		<ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
		<ApplicationVersion>1</ApplicationVersion>

		<!-- 3. OS supportés : On supprime Mac/Windows/iOS, on garde l'API Android minimale -->
		<SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
	</PropertyGroup>

	<ItemGroup>
		<!-- Ressources graphiques (Laissées par défaut) -->
		<MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />
		<MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128" />
		<MauiImage Include="Resources\Images\*" />
		<MauiImage Update="Resources\Images\dotnet_bot.png" Resize="True" BaseSize="300,185" />
		<MauiFont Include="Resources\Fonts\*" />
		<MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
	</ItemGroup>

	<!-- 4. Dépendances : Installation automatique des outils PFE via NuGet -->
	<ItemGroup>
		<PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
		<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="9.0.9" />
		
		<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
		<PackageReference Include="ClosedXML" Version="0.104.2" />
		<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
		<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
		<PackageReference Include="CommunityToolkit.Maui" Version="9.1.1" />
		<PackageReference Include="supabase-csharp" Version="0.16.2" />
	</ItemGroup>

</Project>
```

> **📝 Explication détaillée (Le Nettoyage et les Fondations)** :
> Par défaut, MAUI prépare un énorme projet hybride. Le fait de forcer `<TargetFrameworks>net9.0-android</TargetFrameworks>` (et de supprimer manuellement les autres `Condition` Windows/Apple) allège énormément la mémoire et divise le temps de compilation par 10 !
> De plus, les balises `<PackageReference ... />` indiquent à Visual Studio de télécharger silencieusement nos fondations : 
> - `CommunityToolkit.Mvvm` : Le moteur `INotifyPropertyChanged` qui informe l'écran quand on modifie une variable.
> - `supabase-csharp` : Le pont sécurisé entre notre application et la base de données hébergée PostgreSQL.
> - `ClosedXML` : Un générateur de fichiers Excel pur, ne nécessitant pas qu'Excel soit installé sur le PDA industriel.

---

## PHASE 2 : Créer la Structure des Dossiers

Créer manuellement ces dossiers à la racine : `Models/`, `ViewModels/`, `Views/`, `Services/`, `Helpers/`.

> **📝 Explication détaillée (Le Découpage)** :
> Pourquoi ne mettons-nous pas tout dans le même dossier ? Car dans l'industrie, le code doit être "Testable" et "Maintenable". Si la logique de calcul de stock (ViewModel) est mélangée avec la couleur d'un bouton (View), si on change le design, on risque de casser les mathématiques. Ce découpage garantit la "Séparation des Préoccupations" (Separation of Concerns).

---

## PHASE 3 : Les Helpers (La Configuration Globale)

### Étape 3.1 → Créer `Helpers/Constants.cs`
📄 **Fichier** : `Helpers/Constants.cs`  

**Rôle** : Centraliser **toutes** les clés secrètes et valeurs récurrentes. 
```csharp
public const string SupabaseUrl = "https://VOTRE_URL.supabase.co";
```

> **📝 Explication détaillée** :
> Si demain l'URL du serveur de base de données change, grâce à ce fichier, on ne modifie qu'une seule ligne de code. Si on n'avait pas ce fichier, il faudrait chercher cette URL partout (dans la page Login, dans le Dashboard, dans l'Admin) avec un risque d'oubli critique. C'est le principe du **DRY (Don't Repeat Yourself)**.

### Étape 3.2 → Créer `Helpers/Converters.cs`
📄 **Fichier** : `Helpers/Converters.cs`  

**Rôle** : Les Convertisseurs sont des "moulinettes" visuelles. Le XAML est un langage de balisage bête : il ne sait pas faire de "IF Status == Installed THEN Color = Green". 

> **📝 Explication détaillée** :
> On crée donc une classe C# implémentant `IValueConverter`. Quand le XAML reçoit le texte "Installed", il le passe au Converter qui lui renverra "Colors.Green". Cela permet de garder nos ViewModels propres (nos cerveaux mathématiques n'ont pas à se soucier de donner des "Couleurs", ils fournissent juste des "États de mots").

---

## PHASE 4 : Les Modèles (La Couche Données)

### Étape 4.1 → Créer les Modèles : `Models/Machine.cs`, `Profile.cs`...
📄 **Fichier** : `Models/Machine.cs`  

**Rôle** : C'est le "Moule" ou le "Contrat" de la donnée.

```csharp
[Table("machines")]
public class Machine : BaseModel
{
    [Column("tag_id")]
    public string TagId { get; set; } = string.Empty;
}
```

> **📝 Explication détaillée (ORM et Attributs)** :
> L'informatique est divisée en deux mondes : le C# manipule des Objets (Classes), et PostgreSQL manipule des Lignes (Tables).
> La librairie Supabase agit comme un dictionnaire de traduction appelé **ORM (Object-Relational Mapping)**. 
> Les balises `[Table(...)]` et `[Column(...)]` guident cet ORM. Elles disent : *"Quand le serveur t'enverra la donnée de la colonne 'tag_id', tu la stockeras dans la propriété 'TagId' de la classe C#"*.

> **Propriétés calculées** :
> Dans ce même fichier, on crée souvent des propriétés ne possédant pas de balise `[Column]`, par exemple `public string StatusDisplay => "✅ " + Status;`. Ces propriétés ne sont pas sauvegardées en base de données, elles servent uniquement d'illusion pour formater la donnée visuellement dans l'écran XAML !

---

## PHASE 5 : Les Services (La Couche Intelligence et Matérielle)

### Étape 5.1 → Créer l'Interface `Services/IRfidService.cs`
> **📝 Explication détaillée (Le Polymorphisme)** :
> Pourquoi créer une "Interface" `IRfidService` ? C'est le contrat. Il dit : "Peu importe qui fera le travail, l'application a besoin d'une méthode `StartListening()`". 
> Demain, si l'usine abandonne les scanners Zebra pour des scanners Datalogic, l'étudiante n'aura pas à réécrire tous les ViewModels ! Elle modifiera seulement le connecteur physique, car le "Contrat" (l'interface) restera le même aux yeux de l'application.

### Étape 5.2 → Créer l'implémentation `Services/RfidService.cs`
> **📝 Explication détaillée** :
> Ici, on utilise la **compilation conditionnelle** (`#if ANDROID`). On indique au compilateur d'injecter ce code uniquement dans le fichier final APK d'Android. Cela évite que l'application ne plante (crash) si on exécute l'application sur un ordinateur Windows pour faire des tests (car Windows ne connaît pas l'Intent Android Zebra).

### Étape 5.3 → Créer `Services/SupabaseService.cs`
**Rôle** : Encapsuler tous les échanges Asynchrones avec le Web.

> **📝 Explication détaillée (L'Asynchronisme et la Sécurité)** :
> 1. Ce fichier contient beaucoup de méthodes avec les mots-clés `async` et `await`. Pourquoi ? Parce qu'un appel réseau (télécharger 4000 machines) prend du temps. Si on ne fait pas d'Asynchronisme, l'interface graphique va "geler" (freeze) pendant que la connexion internet cherche. Avec `await`, l'application continue de respirer et affiche un cercle de chargement.
> 2. C'est ici que l'étudiante devra mentionner la gestion de la sécurité "Zero-Knowledge Password". Le service s'authentifie normalement de façon anonyme, mais il bascule sur le `ServiceRoleKey` caché du client MAUI *uniquement* pour forcer l'inscription d'un mot de passe lors du premier "Onboarding" du technicien, sans que l'administrateur n'ait eu besoin de connaître ce mot de passe.

---

## PHASE 6 : La Couche Android Native & Matériel

### Étape 6.1 → Créer `Platforms/Android/DataWedgeIntentReceiver.cs`
**Rôle** : L'intercepteur natif du Laser et capteur UHF Zebra.

> **📝 Explication détaillée (Les Intents Android)** :
> Android fonctionne avec un système de messages internes appelés "Broadcast Intents" (comme une radio publique).
> Quand on appuie sur la gâchette du pistolet industriel Zebra, l'application d'usine de l'appareil (DataWedge) décode la puce RFID et hurle sur la radio Android : *"Hé ! J'ai un tag EPC-1234 !"*.
> Ce fichier C# (marqué de l'attribut `[BroadcastReceiver]`) est une antenne. Il est paramétré sciemment pour écouter cette fréquence spécifique. Lorsqu'il entend le message, il attrape la donnée et la repasse au monde .NET MAUI. C'est beaucoup plus robuste qu'un SDK lourd car c'est géré par le système d'exploitation natif.

---

## PHASE 7 : Les ViewModels (Le Cerveau de Chaque Page)

### Étape 7.1 → Créer `ViewModels/BaseViewModel.cs`
**Rôle** : Le modèle de base contenant la variable de chargement (`IsBusy`). 

> **📝 Explication détaillée (Le Coeur du MVVM)** :
> Chaque ViewModel hérite de `ObservableObject`. Sous le capot, Microsoft a créé un système appelé `INotifyPropertyChanged`. 
> Dans les vieux frameworks, on devait chercher les TextBox et faire `txtNom.Text = nom`. 
> Avec le MVVM, le ViewModel crie : *"Je préviens tout le monde que la variable 'IsBusy' vient de passer à 'true' !"*. L'interface graphique (qui était connectée en "Data-Binding") entend ce cri et va afficher instantanément la roue de chargement toute seule.

### Étape 7.2 → Créer les ViewModels (`LoginViewModel.cs`, `ScanViewModel.cs`...)
**Rôle** : Remplir l'écran de ses données, et recevoir les ordres.

> **📝 Explication détaillée (RelayCommand vs Click)** :
> Dans ces fichiers, au lieu de récupérer les "clics de souris" des vues, on crée des **Commands** (ex: `LogoutCommand`). 
> Pourquoi ? Parce qu'un ViewModel ne doit absoluement RIEN savoir de la vue. S'il n'y a plus de souris (mais une commande vocale, ou un clavier physique de PDA), la Commande reste exactement la même ! L'interface graphique va "lier" (binder) son action utilisateur directement à cette commande.

---

## PHASE 8 : Les Vues (L'Interface Graphique XAML)

### Étape 8.1 → Créer les fichiers `Views/*.xaml` et `*.xaml.cs`
**Rôle** : Dessiner l'application avec des balises de structures (Grilles, Layouts empilés).

> **📝 Explication détaillée (Le BindingContext)** :
> Dans tous les fichiers `.xaml.cs` (le code derrière la page visuelle), le code est quasiment vide, mis à part une ligne capitale : 
> `BindingContext = viewModel;`
> C'est l'essence même du projet. C'est cette ligne qui fait la connexion physique entre le Cerveau (ViewModel) et l'Écran (View). C'est le branchement électrique du système de Data Binding.

---

## PHASE 9 : Le Câblage Final (Configuration & Cycle de Vie)

### Étape 9.1 → Configurer `MauiProgram.cs`
**Rôle** : Initialiser le conteneur d'**Injection de Dépendances**.

> **📝 Explication détaillée (Singleton vs Transient)** :
> C'est ici que l'étudiante doit épater son jury en expliquant comment fonctionne la mémoire vive (RAM) de l'application :
> - **AddSingleton()** (ex: `SupabaseService`) : Le système va créer le composant 1 seule fois au démarrage de l'application. Cette même "session mémoire" tournera en permanence. C'est vital pour la base de données car on ne veut ouvrir qu'une seule "connexion réseau" pour toute l'application.
> - **AddTransient()** (ex: `ScanViewModel` ou `LoginPage`) : Le système va créer le composant puis le détruire chaque fois que l'on quitte la page ! C'est ce qui permet de "vider" les formulaires. Si on utilisait un Singleton pour un formulaire, le texte tapé par le technicien précédent serait encore là à la prochaine ouverture.

### Étape 9.2 → Configurer `AppShell.xaml` et `AppShell.xaml.cs`
**Rôle** : L'Épine dorsale de la navigation.

> **📝 Explication détaillée** :
> `AppShell` crée un système de "Routes". Plutôt que de dire "Affiche moi la page X par-dessus la Y", le système fonctionne comme une adresse de site web (ex: `//login`). C'est beaucoup plus robuste.
> Dans le code-behind, on inclut notre logique "RBAC" (Role Based Access Control) : `AppShell` écoute la base de données, et vient dynamiquement "cacher" le bouton "Administrateur" dans le menu de gauche si l'utilisateur qui vient de se connecter n'est qu'un technicien.

---

## Conclusion et Exécution

Lorsque l'étudiante compilera après avoir suivi ces étapes méthodologiquement, le compilateur liera toutes les interfaces, instanciera les services via le moteur MAUI, connectera le réseau avec Supabase, allumera l'écran d'accueil, qui lui-même s'animera en écoutant les variables de son ViewModel. Le projet est mathématiquement parfait.
