# 📡 Zebra MC3300xR — Fiche Technique & Guide d'Intégration LeoniRFID

> **Document technique pour déploiement en production**  
> **Application** : LeoniRFID (.NET MAUI - Android)  
> **Auteur** : Oussama Souissi  
> **Révision** : Avril 2026

---

## 1. 📋 Fiche Technique du Zebra MC3300xR

### 1.1 Identification de l'appareil

| Caractéristique | Détail |
|---|---|
| **Modèle** | Zebra MC3300xR (série RFID) |
| **Type** | PDA industriel durci avec lecteur RFID UHF intégré |
| **Usage** | Inventaire, traçabilité d'équipements, gestion d'actifs industriels |
| **Fabricant** | Zebra Technologies Corporation |
| **Destiné à** | LEONI Wiring Systems — Suivi machines usine |

### 1.2 Spécifications Matérielles

| Composant | Spécification |
|---|---|
| **Processeur** | Qualcomm Snapdragon™ 660 — Octa-core 2.2 GHz |
| **Mémoire RAM** | 4 Go |
| **Stockage Flash** | 32 Go |
| **Écran** | 4.0" WVGA (800×480), tactile capacitif, lisible en plein soleil |
| **Batterie** | 7 000 mAh (autonomie journée complète en usine) |
| **Poids** | ~490g avec batterie |

### 1.3 Connectivité

| Interface | Détail |
|---|---|
| **Wi-Fi** | 802.11 a/b/g/n/ac/d/h/i/r/k/w (2.4 GHz + 5 GHz) |
| **Bluetooth** | 5.0 (Classic + BLE) |
| **NFC** | Oui — pour appairage rapide |
| **USB** | USB-C (charge + données) |
| **4G/LTE** | Optionnel (selon modèle) |

### 1.4 Capacités RFID UHF

| Paramètre | Valeur |
|---|---|
| **Technologie** | UHF RFID — EPC Gen2v2 / ISO 18000-63 |
| **Fréquence** | 865–928 MHz (bande mondiale) |
| **Vitesse de lecture** | 900+ tags/seconde |
| **Portée de lecture** | Jusqu'à 6m (MC3330xR) / jusqu'à 20m (MC3390xR) |
| **Antenne** | Intégrée, polarisation circulaire |
| **Puissance TX** | Configurable (5–30 dBm) |

### 1.5 Résistance Industrielle (Conformité Usine LEONI)

| Critère | Norme |
|---|---|
| **Étanchéité** | IP64 (protection poussière + éclaboussures) |
| **Chute** | Résiste à des chutes de 1.8m (6 pieds) sur béton |
| **Température** | Fonctionne de -20°C à +50°C |
| **Vibrations** | MIL-STD-810H |
| **Nettoyage** | Compatible produits désinfectants industriels |

### 1.6 Système d'Exploitation

| Paramètre | Valeur |
|---|---|
| **OS** | Android 10/11/14 (selon BSP) |
| **Zebra Mobility DNA** | Suite d'outils entreprise préinstallés |
| **DataWedge** | Préinstallé — capture de données sans SDK |
| **Mises à jour** | LifeGuard™ pour mises à jour de sécurité OTA |

---

## 2. 🏗️ Architecture d'Intégration avec LeoniRFID

### 2.1 Approche : DataWedge (Zero-SDK)

L'application LeoniRFID utilise l'approche **DataWedge** plutôt qu'un SDK RFID natif.

**Pourquoi DataWedge ?**
- ✅ **Aucun SDK tiers** à installer dans le projet
- ✅ **Zero dépendance matérielle** dans le code — l'APK fonctionne sur tout Android
- ✅ **Configuration côté terminal** — pas besoin de recompiler pour changer les paramètres RFID
- ✅ **Maintenance simplifiée** — Zebra met à jour DataWedge indépendamment de l'app
- ✅ **Compatible barcode + RFID** avec le même mécanisme

### 2.2 Schéma du Flux de Données

```
┌─────────────────────────────────────────────────────┐
│                  TERMINAL ZEBRA MC3300xR             │
│                                                     │
│  1. L'opérateur appuie sur la gâchette RFID         │
│     ↓                                               │
│  2. Le lecteur UHF capture le tag EPC               │
│     ↓                                               │
│  3. DataWedge (pré-installé) reçoit l'EPC           │
│     ↓                                               │
│  4. DataWedge envoie un Broadcast Intent Android     │
│     Action = "com.leoni.rfid.SCAN"                  │
│     Extra  = "com.symbol.datawedge.data_string"     │
│     ↓                                               │
│  5. BroadcastReceiver de LeoniRFID capte l'Intent   │
│     ↓                                               │
│  6. RfidService propage l'EPC au ViewModel          │
│     ↓                                               │
│  7. ScanViewModel interroge Supabase                │
│     ↓                                               │
│  8. Affichage machine + actions selon rôle           │
└─────────────────────────────────────────────────────┘
```

