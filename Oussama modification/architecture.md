# Architecture du Projet : LEONI RFID (.NET MAUI)

Ce document décrit en détail l'organisation des fichiers et l'architecture logicielle du projet LEONI RFID. Il est conçu pour aider le jury (et les futurs développeurs) à comprendre comment le projet est structuré et pourquoi ces choix ont été faits.

---

## 1. Le Modèle MVVM (Model-View-ViewModel)

Le projet est entièrement construit autour du pattern **MVVM**. Ce choix architectural est le standard de l'industrie pour les applications C# (.NET MAUI, WPF, UWP) car il permet une séparation claire entre l'interface utilisateur et la logique métier.

*   **View (Vue)** : Interface graphique, strictement limitée à l'affichage (fichiers `.xaml` et code-behind minimaliste).
*   **ViewModel** : Le "cerveau" de l'interface. Ne connaît pas l'existence de la vue, mais gère les règles métiers, les données et les commandes (clics).
*   **Model (Modèle)** : Les données et les structures fondamentales de l'application (ex: Objet `Machine`, Objet `Profile`).

 Grâce au **DataBinding**, la Vue se met automatiquement à jour lorsque les données du ViewModel changent (via `ObservableProperty`).

---

## 2. Structure Répertoire (Folder Structure)

L'arborescence du code source a été pensée pour être modulaire et évolutive.

```text
📁 LeoniRFID/
 ┣ 📁 Models/         -> Contient les définitions des objets (les "Moules" de données).
 ┣ 📁 Views/          -> Contient les écrans de l'application (UI et design).
 ┣ 📁 ViewModels/     -> Contient la logique d'interaction et la préparation des données.
 ┣ 📁 Services/       -> Contient l'intelligence métier et les accès externes (Bases de données, Matériel).
 ┣ 📁 Helpers/        -> Contient les outils transversaux (Constantes de configuration, Convertisseurs XAML).
 ┣ 📁 Resources/      -> Contient toutes les images, icônes, fontes d'écriture, et dictionnaires de Styles/Couleurs.
 ┣ 📁 Platforms/      -> Contient le code spécifique à chaque OS (ici, intégration avec Android DataWedge).
 ┣ 📄 App.xaml        -> Le point de départ des ressources globales.
 ┣ 📄 AppShell.xaml   -> Le chef d'orchestre du routage et de la navigation.
 ┗ 📄 MauiProgram.cs  -> L'initialisation du conteneur d'injection de dépendances.
```

---

## 3. Analyse Intelligente des Couches (Layers)

### A. La Couche Matérielle (Hardware Integration)
La gestion du lecteur RFID n'encombre pas les ViewModels. Elle est isolée dans la couche **Services** :
*   Le design utilise une **Interface** `IRfidService` pour le découplage. 
*   L'implémentation Android passe par `DataWedgeIntentReceiver` (dans *Platforms/Android*). Ce choix permet d'utiliser le plein potentiel industriel du terminal Zebra **sans alourdir l'application avec un SDK propriétaire lourd**. 

### B. L'Accès aux Données (BaaS Supabase)
Le backend n'est pas monolithique. L'application agit comme un "Client Riche" et se connecte à un **BaaS** (Backend-as-a-Service) via `SupabaseService.cs`.
*   Toutes les interactions avec la base de données asynchrones (`async / await`) sont contenues dans ce seul fichier de service global.
*   Les données reçues sont traduites en Modèles (ex: la classe `Machine.cs`) grâce à un ORM (Object-Relational Mapping, via la librairie `postgrest-csharp`). L'attribut `[Table("machines")]` indique comment la donnée transite.

### C. La Couche Chef d'Orchestre (Dependency Injection)
Le fichier `MauiProgram.cs` utilise l'injection de dépendances héritée du monde ASP.NET Core :
*   Les services lourds (`SupabaseService`, `ExcelService`) sont ajoutés en **Singleton** (une seule instance en mémoire).
*   Les écrans et les processus de vues (`LoginViewModel`, `LoginPage`) sont ajoutés en **Transient** (détruits et recréés à chaque ouverture pour garantir des données fraîches).
Ce moteur prévient les "fuites de mémoire" et offre une application très fluide.

---

## 4. Flux de Données Type (Exemple Pratique : Le Scanner)

Comment une donnée circule-t-elle lors d'un scan RFID ?

1.  **Le Matériel :** Le technicien appuie sur la gâchette du Zebra. Le capteur physique lit un tag (ex: "EPC-123").
2.  **L'Écouteur Android :** `DataWedgeIntentReceiver` capte nativement l'Intent diffusé par l'OS Android.
3.  **Le Service :** Il renvoie cet "EPC-123" sous forme d'événement vers notre `IRfidService`.
4.  **Le ViewModel (`ScanViewModel`) :** Il écoute le service. Il récupère "EPC-123" et "demande" à la base de données : *`_supabase.GetMachineByTagId("EPC-123")`*.
5.  **Data Binding :** La base de données répond. Le ViewModel met à jour ses propriétés locales (ex: `ScannedMachine`).
6.  **La Vue XAML (`ScanPage`) :** L'interface écoute passivement le ViewModel. Dès que `ScannedMachine` est modifié par le code C#, l'interface (couleurs, libellés) se redessine automatiquement à l'écran sans le moindre calcul complexe côté UI.

---

## 5. Bonnes Pratiques Adoptées (Clean Code)

*   **Principe DRY (Don't Repeat Yourself)** : Centralisation des configurations critiques dans `Helpers/Constants.cs`. Un changement de base de données ne requiert la modification que d'une seule ligne de code.
*   **Contrôle Centralisé des Designs** : L'utilisation de `ResourceDictionary` dans `App.xaml` garantit que si demain la charte graphique de LEONI passe du "Bleu Foncé" au "Noir", il suffit de modifier `Colors.xaml` et `Styles.xaml` pour que l'application entière change d'allure.
*   **Formatage (Converters)** : Pour afficher une couleur ou masquer un composant, le principe d'interface est délégué à de petites classes très rapides nommées "Convertisseurs XAML" (ex: `StatusToColorConverter`), qui allègent drastiquement les IF/THEN dans le ViewModel principal.
