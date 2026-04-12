# 📡 Guide Complet : Intégration du Lecteur RFID Zebra MC3300x avec .NET MAUI

> **Auteur / Encadrant** : Oussama Souissi  
> **Matériel** : Zebra MC3300x (Modèle MC3300U) — PDA industriel durci avec lecteur RFID UHF intégré  
> **Application** : LeoniRFID (.NET MAUI - Android)

---

## 📖 Table des Matières

1. [Présentation du Matériel](#1--présentation-du-matériel-zebra-mc3300x)
2. [Philosophie d'Intégration : DataWedge vs SDK](#2--philosophie-dintégration--datawedge-vs-sdk)
3. [Schéma Global du Flux de Données](#3--schéma-global-du-flux-de-données)
4. [ÉTAPE 1 — Configuration de DataWedge sur le PDA Zebra](#4--étape-1--configuration-de-datawedge-sur-le-pda-zebra)
5. [ÉTAPE 2 — Le Récepteur Android Natif (BroadcastReceiver)](#5--étape-2--le-récepteur-android-natif-broadcastreceiver)
6. [ÉTAPE 3 — Le Contrat d'Abstraction (Interface IRfidService)](#6--étape-3--le-contrat-dabstraction-interface-irfidservice)
7. [ÉTAPE 4 — L'Implémentation du Service RFID (RfidService)](#7--étape-4--limplémentation-du-service-rfid-rfidservice)
8. [ÉTAPE 5 — Enregistrement dans l'Injection de Dépendances (MauiProgram.cs)](#8--étape-5--enregistrement-dans-linjection-de-dépendances-mauprogramcs)
9. [ÉTAPE 6 — Le ViewModel : Cerveau du Scan (ScanViewModel)](#9--étape-6--le-viewmodel--cerveau-du-scan-scanviewmodel)
10. [ÉTAPE 7 — L'Interface Utilisateur XAML (ScanPage)](#10--étape-7--linterface-utilisateur-xaml-scanpage)
11. [ÉTAPE 8 — Le Code-Behind et le Cycle de Vie (ScanPage.xaml.cs)](#11--étape-8--le-code-behind-et-le-cycle-de-vie-scanpagexamlcs)
12. [ÉTAPE 9 — Les Modèles de Données (Machine & ScanEvent)](#12--étape-9--les-modèles-de-données-machine--scanevent)
13. [ÉTAPE 10 — Configuration Android Manifest](#13--étape-10--configuration-android-manifest)
14. [Résumé du Flux Complet (De la Gâchette à la Base de Données)](#14--résumé-du-flux-complet)
15. [Déploiement sur le Zebra (Méthode Recommandée PFE - Câble USB)](#15--déploiement-sur-le-zebra-méthode-recommandée-pfe---câble-usb)
16. [Hébergement sur VPS Local (Sécurité Industrielle LEONI)](#16--hébergement-sur-vps-local-sécurité-industrielle-leoni)
17. [Dépannage et FAQ](#17--dépannage-et-faq)

---

## 1 — Présentation du Matériel Zebra MC3300x

### Fiche Technique du Terminal

| Caractéristique       | Valeur                                      |
|----------------------|---------------------------------------------|
| **Fabricant**        | Zebra Technologies Corp. (Holtsville, NY)   |
| **Modèle**           | MC3300x / MC3300U                            |
| **Alimentation**     | 5V ⎓ 1.8A (batterie rechargeable)          |
| **OS**               | Android (AOSP)                               |
| **Clavier**          | Clavier physique alphanumérique complet      |
| **Capteurs intégrés**| Laser barcode (1D/2D) + Antenne RFID UHF    |
| **Classe Laser**     | Classe 2 (630–680 nm, 1mW)                  |
| **Robustesse**       | Terminal durci (IP54, chutes de 1.5m)        |
| **Usage**            | Environnement industriel, entrepôts, usines |

### Composants Clés pour Notre Projet

- **Gâchette physique (Trigger)** : Le bouton jaune latéral. Quand on appuie dessus, le PDA active son antenne RFID UHF (ou son laser barcode selon la configuration) et scanne les tags dans un rayon de ~5 mètres.
- **Application DataWedge** : Application système pré-installée par Zebra sur tous ses PDA. C'est elle qui transforme le signal physique brut de l'antenne en donnée exploitable (texte EPC).
- **Android OS** : Le MC3300x tourne sous Android. Notre application .NET MAUI se compile en APK Android et s'installe directement dessus.

---

## 2 — Philosophie d'Intégration : DataWedge vs SDK

### ❌ L'approche classique (SDK Zebra) — Ce qu'on ne fait PAS

Zebra propose un SDK Java/Android lourd (`EMDK for Android`) pour accéder directement au matériel RFID. Cette approche présente de nombreux inconvénients :

| Problème                       | Impact                                              |
|-------------------------------|-----------------------------------------------------|
| SDK Java incompatible avec C# | Nécessite des bindings JNI complexes                |
| Dépendance du SDK Zebra       | Mise à jour obligée à chaque nouvelle version Zebra |
| Poids du package              | +50 MB de librairies ajoutées à l'APK               |
| Complexité d'intégration      | Des centaines de lignes de code d'initialisation     |
| Verrouillage constructeur     | Si l'usine change de marque de PDA, tout est à réécrire |

### ✅ Notre approche (DataWedge + Intents Android) — Ce qu'on fait

Au lieu d'utiliser le SDK, on exploite **DataWedge**, l'application système de Zebra qui fait déjà tout le travail lourd (gérer l'antenne, décoder le signal radio, extraire l'EPC). On lui demande simplement de **diffuser le résultat nativement sous forme d'Intent Android** (un message interne au système d'exploitation).

Notre application .NET MAUI n'a alors qu'à **écouter ce message** avec un simple `BroadcastReceiver` Android — une classe C# de **36 lignes** seulement !

| Avantage                        | Détail                                                  |
|--------------------------------|--------------------------------------------------------|
| **Zéro SDK externe**           | Aucune dépendance Zebra dans notre `csproj`             |
| **36 lignes de code**          | vs des centaines avec un SDK                            |
| **Agnostique constructeur**    | Si on change pour Datalogic/Honeywell, seul le profil DataWedge change |
| **Géré par l'OS**              | Android gère la mémoire, la batterie et le cycle de vie |
| **Découplage total**           | L'application ne sait même pas qu'elle parle à du matériel Zebra |

---

## 3 — Schéma Global du Flux de Données

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ZEBRA MC3300x (Matériel Physique)                    │
│                                                                         │
│  ┌──────────┐    ┌───────────────────┐    ┌──────────────────────────┐  │
│  │ Gâchette │───▶│ Antenne RFID UHF  │───▶│ DataWedge (App Système)  │  │
│  │  (Jaune) │    │ scanne le tag     │    │ décode EPC brut          │  │
│  └──────────┘    └───────────────────┘    └──────────┬───────────────┘  │
│                                                      │                  │
│                                         Broadcast Intent Android        │
│                                  Action: "com.symbol.datawedge.         │
│                                           api.ACTION"                   │
│                                  Extra: "com.symbol.datawedge.          │
│                                          data_string" = "EPC-ABC123"   │
│                                                      │                  │
└──────────────────────────────────────────────────────┼──────────────────┘
                                                       │
                                                       ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                    NOTRE APPLICATION .NET MAUI                           │
│                                                                          │
│  ┌────────────────────────────────────────────┐                          │
│  │  COUCHE ANDROID NATIVE                      │                          │
│  │  DataWedgeIntentReceiver.cs                 │                          │
│  │  [BroadcastReceiver] — écoute l'Intent     │                          │
│  │  Extrait l'EPC → déclenche événement C#    │                          │
│  └──────────────────┬─────────────────────────┘                          │
│                     │ événement static TagReceived                       │
│                     ▼                                                    │
│  ┌────────────────────────────────────────────┐                          │
│  │  COUCHE SERVICE                             │                          │
│  │  RfidService.cs (implémente IRfidService)  │                          │
│  │  Pont entre Android natif et .NET MAUI     │                          │
│  │  #if ANDROID → s'abonne au Receiver        │                          │
│  └──────────────────┬─────────────────────────┘                          │
│                     │ événement TagScanned                               │
│                     ▼                                                    │
│  ┌────────────────────────────────────────────┐                          │
│  │  COUCHE VIEWMODEL (MVVM)                    │                          │
│  │  ScanViewModel.cs                           │                          │
│  │  Reçoit l'EPC → cherche dans Supabase      │                          │
│  │  → met à jour les ObservableProperties     │                          │
│  │  → enregistre un ScanEvent                 │                          │
│  └──────────────────┬─────────────────────────┘                          │
│                     │ Data Binding                                       │
│                     ▼                                                    │
│  ┌────────────────────────────────────────────┐                          │
│  │  COUCHE VUE (XAML)                          │                          │
│  │  ScanPage.xaml                              │                          │
│  │  Affiche automatiquement la machine         │                          │
│  │  Boutons : INSTALLER / RETIRER / MAINT.    │                          │
│  └────────────────────────────────────────────┘                          │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 4 — ÉTAPE 1 : Configuration de DataWedge sur le PDA Zebra

> ⚠️ **Cette étape se fait UNE SEULE FOIS sur le terminal physique Zebra MC3300x.** Elle ne requiert aucun code — c'est de la configuration matérielle.

### 4.1 — Ouvrir DataWedge

Sur le PDA Zebra, ouvrez le **tiroir d'applications** (App Drawer) et cherchez l'icône **DataWedge** (icône bleue avec un code-barres). C'est une application **pré-installée d'usine** sur tous les terminaux Zebra.

### 4.2 — Créer un Nouveau Profil Dédié

1. Appuyez sur le menu **☰** (3 barres ou 3 points en haut à droite).
2. Sélectionnez → **« New profile »** (Nouveau profil).
3. Nommez-le : **`LeoniRFID`**.
4. Validez.

> 📝 **Pourquoi un profil dédié ?**  
> DataWedge permet de créer des profils différents pour chaque application installée. Ainsi, si une autre appli industrielle utilise le même PDA, les configurations de scan ne se mélangent pas.

### 4.3 — Associer le Profil à Notre Application

1. Ouvrez le profil `LeoniRFID` que vous venez de créer.
2. Descendez jusqu'à la section **« Associated Apps »** (Applications associées).
3. Appuyez dessus, puis sur le menu **+** pour ajouter.
4. Dans la liste des applications installées, sélectionnez :
   - **Application** : `com.leoni.rfid.production`
   - **Activity** : `*` (astérisque = toutes les pages de l'application)
5. Validez.

> 📝 **Explication** : L'identifiant `com.leoni.rfid.production` correspond au `<ApplicationId>` déclaré dans notre fichier `LeoniRFID.csproj`. DataWedge activera automatiquement notre profil uniquement lorsque l'application LeoniRFID est au premier plan.

### 4.4 — Configurer l'Entrée (Input)

Dans le profil, configurez les sources d'entrée :

| Paramètre           | Valeur        | Explication                                  |
|---------------------|---------------|----------------------------------------------|
| **Barcode Input**   | ✅ Activé      | Pour lire les codes-barres 1D/2D             |
| **RFID Input**      | ✅ Activé      | Pour lire les tags RFID UHF des machines     |
| **DCP (Data Capture Plus)** | ❌ Désactivé | Pas nécessaire pour notre usage              |

### 4.5 — Configurer la Sortie (Output) — LE POINT CLÉ

C'est ici que la magie opère. On dit à DataWedge **comment livrer la donnée scannée** à notre application :

#### A) Désactiver la sortie clavier (Keystroke Output)
- Trouvez **« Keystroke Output »** → **Désactiver** (Enable = OFF).

> 📝 **Pourquoi ?** Par défaut, DataWedge simule un clavier : le code scanné est tapé lettre par lettre dans le champ texte actif. C'est lent, imprécis, et inutilisable programmatiquement.

#### B) Activer la sortie par Intent (Intent Output)
- Trouvez **« Intent Output »** → **Activer** (Enable = ON).
- Configurez les paramètres suivants :

| Paramètre                | Valeur                                      |
|--------------------------|---------------------------------------------|
| **Intent Action**        | `com.symbol.datawedge.api.ACTION`            |
| **Intent Category**      | `android.intent.category.DEFAULT`            |
| **Intent Delivery**      | `Broadcast intent` ⚡                        |

> ⚠️ **CRITIQUE : Le paramètre « Intent Delivery » DOIT être sur `Broadcast intent`** (et non pas `Send via startActivity` ou `Send via startService`). C'est ce qui permet à notre `BroadcastReceiver` C# de capter le message silencieusement, sans ouvrir de nouvelle fenêtre.

### 4.6 — Tester la Configuration

1. Ouvrez n'importe quelle application avec un champ texte (ex: le Bloc-Notes Android).
2. Appuyez sur la gâchette jaune du PDA.
3. **Rien ne devrait être tapé dans le champ texte** (car on a désactivé Keystroke).
4. Cela confirme que DataWedge envoie désormais les données **uniquement par Intent**.

### 4.7 — Résumé Visuel de la Configuration

```
DataWedge → Profil "LeoniRFID"
├── Associated Apps: com.leoni.rfid.production / *
├── INPUT
│   ├── Barcode Input:  ✅ Enabled
│   └── RFID Input:     ✅ Enabled
├── OUTPUT
│   ├── Keystroke Output: ❌ Disabled
│   └── Intent Output:    ✅ Enabled
│       ├── Action:   com.symbol.datawedge.api.ACTION
│       ├── Category: android.intent.category.DEFAULT
│       └── Delivery: Broadcast intent
```

---

## 5 — ÉTAPE 2 : Le Récepteur Android Natif (BroadcastReceiver)

### Fichier : `Platforms/Android/DataWedgeIntentReceiver.cs`

Ce fichier est le **premier point de contact** entre le système d'exploitation Android et notre code C#. C'est une "antenne" qui écoute les messages radio internes d'Android.

```csharp
using Android.App;
using Android.Content;

namespace LeoniRFID.Platforms.Android;

/// <summary>
/// 🎓 Pédagogie PFE : Intégration Matérielle Native Android (BroadcastReceiver)
/// Ce composant très spécifique à Android "écoute" les messages internes (Intents)
/// du système d'exploitation. Quand le lecteur physique (Zebra) scanne un tag,
/// l'appli système Zebra (DataWedge) diffuse un Intent. Ce code le capte
/// pour récupérer le code EPC sans avoir besoin d'un SDK complexe.
/// C'est une façon très élégante d'utiliser les capacités du terminal industriel.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilterAttribute(
    new string[] { "com.symbol.datawedge.api.ACTION" }, 
    Categories = new string[] { "android.intent.category.DEFAULT" })]
public class DataWedgeIntentReceiver : BroadcastReceiver
{
    // Événement statique C# — le pont vers le monde .NET MAUI
    public static event EventHandler<string>? TagReceived;

    public override void OnReceive(Context? context, Intent? intent)
    {
        // Vérification de sécurité : on ignore les Intents inconnus
        if (intent == null || intent.Action != "com.symbol.datawedge.api.ACTION") return;

        // DataWedge EPC RFID uses specific extra keys
        // Note: For newer Zebra devices, the key might vary based on DataWedge version.
        // Usually, the EPC is in "com.symbol.datawedge.data_string"
        
        // Extraction de la donnée EPC depuis le "paquet" de l'Intent Android
        string epc = intent.GetStringExtra("com.symbol.datawedge.data_string") ?? string.Empty;

        if (!string.IsNullOrEmpty(epc))
        {
            // On déclenche l'événement C# avec la valeur EPC
            TagReceived?.Invoke(this, epc);
        }
    }
}
```

### Décomposition Ligne par Ligne

#### Les Attributs (annotations)

```csharp
[BroadcastReceiver(Enabled = true, Exported = true)]
```
- **`[BroadcastReceiver]`** : Dit au compilateur .NET MAUI de générer automatiquement l'entrée XML correspondante dans le `AndroidManifest.xml` final. Sans cet attribut, Android ne sait pas que notre classe existe.
- **`Enabled = true`** : Le récepteur est actif (peut recevoir des messages).
- **`Exported = true`** : Le récepteur est visible par les autres applications (nécessaire car DataWedge est une application externe à la nôtre).

```csharp
[IntentFilterAttribute(
    new string[] { "com.symbol.datawedge.api.ACTION" }, 
    Categories = new string[] { "android.intent.category.DEFAULT" })]
```
- **`IntentFilterAttribute`** : Définit la "fréquence radio" que notre antenne écoute.
- **`"com.symbol.datawedge.api.ACTION"`** : C'est exactement la même chaîne configurée dans DataWedge à l'Étape 1 (section Intent Output → Action). **Les deux doivent correspondre parfaitement.**
- **`android.intent.category.DEFAULT`** : Catégorie standard Android.

#### L'événement statique

```csharp
public static event EventHandler<string>? TagReceived;
```
- **`static`** : L'événement est partagé par toute l'application. Comme Android instancie le `BroadcastReceiver` lui-même (on ne fait pas `new DataWedgeIntentReceiver()`), un événement statique est le seul moyen fiable de communiquer avec le reste de notre code .NET MAUI.
- **`EventHandler<string>`** : Le type de données transmis est `string` (le code EPC du tag RFID).

#### La méthode OnReceive

```csharp
public override void OnReceive(Context? context, Intent? intent)
```
- C'est la méthode qu'Android appelle automatiquement quand un Intent correspondant à notre filtre arrive. On n'appelle jamais cette méthode nous-mêmes — c'est le système d'exploitation qui le fait.

---

## 6 — ÉTAPE 3 : Le Contrat d'Abstraction (Interface IRfidService)

### Fichier : `Services/IRfidService.cs`

```csharp
namespace LeoniRFID.Services;

// 🎓 Pédagogie PFE : Le Principe d'Abstraction (Interface)
// Une interface ("I" devant le nom) définit un CONTRAT : elle dit QUOI faire, mais pas COMMENT.
// Cela permet de remplacer facilement l'implémentation réelle (lecteur Zebra physique)
// par une simulation (pour tester sur un PC sans lecteur RFID).
// C'est le principe d'Inversion de Dépendance (le "D" de SOLID).
public interface IRfidService
{
    // Événement déclenché automatiquement quand un tag RFID est détecté
    event EventHandler<string> TagScanned;

    // État actuel du service (écoute active ou non)
    bool IsListening { get; }

    // Démarrer l'écoute du lecteur RFID physique
    void StartListening();

    // Arrêter l'écoute (économie de batterie)
    void StopListening();

    // Simuler un scan pour les tests sur émulateur/PC
    void SimulateScan(string epc);  // For development/testing
}
```

### Pourquoi une Interface ?

L'interface est un **contrat** qui sépare le "quoi" du "comment". Voici les scénarios qu'elle permet :

| Scénario                          | Implémentation utilisée              |
|-----------------------------------|--------------------------------------|
| PDA Zebra MC3300x réel             | `RfidService` (avec DataWedge)        |
| Émulateur Android (développement) | `RfidService` (avec `SimulateScan()`) |
| Test unitaire automatisé          | `MockRfidService` (fausse classe)     |
| Changement de marque (Honeywell)  | `HoneywellRfidService` (même contrat) |

> 📝 **Point Soutenance** : Le `ScanViewModel` ne sait pas s'il parle à un vrai lecteur Zebra ou à une simulation. Il ne connaît que le contrat `IRfidService`. C'est le principe **SOLID "D" (Dependency Inversion)** : les modules de haut niveau ne dépendent pas des détails, ils dépendent des abstractions.

---

## 7 — ÉTAPE 4 : L'Implémentation du Service RFID (RfidService)

### Fichier : `Services/RfidService.cs`

```csharp
namespace LeoniRFID.Services;

/// <summary>
/// 🎓 Pédagogie PFE : Intégration Matérielle (Hardware)
/// Ce service fait le lien entre le lecteur RFID physique (Zebra MC3300x)
/// et notre application .NET MAUI. Sur un vrai appareil Zebra, l'application
/// DataWedge envoie un "Intent Android" contenant le code EPC du tag scanné.
/// Sur un PC de développement (sans lecteur), on utilise SimulateScan().
/// </summary>
public class RfidService : IRfidService
{
    public event EventHandler<string>? TagScanned;
    public bool IsListening { get; private set; }

    /// <summary>
    /// Start listening for DataWedge scan intents.
    /// The actual BroadcastReceiver is registered on Android platform.
    /// </summary>
    public void StartListening()
    {
        IsListening = true;

        // 🎓 Pédagogie PFE : Compilation Conditionnelle (#if ANDROID)
        // Le code entre #if ANDROID et #endif ne sera compilé QUE pour Android.
        // Sur Windows ou iOS, ce bloc est totalement ignoré par le compilateur.
        // C'est ainsi qu'on écrit du code multi-plateforme dans .NET MAUI.
#if ANDROID
        // S'abonner aux scans physiques venant du lecteur Zebra
        LeoniRFID.Platforms.Android.DataWedgeIntentReceiver.TagReceived += OnTagReceived;
#endif
    }

    public void StopListening()
    {
        IsListening = false;
#if ANDROID
        LeoniRFID.Platforms.Android.DataWedgeIntentReceiver.TagReceived -= OnTagReceived;
#endif
    }

    private void OnTagReceived(object? sender, string epc)
    {
        // Normalisation de l'EPC : suppression des espaces et conversion en majuscules
        TagScanned?.Invoke(this, epc.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Simulate an RFID scan — use for testing on non-Zebra devices.
    /// </summary>
    public void SimulateScan(string epc)
    {
        if (!string.IsNullOrWhiteSpace(epc))
            TagScanned?.Invoke(this, epc.Trim().ToUpperInvariant());
    }
}
```

### Décomposition des Concepts Clés

#### La Compilation Conditionnelle `#if ANDROID`

```csharp
#if ANDROID
    LeoniRFID.Platforms.Android.DataWedgeIntentReceiver.TagReceived += OnTagReceived;
#endif
```

C'est une directive du **pré-processeur C#**. Au moment de la compilation :
- Si la cible est `net9.0-android` → le code est **inclus** dans le binaire (APK).
- Si on tentait (par erreur) de compiler pour une autre plateforme → le code serait **supprimé** comme s'il n'existait pas.

> 📝 **Pourquoi c'est nécessaire si l'application est 100% dédiée à Android ?**  
> Notre projet `LeoniRFID` est optimisé et limité à Android dans le `.csproj` (`<TargetFrameworks>net9.0-android</TargetFrameworks>`) pour être le plus léger possible. Cependant, .NET MAUI reste fondamentalement un framework cross-platform.   
> Entourer les références natives (comme `BroadcastReceiver` qui n'existe que chez Google/Android) par `#if ANDROID` est une **bonne pratique d'architecture stricte**. Cela sert de garde-fou et empêche l'IDE (Visual Studio) de s'embrouiller lors de l'analyse du code, supprimant ainsi les "fausses erreurs". Cela montre au jury une excellente maîtrise des mécanismes internes du framework MAUI.

#### L'abonnement aux événements (`+=` et `-=`)

```csharp
DataWedgeIntentReceiver.TagReceived += OnTagReceived;  // S'abonner
DataWedgeIntentReceiver.TagReceived -= OnTagReceived;  // Se désabonner
```

- **`+=`** : "Quand l'événement `TagReceived` sera déclenché par le `BroadcastReceiver`, appelle ma méthode `OnTagReceived`."
- **`-=`** : "Arrête d'écouter." Crucial pour éviter les **fuites mémoire** (memory leaks). Si on oublie le `-=`, des copies fantômes du récepteur continuent à tourner en mémoire même après avoir quitté la page.

#### La normalisation de l'EPC

```csharp
TagScanned?.Invoke(this, epc.Trim().ToUpperInvariant());
```

- **`.Trim()`** : Supprime les espaces avant/après (les lecteurs RFID ajoutent parfois des espaces parasites).
- **`.ToUpperInvariant()`** : Convertit en majuscules. Un même tag lu deux fois peut donner `"aB12cD"` puis `"Ab12Cd"`. En normalisant en `"AB12CD"`, on garantit une comparaison fiable dans Supabase.

---

## 8 — ÉTAPE 5 : Enregistrement dans l'Injection de Dépendances (MauiProgram.cs)

### Fichier : `MauiProgram.cs` (extrait pertinent)

```csharp
// ── Services (La Couche Logique & Accès aux données) ──────────────────
builder.Services.AddSingleton<IRfidService, RfidService>();
```

### Pourquoi `AddSingleton` pour le RFID ?

| Mode DI          | Comportement                                     | Usage                          |
|-----------------|--------------------------------------------------|--------------------------------|
| `AddSingleton`  | **1 seule instance** pour toute la durée de vie de l'app | Services, connexions réseau    |
| `AddTransient`  | **Nouvelle instance** à chaque injection          | ViewModels, Pages              |

Le `RfidService` est un **Singleton** car :
1. On veut **un seul point d'écoute** du matériel RFID dans toute l'application.
2. Si on créait plusieurs instances, chaque page "Scanner" créerait son propre abonnement aux événements Android, causant des **scans dupliqués**.
3. L'état `IsListening` doit être partagé globalement.

### Le Mapping `<Interface, Implémentation>`

```csharp
AddSingleton<IRfidService, RfidService>();
//           ↑ Le contrat    ↑ Le code réel
```

Quand le `ScanViewModel` demande un `IRfidService` dans son constructeur, le moteur DI de MAUI lui donne automatiquement l'unique instance de `RfidService`. Le ViewModel ne sait jamais quelle classe concrète il utilise.

---

## 9 — ÉTAPE 6 : Le ViewModel — Cerveau du Scan (ScanViewModel)

### Fichier : `ViewModels/ScanViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Le Scanner RFID (Cœur Métier de l'Application)
// Ce ViewModel gère le flux complet du scan RFID :
// 1. Démarrer/arrêter l'écoute du lecteur physique (Zebra MC3300x)
// 2. Recevoir le code EPC (identifiant unique du tag RFID)
// 3. Chercher la machine correspondante dans Supabase
// 4. Enregistrer un événement de traçabilité (ScanEvent)
public partial class ScanViewModel : BaseViewModel
{
    private readonly IRfidService    _rfid;
    private readonly SupabaseService _supabase;

    public ScanViewModel(SupabaseService supabase, IRfidService rfid)
    {
        _supabase = supabase;
        _rfid = rfid;
        Title  = "Scanner RFID";

        // 🎓 S'abonner à l'événement TagScanned du service RFID.
        // Dès qu'un tag est détecté, la méthode OnTagScanned sera appelée automatiquement.
        _rfid.TagScanned += OnTagScanned;
    }

    // ── Propriétés Observables (liées au XAML par Data Binding) ──────────

    [ObservableProperty] private string   _scannedEpc     = string.Empty;
    [ObservableProperty] private string   _manualEpc      = string.Empty;
    [ObservableProperty] private Machine? _foundMachine;
    [ObservableProperty] private bool     _isScanning     = false;
    [ObservableProperty] private bool     _tagNotFound    = false;
    [ObservableProperty] private string   _scanStatusText = "Approchez un tag RFID…";

    public bool HasMachine => FoundMachine is not null;

    // ── Commandes (déclenchées par les boutons XAML) ─────────────────────

    [RelayCommand]
    private void StartScan()
    {
        IsScanning     = true;
        TagNotFound    = false;
        FoundMachine   = null;
        ScanStatusText = "Lecture en cours…";
        ScannedEpc     = string.Empty;
        ErrorMessage   = string.Empty;
        _rfid.StartListening();  // ← Active le BroadcastReceiver Android
    }

    [RelayCommand]
    private void StopScan()
    {
        IsScanning     = false;
        ScanStatusText = "Scan arrêté.";
        _rfid.StopListening();   // ← Désactive le BroadcastReceiver Android
    }

    [RelayCommand]
    private async Task ManualScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualEpc)) return;
        _rfid.StopListening();
        await ProcessEpcAsync(ManualEpc.Trim().ToUpperInvariant());
    }

    // ── Callback déclenché automatiquement par le matériel Zebra ────────

    private async void OnTagScanned(object? sender, string epc)
    {
        _rfid.StopListening();  // Arrête l'écoute immédiatement après un scan réussi
        IsScanning = false;
        await ProcessEpcAsync(epc);
    }

    // ── Logique Métier : traitement de l'EPC scanné ─────────────────────

    private async Task ProcessEpcAsync(string epc)
    {
        IsBusy     = true;
        ScannedEpc = epc;
        ScanStatusText = $"EPC: {epc}";

        try
        {
            // 1. Chercher la machine dans Supabase via son tag RFID
            FoundMachine = await _supabase.GetMachineByTagIdAsync(epc);
            TagNotFound  = FoundMachine is null;

            if (TagNotFound)
                ScanStatusText = "⚠️ Tag inconnu — non enregistré dans la base.";
            else
                ScanStatusText = $"✅ Machine trouvée : {FoundMachine!.Name}";

            OnPropertyChanged(nameof(HasMachine));

            // 2. 🎓 Enregistrement de la Traçabilité (ScanEvent)
            // Chaque scan réussi crée un "ScanEvent" dans la base de données.
            // C'est le journal d'audit : QUI a scanné QUOI et QUAND ?
            if (FoundMachine is not null)
            {
                var scanEvent = new ScanEvent
                {
                    TagId = epc,
                    MachineId = FoundMachine.Id,
                    UserId = _supabase.CurrentProfile?.Id,
                    EventType = "Scan",
                    Timestamp = DateTime.UtcNow
                };
                await _supabase.SaveScanEventAsync(scanEvent);
            }
        }
        finally { IsBusy = false; }
    }

    // ── Changement de Statut de la Machine ──────────────────────────────

    [RelayCommand]
    private async Task SetStatusAsync(string status)
    {
        if (FoundMachine is null || IsBusy) return;
        IsBusy = true;
        try
        {
            string label = status switch
            {
                "Installed"   => "Installer",
                "Removed"     => "Retirer",
                "Maintenance" => "Mettre en maintenance",
                _             => status
            };

            bool confirm = await Shell.Current.DisplayAlert(
                "Confirmer", $"Confirmer : {label} la machine {FoundMachine.Name} ?", "Oui", "Non");
            if (!confirm) return;

            FoundMachine.Status = status;
            if (status == "Installed")  FoundMachine.InstallationDate = DateTime.Now;
            if (status == "Removed")    FoundMachine.ExitDate         = DateTime.Now;

            await _supabase.SaveMachineAsync(FoundMachine);

            // Traçabilité : enregistrer le changement de statut
            var scanEvent = new ScanEvent
            {
                TagId = FoundMachine.TagId,
                MachineId = FoundMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = status,
                Notes = $"Status changed to {status} from PDA",
                Timestamp = DateTime.UtcNow
            };

            await _supabase.SaveScanEventAsync(scanEvent);

            SetSuccess($"Statut mis à jour : {status}");
            OnPropertyChanged(nameof(FoundMachine));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ViewDetailAsync()
    {
        if (FoundMachine is null) return;
        await Shell.Current.GoToAsync($"machinedetail?machineId={FoundMachine.Id}");
    }

    // 🎓 Très important : libérer le matériel quand la page disparaît
    public void OnDisappearing() => _rfid.StopListening();
}
```

### Points Clés pour la Soutenance

#### Le Constructeur (Injection de Dépendances + Abonnement)

```csharp
public ScanViewModel(SupabaseService supabase, IRfidService rfid)
{
    _rfid.TagScanned += OnTagScanned;
}
```

Le ViewModel ne crée jamais un `new RfidService()`. Il le reçoit **automatiquement** du moteur DI de MAUI (configuré dans `MauiProgram.cs`). C'est ce qui permet le découplage total.

#### Les `[ObservableProperty]` et `[RelayCommand]`

Grâce au `CommunityToolkit.Mvvm`, le code est ultra-concis :

```csharp
[ObservableProperty] private string _scanStatusText = "Approchez un tag RFID…";
```

Le toolkit **génère automatiquement** au moment de la compilation :
- Une propriété publique `ScanStatusText` avec getter/setter.
- L'appel à `OnPropertyChanged()` dans le setter → le XAML est averti instantanément.

```csharp
[RelayCommand]
private void StartScan() { ... }
```

Le toolkit génère une propriété `StartScanCommand` de type `ICommand` que le XAML peut binder directement sur un bouton.

---

## 10 — ÉTAPE 7 : L'Interface Utilisateur XAML (ScanPage)

### Fichier : `Views/ScanPage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:LeoniRFID.ViewModels"
             x:Class="LeoniRFID.Views.ScanPage"
             x:DataType="vm:ScanViewModel"
             Title="Scanner RFID">

    <!-- 🎓 Pédagogie PFE : Layout en Grille (Grid)
         La page est découpée en 3 rangées :
         Row 0 (Auto) = Barre de statut + saisie manuelle EPC
         Row 1 (*)    = Zone centrale (animation de scan / résultat)
         Row 2 (Auto) = Boutons d'action en bas (Installer / Retirer / Maintenance) -->
    <Grid RowDefinitions="Auto, *, Auto" Padding="20" RowSpacing="20">

        <!-- ═══ ZONE HAUTE : Statut + Saisie Manuelle ═══ -->
        <VerticalStackLayout Grid.Row="0" Spacing="10">
            <Label Text="{Binding ScanStatusText}" 
                   Style="{StaticResource SubHeadingLabel}" 
                   HorizontalTextAlignment="Center"
                   TextColor="{Binding TagNotFound, Converter={StaticResource StatusToColor}}" />

            <!-- Champ de saisie manuelle EPC (pour tests sans lecteur physique) -->
            <Frame Style="{StaticResource CardAlt}" Padding="0" HeightRequest="50">
                <Grid ColumnDefinitions="*, Auto">
                    <Entry Placeholder="Saisie manuelle EPC..." 
                           Text="{Binding ManualEpc}"
                           Style="{StaticResource ModernEntry}"
                           Margin="10,0" />
                    <Button Grid.Column="1" Text="OK" 
                            Command="{Binding ManualScanCommand}" 
                            BackgroundColor="{StaticResource LeoniMidBlue}" 
                            CornerRadius="0" WidthRequest="60" />
                </Grid>
            </Frame>
        </VerticalStackLayout>

        <!-- ═══ ZONE CENTRALE : Animation ou Résultat ═══ -->
        <Grid Grid.Row="1" VerticalOptions="Center">
            
            <!-- Animation de chargement pendant le scan actif -->
            <VerticalStackLayout IsVisible="{Binding IsScanning}" Spacing="20" VerticalOptions="Center">
                <ActivityIndicator IsRunning="True" HeightRequest="100" WidthRequest="100" 
                                   Color="{StaticResource LeoniOrange}" />
                <Label Text="En attente de tag RFID..." 
                       Style="{StaticResource BodyLabel}" HorizontalTextAlignment="Center" />
            </VerticalStackLayout>

            <!-- Carte de résultat : machine trouvée -->
            <Frame IsVisible="{Binding HasMachine}" Style="{StaticResource Card}" 
                   VerticalOptions="Center" Padding="25">
                <VerticalStackLayout Spacing="15">
                    <HorizontalStackLayout Spacing="10" HorizontalOptions="Center">
                        <Label Text="EPC:" Style="{StaticResource CaptionLabel}" VerticalOptions="Center" />
                        <Label Text="{Binding ScannedEpc}" Style="{StaticResource BodyLabel}" 
                               FontFamily="RobotoBold" />
                    </HorizontalStackLayout>
                    
                    <BoxView Style="{StaticResource Divider}" />

                    <Label Text="{Binding FoundMachine.Name}" 
                           Style="{StaticResource HeadingLabel}" HorizontalTextAlignment="Center" />
                    <Label Text="{Binding FoundMachine.Department}" 
                           Style="{StaticResource SubHeadingLabel}" HorizontalTextAlignment="Center" />
                    
                    <!-- Badge de statut avec couleur dynamique via Converter -->
                    <Frame BackgroundColor="{Binding FoundMachine.Status, Converter={StaticResource StatusToBadge}}" 
                           Padding="10,5" CornerRadius="5" HorizontalOptions="Center" HasShadow="False">
                        <Label Text="{Binding FoundMachine.StatusDisplay}" 
                               TextColor="White" FontFamily="RobotoBold" />
                    </Frame>

                    <Button Text="Voir détails complets" 
                            Style="{StaticResource SecondaryButton}" 
                            Command="{Binding ViewDetailCommand}" 
                            Margin="0,15,0,0" />
                </VerticalStackLayout>
            </Frame>

            <!-- État "Tag non reconnu" -->
            <VerticalStackLayout IsVisible="{Binding TagNotFound}" Spacing="15" VerticalOptions="Center">
                <Label Text="⚠️" FontSize="64" HorizontalTextAlignment="Center" />
                <Label Text="Tag non reconnu" 
                       Style="{StaticResource HeadingLabel}" HorizontalTextAlignment="Center" />
                <Label Text="Ce tag RFID n'est pas associé à une machine dans la base LEONI." 
                       Style="{StaticResource BodyLabel}" HorizontalTextAlignment="Center" Opacity="0.7" />
                <Button Text="Réessayer" Style="{StaticResource SecondaryButton}" 
                        Command="{Binding StartScanCommand}" WidthRequest="200" HorizontalOptions="Center" />
            </VerticalStackLayout>

        </Grid>

        <!-- ═══ ZONE BASSE : Boutons d'Action Statut ═══ -->
        <!-- Visible uniquement si une machine a été trouvée -->
        <Grid Grid.Row="2" ColumnDefinitions="*, *, *" ColumnSpacing="10" 
              IsVisible="{Binding HasMachine}">
            <Button Text="INSTALLER" 
                    Command="{Binding SetStatusCommand}" CommandParameter="Installed"
                    BackgroundColor="{StaticResource StatusInstalled}" 
                    HeightRequest="60" CornerRadius="10" FontAttributes="Bold" />
            <Button Grid.Column="1" Text="RETIRER" 
                    Command="{Binding SetStatusCommand}" CommandParameter="Removed"
                    BackgroundColor="{StaticResource StatusRemoved}" 
                    HeightRequest="60" CornerRadius="10" FontAttributes="Bold" />
            <Button Grid.Column="2" Text="MAINT." 
                    Command="{Binding SetStatusCommand}" CommandParameter="Maintenance"
                    BackgroundColor="{StaticResource StatusMaintenance}" 
                    HeightRequest="60" CornerRadius="10" FontAttributes="Bold" />
        </Grid>

        <!-- Bouton Démarrer / Arrêter le Scan -->
        <Button Grid.Row="2" Text="DÉMARRER SCAN RFID" 
                IsVisible="{Binding IsScanning, Converter={StaticResource InverseBool}}"
                Command="{Binding StartScanCommand}"
                Style="{StaticResource PrimaryButton}" 
                VerticalOptions="End" />

        <Button Grid.Row="2" Text="ARRÊTER SCAN" 
                IsVisible="{Binding IsScanning}"
                Command="{Binding StopScanCommand}"
                BackgroundColor="#C0392B"
                Style="{StaticResource PrimaryButton}" 
                VerticalOptions="End" />

    </Grid>
</ContentPage>
```

### Points Importants du XAML

- **`x:DataType="vm:ScanViewModel"`** : Active la compilation vérifiée du Data Binding. Si on tape `{Binding XXX}` et que `XXX` n'existe pas dans le ViewModel, le compilateur lève une erreur immédiatement (au lieu d'un bug silencieux au runtime).
- **`{Binding StartScanCommand}`** : Lie le bouton à la commande `[RelayCommand] StartScan()` du ViewModel.
- **`{Binding FoundMachine.Status, Converter={StaticResource StatusToBadge}}`** : Utilise un Converter pour transformer le texte `"Installed"` en couleur verte pour le badge visuel.

---

## 11 — ÉTAPE 8 : Le Code-Behind et le Cycle de Vie (ScanPage.xaml.cs)

### Fichier : `Views/ScanPage.xaml.cs`

```csharp
using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

// 🎓 Pédagogie PFE : Écouteur d'Événements du Cycle de Vie
// Les applications mobiles tournent sur batterie. Il est impératif de couper
// les composants matériels (Bluetooth, RFID Zebra, GPS) quand l'utilisateur ne regarde plus la vue.
// `OnDisappearing()` est déclenché par le système d'exploitation quand on change d'onglet.
public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _viewModel;

    public ScanPage(ScanViewModel viewModel)
    {
        InitializeComponent();
        // 🎓 LE BRANCHEMENT CAPITAL : lier le cerveau (ViewModel) à l'écran (View)
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    // 🎓 Très important : on stoppe le lecteur RFID quand on quitte la page
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();  // → appelle _rfid.StopListening()
    }
}
```

### Pourquoi `OnDisappearing()` est Vital

Sur un PDA industriel Zebra alimenté par batterie, laisser le récepteur RFID actif en arrière-plan :
1. **Consomme la batterie** inutilement (l'antenne UHF est énergivore).
2. **Provoque des scans fantômes** : si le technicien est sur la page Dashboard mais que l'antenne capte un tag, cela déclenche un traitement non désiré.
3. **Cause des fuites mémoire** : les événements non désabonnés maintiennent des références en mémoire.

---

## 12 — ÉTAPE 9 : Les Modèles de Données (Machine & ScanEvent)

### Fichier : `Models/Machine.cs`

```csharp
using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

[Table("machines")]
public class Machine : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("tag_id")]
    public string TagId { get; set; } = string.Empty;  // ← Le code EPC du tag RFID !

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("department")]
    public string Department { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "Installed";

    [Column("installation_date")]
    public DateTime InstallationDate { get; set; } = DateTime.Now;

    [Column("exit_date")]
    public DateTime? ExitDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // 🎓 Propriétés calculées pour l'affichage XAML
    public string StatusDisplay => Status switch
    {
        "Installed"   => "✅ Installé",
        "Removed"     => "❌ Retiré",
        "Maintenance" => "🔧 Maintenance",
        _             => Status
    };

    public string InstallationDateDisplay =>
        InstallationDate != default
            ? InstallationDate.ToString("dd/MM/yyyy")
            : "—";
}
```

> 📝 Le champ `TagId` est la **clé de liaison** entre le matériel et la base de données. Quand le Zebra scanne un tag et obtient l'EPC `"A1B2C3D4"`, le `ScanViewModel` cherche dans Supabase la machine dont le `tag_id` correspond. C'est le cœur du système de traçabilité.

### Fichier : `Models/ScanEvent.cs`

```csharp
using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

// 🎓 Pédagogie PFE : Modèle "ScanEvent" (Événement de Scan RFID)
// Chaque fois qu'un technicien passe un lecteur RFID devant une machine,
// un "ScanEvent" est créé et enregistré dans Supabase. C'est le journal
// de traçabilité complet qui permet de savoir QUI a scanné QUOI et QUAND.
[Table("scan_events")]
public class ScanEvent : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("tag_id")]
    public string TagId { get; set; } = string.Empty;

    [Column("machine_id")]
    public int MachineId { get; set; }

    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = "Scan";

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("notes")]
    public string? Notes { get; set; }

    public string EventIcon => EventType switch
    {
        "Install"     => "📥",
        "Remove"      => "📤",
        "Maintenance" => "🔧",
        _             => "📡",
    };

    public string TimestampDisplay =>
        Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
```

---

## 13 — ÉTAPE 10 : Configuration Android Manifest

### Fichier : `Platforms/Android/AndroidManifest.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application android:allowBackup="true" 
                 android:icon="@mipmap/appicon" 
                 android:roundIcon="@mipmap/appicon_round" 
                 android:supportsRtl="true">
    </application>
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
    <uses-permission android:name="android.permission.CAMERA" />
</manifest>
```

> 📝 **Note** : Le `BroadcastReceiver` n'a pas besoin d'être déclaré manuellement dans ce fichier. L'attribut `[BroadcastReceiver]` dans le code C# fait que le compilateur .NET MAUI génère automatiquement l'entrée XML correspondante dans le manifest final compilé.

---

## 14 — Résumé du Flux Complet

### De la Gâchette à la Base de Données — En 8 Étapes

```
 ÉTAPE    QUI                          FAIT QUOI
 ─────    ───                          ─────────
  ①       Technicien                   Appuie sur la gâchette jaune du MC3300x
  
  ②       Antenne RFID UHF            Détecte le tag collé sur la machine industrielle
  
  ③       DataWedge (App Système)     Décode le signal radio en texte EPC (ex: "A1B2C3D4")
                                       et diffuse un Broadcast Intent Android
  
  ④       DataWedgeIntentReceiver.cs   Capte l'Intent, extrait l'EPC via GetStringExtra()
                                       et déclenche l'événement C# TagReceived
  
  ⑤       RfidService.cs              Reçoit l'EPC, le normalise (.Trim().ToUpperInvariant())
                                       et déclenche l'événement TagScanned
  
  ⑥       ScanViewModel.cs            Reçoit l'EPC via OnTagScanned()
                                       → appelle Supabase pour chercher la machine
                                       → enregistre un ScanEvent (traçabilité)
                                       → met à jour les ObservableProperties
  
  ⑦       Moteur Data Binding MAUI    Détecte que les propriétés ont changé
                                       → met à jour automatiquement le XAML
  
  ⑧       ScanPage.xaml               Affiche la carte de la machine trouvée
                                       avec le nom, le département, le statut
                                       et les boutons INSTALLER / RETIRER / MAINT.
```

### Temps Total Estimé

| Étape              | Temps         |
|--------------------|---------------|
| Gâchette → EPC     | ~200 ms       |
| Intent → C#        | ~5 ms         |
| Requête Supabase   | ~300-800 ms   |
| Affichage XAML     | ~10 ms        |
| **TOTAL**          | **~0.5 - 1 seconde** |

---

## 15 — Déploiement sur le Zebra (Méthode Recommandée PFE - Câble USB)

> 💡 **Information Pédagogique** : L'application n'est pas destinée à tourner sur une tablette externe connectée par Bluetooth. **Le Zebra MC3300x est un terminal "Tout-en-un"**. L'application LeoniRFID doit être installée **directement sur l'écran Android du pistolet**.

Pour un environnement de Projet de Fin d'Études (développement et soutenance), la méthode de déploiement par câble USB est la plus fiable et professionnelle :

1. **Activer le Mode Développeur sur le Zebra**
   - Allez dans *Paramètres (Settings) > À propos du téléphone (About phone)*.
   - Touchez 7 fois la ligne *Numéro de build (Build number)*.
   - Allez dans *Système > Developer options* et activez **USB debugging**.

2. **Brancher au PC**
   - Connectez le Zebra à votre ordinateur via un câble USB-C.
   - Sur le Zebra, acceptez l'autorisation *"Allow USB debugging?"*.

3. **Déployer depuis Visual Studio (Le "Play" magique)**
   - Ouvrez la solution `LeoniRFID.sln`.
   - Dans le menu de démarrage (en haut au centre), cliquez sur la petite flèche à côté de "Android Emulator".
   - Sous la catégorie **Périphériques Locaux Android**, sélectionnez votre Zebra (ex: `Zebra Technologies MC3300x`).
   - Appuyez sur **Démarrer (F5)**.
   
Visual Studio va compiler l'application, l'envoyer par le câble et l'ouvrir automatiquement sur l'écran du pistolet. Vous bénéficiez ainsi d'un déploiement sans problème de réseau, d'une recharge de l'appareil simultanée, et d'une capture en temps réel des erreurs éventuelles dans la console Visual Studio.

---

## 16 — Hébergement sur VPS Local (Sécurité Industrielle LEONI)

> 💡 **Information Pédagogique** : Les données de production LEONI sont extrêmement confidentielles (machines, traçabilité, identifiants des techniciens). Elles ne doivent en aucun cas fuiter sur le cloud public.

C'est là tout l'intérêt de la pile technologique choisie : **Supabase est 100% Open-Source !**
Actuellement, pour le cadre du PFE, l'application pointe vers le cloud public gratuit de Supabase (`.supabase.co`). Mais pour le déploiement en usine (Production), LEONI peut héberger sa propre base de données.

### Ce qui change côté code : UNE SEULE LIGNE
Le principe fondamental de notre architecture (Separation of Concerns) fait que l'URI de la base de données est centralisée. Il suffit de modifier le fichier `Helpers/Constants.cs` :

```csharp
// Version PFE (Cloud Public)
public const string SupabaseUrl = "https://xxxxx.supabase.co";

// Version PRODUCTION LEONI (VPS Interne / Datacenter Usine)
public const string SupabaseUrl = "http://192.168.1.100:8000"; // IP locale de l'usine
```
**AUCUN autre fichier du projet ne change.** Le `ScanViewModel` et l'application MAUI ne font aucune différence entre le cloud ou le local !

### Schéma Réseau de l'Usine (Air-Gapped)

```text
┌─────────────────────────── RÉSEAU INTERNE LEONI (Wi-Fi usine) ──────────────────────┐
│                                                                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                                │
│  │ Zebra n°1    │  │ Zebra n°2    │  │ Zebra n°20   │                                │
│  │ (App MAUI)   │  │ (App MAUI)   │  │ (App MAUI)   │                                │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘                                │
│         │ HTTP API        │ HTTP API        │ HTTP API                                │
│         │ (Wi-Fi usine)   │                 │                                         │
│         └─────────────────┼─────────────────┘                                         │
│                           ▼                                                           │
│                  ┌─────────────────────┐                                              │
│                  │  Serveur VPS LEONI  │                                              │
│                  │  (Supabase Docker)  │                                              │
│                  │  PostgreSQL + API   │                                              │
│                  │  192.168.1.100:8000 │                                              │
│                  └─────────────────────┘                                              │
│                                                                                       │
│              ❌ AUCUNE DONNÉE NE SORT VERS INTERNET ❌                                │
└───────────────────────────────────────────────────────────────────────────────────────┘
```
L'équipe informatique de l'usine déploie Supabase via `Docker` sur leurs serveurs. Le lecteur Zebra scanne le code-barres (localement via Intent), puis l'app MAUI envoie l'information en Wi-Fi au VPS interne.

---

## 17 — Dépannage et FAQ

### ❓ Le scan ne détecte rien quand j'appuie sur la gâchette

**Vérifiez la configuration DataWedge :**
1. Ouvrez DataWedge → votre profil `LeoniRFID`.
2. Vérifiez que l'application `com.leoni.rfid.production` est bien associée.
3. Vérifiez que l'Intent Output est activé avec l'action `com.symbol.datawedge.api.ACTION`.
4. Vérifiez que le Delivery est bien sur `Broadcast intent` (et non `startActivity`).

### ❓ Le scan fonctionne dans le Bloc-Notes mais pas dans notre app

C'est l'inverse du problème : DataWedge est en mode **Keystroke Output** (clavier) au lieu de **Intent Output**. Désactivez Keystroke et activez Intent.

### ❓ Comment tester sur un émulateur Android (sans PDA Zebra) ?

Utilisez la méthode `SimulateScan()` du `IRfidService` ou le champ de saisie manuelle EPC intégré dans la `ScanPage`. Tapez un code EPC existant dans votre base Supabase et cliquez "OK".

### ❓ L'EPC scanné ne correspond à aucune machine

Vérifiez dans Supabase que la table `machines` contient bien une ligne avec le `tag_id` correspondant à l'EPC scanné. Attention à la casse : l'application normalise tout en majuscules (`ToUpperInvariant()`), vérifiez que la valeur en base est aussi en majuscules.

### ❓ Le scan se déclenche deux fois

C'est un symptôme de **double abonnement**. Vérifiez que :
1. `StopListening()` est bien appelé dans `OnDisappearing()`.
2. Le `RfidService` est bien enregistré en `AddSingleton` (et non `AddTransient`).

---

> **📝 Ce document constitue la documentation technique complète du module d'intégration matérielle RFID Zebra MC3300x. Il couvre l'ensemble de la chaîne, du matériel physique à la base de données cloud, en passant par toutes les couches logicielles intermédiaires.**
>
> *Encadré par : Oussama Souissi*