### 2.3 Composants Logiciels Impliqués

```
LeoniRFID/
├── Platforms/Android/
│   ├── DataWedgeIntentReceiver.cs  ← Capte les Intents DataWedge
│   ├── MainActivity.cs            ← Point d'entrée Android
│   └── AndroidManifest.xml        ← Permissions
├── Services/
│   ├── IRfidService.cs            ← Interface (abstraction)
│   └── RfidService.cs             ← Pont entre Receiver et ViewModel
├── ViewModels/
│   └── ScanViewModel.cs           ← Logique métier du scan
└── Views/
    └── ScanPage.xaml               ← Interface utilisateur
```

---

## 3. ⚙️ Configuration DataWedge sur le Terminal Zebra

> **⚠️ CRITIQUE** : Sans cette configuration, l'APK ne recevra aucun scan RFID.

### 3.1 Étapes de Configuration (à faire UNE SEULE FOIS par terminal)

#### Étape 1 : Ouvrir DataWedge
- Sur le Zebra, allez dans **Paramètres → DataWedge** (ou cherchez "DataWedge" dans les applications)

#### Étape 2 : Créer un nouveau profil
1. Appuyez sur le menu **☰ → Nouveau profil**
2. Nommez-le : **`LeoniRFID`**
3. Appuyez sur le profil pour l'ouvrir

#### Étape 3 : Associer l'application
1. Dans **Applications associées** → **☰ → Nouveau**
2. Sélectionnez : **`com.companyname.leonirfid`**
3. Activity : **`*`** (toutes les activités)

#### Étape 4 : Configurer l'entrée RFID
1. **RFID Input** → Activer ✅
2. Paramètres recommandés :
   - **Hardware Trigger** : Activer ✅ (gâchette physique)
   - **Tag Read Duration** : 2000 ms
   - **Antenna Transmit Power** : 27 dBm (ajuster selon la distance de lecture)
   - **Session** : Session 1

#### Étape 5 : Configurer la sortie Intent
1. **Intent Output** → Activer ✅
2. **Intent Action** : `com.leoni.rfid.SCAN`
3. **Intent Category** : `android.intent.category.DEFAULT`
4. **Intent Delivery** : `Broadcast Intent`

#### Étape 6 : Désactiver les sorties inutiles
1. **Keystroke Output** → Désactiver ❌
2. **Clipboard Output** → Désactiver ❌

### 3.2 Vérification Rapide

| Paramètre | Valeur Attendue |
|---|---|
| Profil actif | `LeoniRFID` |
| Application | `com.companyname.leonirfid / *` |
| RFID Input | ✅ Activé |
| Intent Output | ✅ Activé |
| Intent Action | `com.leoni.rfid.SCAN` |
| Intent Delivery | `Broadcast Intent` |
| Keystroke Output | ❌ Désactivé |

### 3.3 Test de Validation

1. Installez l'APK LeoniRFID sur le terminal
2. Ouvrez l'application → Page **Scanner RFID**
3. Appuyez sur la **gâchette RFID** du Zebra
4. Le tag EPC doit apparaître automatiquement dans l'application
5. Si la machine est connue : affichage des détails + actions
6. Si le tag est inconnu : formulaire d'enregistrement

---

## 4. 🔧 Code Source — Composants Clés

### 4.1 BroadcastReceiver (`DataWedgeIntentReceiver.cs`)

```csharp
// L'action DOIT matcher celle configurée dans DataWedge
public const string DATAWEDGE_ACTION = "com.leoni.rfid.SCAN";

// Clé standard pour extraire l'EPC/barcode
private const string EXTRA_DATA_STRING = "com.symbol.datawedge.data_string";
```

**Rôle** : Intercepte les Intents Android envoyés par DataWedge et déclenche l'événement `TagReceived`.

### 4.2 Service RFID (`RfidService.cs`)

```csharp
public void StartListening()
{
    IsListening = true;
#if ANDROID
    DataWedgeIntentReceiver.TagReceived += OnTagReceived;
#endif
}
```

**Rôle** : Pont entre le code natif Android et le code .NET MAUI multi-plateforme.

### 4.3 ViewModel (`ScanViewModel.cs`)

Le ViewModel gère le **workflow complet** :

| Étape | Action |
|---|---|
| Tag scanné | `ProcessEpcAsync(epc)` → cherche la machine en base |
| Machine trouvée | Affiche détails + boutons selon rôle |
| Tag inconnu | Affiche formulaire d'enregistrement |
| Technicien + machine Active | Bouton **🔴 Signaler Panne** |
| Maintenance + machine Defect | Bouton **🔧 Commencer Maintenance** |
| Maintenance en cours | Timer ⏱️ + Bouton **✅ Terminée** |

---

## 5. 📦 Déploiement APK — Guide de Production

### 5.1 Générer l'APK

```bash
# Mode Release (optimisé pour la production)
dotnet publish -f net9.0-android -c Release

# L'APK sera dans :
# bin/Release/net9.0-android/publish/com.companyname.leonirfid-Signed.apk
```

### 5.2 Installation sur le Zebra

**Méthode 1 : USB**
```bash
adb install com.companyname.leonirfid-Signed.apk
```

**Méthode 2 : Fichier APK**
1. Copier l'APK sur le stockage du terminal
2. Ouvrir avec le gestionnaire de fichiers
3. Autoriser l'installation depuis "Sources inconnues"

**Méthode 3 : MDM Enterprise (StageNow)**
- Utiliser Zebra StageNow pour déployer sur plusieurs terminaux
- Configurer le profil DataWedge + l'APK en un seul staging barcode

### 5.3 Checklist de Déploiement

| # | Étape | Statut |
|---|---|---|
| 1 | APK signée générée en mode Release | ☐ |
| 2 | APK installée sur le terminal Zebra | ☐ |
| 3 | Profil DataWedge **"LeoniRFID"** créé | ☐ |
| 4 | Application associée : `com.companyname.leonirfid` | ☐ |
| 5 | RFID Input activé | ☐ |
| 6 | Intent Output configuré : `com.leoni.rfid.SCAN` | ☐ |
| 7 | Keystroke Output désactivé | ☐ |
| 8 | Wi-Fi connecté au réseau usine | ☐ |
| 9 | Test scan RFID → machine affichée | ☐ |
| 10 | Test workflow maintenance complet | ☐ |

---

## 6. 🛡️ Troubleshooting — Résolution de Problèmes

### 6.1 L'application ne reçoit pas les scans

| Cause Possible | Solution |
|---|---|
| Profil DataWedge non créé | Créer le profil "LeoniRFID" (voir section 3) |
| Mauvaise Intent Action | Vérifier : `com.leoni.rfid.SCAN` |
| Intent Delivery mauvais | Doit être **Broadcast Intent** (pas Activity) |
| Keystroke Output actif | Le scan va dans le champ texte au lieu de l'Intent |
| Application non associée | Associer `com.companyname.leonirfid / *` |
| DataWedge pas prêt après reboot | Attendre 10-15 secondes après le démarrage |

### 6.2 Le scan RFID est lent ou ne détecte pas

| Cause | Solution |
|---|---|
| Puissance TX trop faible | Augmenter l'Antenna Transmit Power (27-30 dBm) |
| Tags trop loin | Rapprocher le terminal (< 1m pour les petits tags) |
| Interférences métal | Utiliser des tags anti-métal (spéciaux pour machines) |
| Batterie faible | La puissance RF diminue sous 20% de batterie |

### 6.3 Erreurs de connexion Supabase

| Erreur | Solution |
|---|---|
| `Network unreachable` | Vérifier la connexion Wi-Fi usine |
| `401 Unauthorized` | Vérifier les clés API dans `SupabaseConfig` |
| `Timeout` | Le serveur Supabase est peut-être injoignable |

---

## 7. 📊 Résumé du Flux Complet

```
╔══════════════════════════════════════════════════════════════╗
║              FLUX COMPLET : DE LA GÂCHETTE À LA BASE        ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  👆 Opérateur appuie sur la gâchette RFID                   ║
║     │                                                        ║
║     ▼                                                        ║
║  📡 Antenne UHF lit le tag EPC (ex: 079278000000000000E77)   ║
║     │                                                        ║
║     ▼                                                        ║
║  ⚙️ DataWedge envoie Broadcast "com.leoni.rfid.SCAN"        ║
║     │                                                        ║
║     ▼                                                        ║
║  📨 DataWedgeIntentReceiver.OnReceive() capte l'Intent       ║
║     │                                                        ║
║     ▼                                                        ║
║  🔗 RfidService.OnTagReceived() propage au ViewModel         ║
║     │                                                        ║
║     ▼                                                        ║
║  🧠 ScanViewModel.ProcessEpcAsync() interroge Supabase       ║
║     │                                                        ║
║     ├── Machine trouvée → Affiche détails + actions rôle     ║
║     └── Tag inconnu → Formulaire nouvelle machine            ║
║                                                              ║
║  📊 Traçabilité : ScanEvent enregistré dans Supabase         ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

---

## 8. 🔐 Sécurité en Production

| Mesure | Implémentation |
|---|---|
| **Authentification** | Login obligatoire via Supabase Auth |
| **RBAC** | Rôles : Admin, Technician, Maintenance |
| **RLS Supabase** | Politiques Row Level Security par rôle |
| **HTTPS** | Toutes les communications chiffrées (TLS 1.3) |
| **Session** | Token JWT avec expiration automatique |
| **Audit Trail** | Chaque action (scan, changement statut) est tracée |

---

> **Document validé pour le déploiement en production**  
> LEONI Wiring Systems — Système de traçabilité RFID  
> © 2026 Oussama Souissi
