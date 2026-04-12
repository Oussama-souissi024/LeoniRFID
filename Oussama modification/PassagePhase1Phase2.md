# 🚀 Plan de Test et Déploiement en 2 Phases — LeoniRFID

> **Auteur / Encadrant** : Oussama Souissi  
> **Projet** : LeoniRFID (.NET MAUI - Android)  
> **Matériel** : Zebra MC3300x (MC3300U)

---

## 📋 Pourquoi 2 Phases ?

| | Phase 1 — Fondamentaux & Logique | Phase 2 — Intégration Réelle |
|---|---|---|
| **Objectif** | Valider que toute la logique métier fonctionne | Connecter le vrai matériel Zebra |
| **Base de données** | Supabase Cloud (actuelle) | Supabase Local (VPS LEONI) |
| **Données** | Mock / données de test fictives | Données réelles des machines LEONI |
| **Matériel** | Smartphone/Émulateur + saisie manuelle EPC | Pistolet Zebra MC3300x physique |
| **Scan RFID** | Simulé via le champ "Saisie manuelle EPC" | Gâchette physique du Zebra |
| **Réseau** | Internet (cloud Supabase) | Wi-Fi interne usine (VPS local) |

```text
  PHASE 1                                    PHASE 2
  (Logique & Fondamentaux)                   (Intégration Réelle)
                                            
  ┌──────────────┐                           ┌──────────────┐
  │ Smartphone   │                           │ Pistolet     │
  │ ou Émulateur │                           │ Zebra MC3300x│
  │              │                           │              │
  │ Saisie       │                           │ Gâchette     │
  │ Manuelle EPC │                           │ Physique RFID│
  └──────┬───────┘                           └──────┬───────┘
         │                                          │
         │ Internet                                 │ Wi-Fi Usine
         ▼                                          ▼
  ┌──────────────┐                           ┌──────────────┐
  │ Supabase     │                           │ Supabase     │
  │ CLOUD        │                           │ LOCAL (VPS)  │
  │ (Gratuit)    │                           │ (LEONI)      │
  └──────────────┘                           └──────────────┘
```

---

# 📘 PHASE 1 — Fondamentaux & Logique

> **But** : Prouver que l'application fonctionne de bout en bout (Login → Scan → Recherche machine → Changement de statut → Traçabilité) **sans avoir besoin du pistolet Zebra physique**.

---

## Étape 1.1 — Préparer les Données de Test dans Supabase Cloud

Connectez-vous au **Dashboard Supabase** :  
🔗 `https://supabase.com/dashboard` → Projet `LeoniRFID`

### A) Insérer des Machines de Test dans la table `machines`

Allez dans **Table Editor → machines** et ajoutez les lignes suivantes :

| id | tag_id | name | department | status | installation_date | notes |
|----|--------|------|------------|--------|-------------------|-------|
| 1 | `E200001234567890` | Machine Soudure Alpha | LTN1 | Installed | 2025-01-15 | Machine de test Phase 1 |
| 2 | `E200009876543210` | Presse Hydraulique Beta | LTN2 | Maintenance | 2025-03-20 | En maintenance préventive |
| 3 | `E20000AABBCCDDEE` | Robot Assemblage Gamma | LTN3 | Removed | 2024-11-01 | Retirée pour remplacement |
| 4 | `E20000FFEEDDCCBB` | Convoyeur Delta | LTN1 | Installed | 2025-06-10 | Ligne de production 4 |

> 📝 **Astuce** : Les `tag_id` ci-dessus sont des codes EPC fictifs mais réalistes (format hexadécimal en majuscules, préfixe E200 typique des tags RFID UHF). Vous pourrez les taper dans le champ "Saisie manuelle EPC" de l'application.

**Commande SQL alternative** (via l'éditeur SQL de Supabase) :

```sql
INSERT INTO machines (tag_id, name, department, status, installation_date, notes)
VALUES
  ('E200001234567890', 'Machine Soudure Alpha',   'LTN1', 'Installed',   '2025-01-15', 'Machine de test Phase 1'),
  ('E200009876543210', 'Presse Hydraulique Beta',  'LTN2', 'Maintenance', '2025-03-20', 'En maintenance préventive'),
  ('E20000AABBCCDDEE', 'Robot Assemblage Gamma',   'LTN3', 'Removed',     '2024-11-01', 'Retirée pour remplacement'),
  ('E20000FFEEDDCCBB', 'Convoyeur Delta',          'LTN1', 'Installed',   '2025-06-10', 'Ligne de production 4');
```

### B) Créer un Compte Technicien de Test

Via **Supabase Auth** ou la table `profiles`, assurez-vous d'avoir un utilisateur avec :
- **Email** : `technicien.test@leoni.com`
- **Mot de passe** : `Test1234!`
- **Role** : `Technician`

---

## Étape 1.2 — Lancer l'Application

### Option A : Sur un Smartphone Android Réel (Recommandé)
1. Branchez votre téléphone Android personnel en USB.
2. Dans Visual Studio, sélectionnez votre téléphone dans la liste des périphériques.
3. Appuyez sur **F5** (Démarrer).

### Option B : Sur l'Émulateur Android
1. Dans Visual Studio, sélectionnez un émulateur Android (ex: `Pixel 5 - API 34`).
2. Appuyez sur **F5** (Démarrer).

> ⚠️ L'émulateur ne possède pas de lecteur RFID. C'est normal ! C'est justement pour cela qu'on utilise le champ **"Saisie manuelle EPC"** pendant la Phase 1.

---

## Étape 1.3 — Se Connecter à l'Application

1. L'application s'ouvre sur la page de **Login**.
2. Entrez les identifiants du technicien de test :
   - Email : `technicien.test@leoni.com`
   - Mot de passe : `Test1234!`
3. Cliquez sur **Connexion**.
4. Vous arrivez sur le **Dashboard**.

---

## Étape 1.4 — Tester le Module de Scan RFID (Le Test Principal !)

C'est ici que vous simulez ce que le Zebra ferait automatiquement :

1. Naviguez vers la page **"Scanner RFID"** (via le menu latéral ☰).
2. Vous voyez l'écran avec le message **"Approchez un tag RFID..."** et le champ **"Saisie manuelle EPC..."**.

### Test 1 : Scanner une Machine Existante ✅
1. Dans le champ **"Saisie manuelle EPC..."**, tapez : `E200001234567890`
2. Cliquez sur le bouton **"OK"**.
3. **Résultat attendu** :
   - Le message passe à **"✅ Machine trouvée : Machine Soudure Alpha"**
   - Une carte apparaît avec le nom, le département (`LTN1`), et le statut (`✅ Installé`)
   - Les 3 boutons d'action apparaissent en bas : **INSTALLER** | **RETIRER** | **MAINT.**

### Test 2 : Scanner un Tag Inconnu ❌
1. Tapez un EPC qui n'existe pas dans Supabase : `AAAA1111BBBB2222`
2. Cliquez sur **"OK"**.
3. **Résultat attendu** :
   - Le message affiche **"⚠️ Tag inconnu — non enregistré dans la base."**
   - Un grand symbole ⚠️ apparaît avec le message "Tag non reconnu"

### Test 3 : Changer le Statut d'une Machine 🔄
1. Re-scannez `E200001234567890` (Machine Soudure Alpha).
2. Cliquez sur le bouton **"RETIRER"**.
3. Une boîte de dialogue apparaît : *"Confirmer : Retirer la machine Machine Soudure Alpha ?"*
4. Cliquez **"Oui"**.
5. **Résultat attendu** :
   - Le statut de la machine passe à **"❌ Retiré"**
   - Dans Supabase, la colonne `status` de cette machine est maintenant `Removed`
   - La colonne `exit_date` est maintenant remplie avec la date du jour

### Test 4 : Vérifier la Traçabilité (ScanEvent) 📋
1. Retournez dans le **Dashboard Supabase** (navigateur web).
2. Allez dans **Table Editor → scan_events**.
3. **Résultat attendu** : Vous devez voir des nouvelles lignes :

| tag_id | machine_id | event_type | timestamp |
|--------|------------|------------|-----------|
| E200001234567890 | 1 | Scan | (date/heure du test) |
| E200001234567890 | 1 | Removed | (date/heure du test) |

> 🎉 Si ces 4 tests passent, **toute la logique métier de l'application est validée !** Le passage en Phase 2 ne changera que la source de données (cloud → local) et le mode de scan (manuel → physique).

---

## Étape 1.5 — Checklist de Validation Phase 1

| # | Test | Statut |
|---|------|--------|
| 1 | Login avec identifiants de test | ⬜ |
| 2 | Navigation vers la page Scanner RFID | ⬜ |
| 3 | Saisie manuelle d'un EPC existant → machine trouvée | ⬜ |
| 4 | Saisie d'un EPC inconnu → message "Tag non reconnu" | ⬜ |
| 5 | Changement de statut (Installer/Retirer/Maintenance) | ⬜ |
| 6 | Vérification du ScanEvent dans Supabase | ⬜ |
| 7 | Page Dashboard affiche les statistiques à jour | ⬜ |
| 8 | Navigation "Voir détails complets" fonctionne | ⬜ |

> ✅ **Critère de passage en Phase 2** : Tous les tests ci-dessus doivent afficher ✅.

---

---

# 🏭 PHASE 2 — Intégration Réelle (VPS Local + Zebra Physique)

> **But** : Migrer vers l'infrastructure sécurisée de LEONI et connecter le vrai matériel industriel.

---

## Étape 2.1 — Installer Supabase en Local sur le VPS LEONI

### Prérequis sur le Serveur VPS
- **OS** : Linux (Ubuntu 22.04+ recommandé)
- **Docker** et **Docker Compose** installés
- **RAM** : Minimum 4 GB
- **Disque** : Minimum 20 GB

### Installation

```bash
# 1. Cloner le dépôt officiel Supabase
git clone --depth 1 https://github.com/supabase/supabase
cd supabase/docker

# 2. Copier le fichier de configuration
cp .env.example .env

# 3. Modifier les variables critiques dans .env
#    (Utilisez un éditeur comme nano ou vim)
nano .env
```

### Variables à Modifier dans `.env`

```bash
# Mot de passe PostgreSQL (CHANGER OBLIGATOIREMENT !)
POSTGRES_PASSWORD=VotreMotDePasseForUltraSecurise2025!

# URL publique du service (IP du VPS sur le réseau LEONI)
API_EXTERNAL_URL=http://192.168.1.100:8000

# Clés JWT (générez des nouvelles avec : openssl rand -base64 32)
JWT_SECRET=VotreCleJWTSecreteSuperLongue
ANON_KEY=votre_clé_anon_générée
SERVICE_ROLE_KEY=votre_clé_service_générée

# Port d'écoute
KONG_HTTP_PORT=8000
```

### Lancer Supabase

```bash
# 4. Démarrer tous les services
docker compose up -d

# 5. Vérifier que tout tourne
docker compose ps
```

> ✅ Si tout est vert, Supabase est accessible à `http://192.168.1.100:8000`  
> Le Dashboard d'administration est sur `http://192.168.1.100:3000`

---

## Étape 2.2 — Recréer le Schéma de Base de Données

Dans le **Dashboard Supabase local** (`http://192.168.1.100:3000`), ouvrez l'éditeur SQL et exécutez :

```sql
-- ══════════════════════════════════════════════
-- TABLE : machines (Parc machines LEONI)
-- ══════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS machines (
    id                SERIAL PRIMARY KEY,
    tag_id            VARCHAR(64) NOT NULL UNIQUE,  -- Code EPC du tag RFID
    name              VARCHAR(255) NOT NULL,
    department        VARCHAR(50) NOT NULL,
    status            VARCHAR(50) NOT NULL DEFAULT 'Running', -- Nouveaux statuts: Running, Broken, InMaintenance, Removed
    installation_date TIMESTAMP DEFAULT NOW(),
    exit_date         TIMESTAMP NULL,
    notes             TEXT NULL,
    last_updated      TIMESTAMP DEFAULT NOW()
);

-- ══════════════════════════════════════════════
-- TABLE : maintenance_sessions (Logique de Maintenance)
-- ══════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS maintenance_sessions (
    id              SERIAL PRIMARY KEY,
    machine_id      INTEGER NOT NULL REFERENCES machines(id),
    technician_id   VARCHAR(255),
    started_at      TIMESTAMP NOT NULL DEFAULT NOW(),
    ended_at        TIMESTAMP NULL,
    duration_minutes DOUBLE PRECISION NULL,
    notes           TEXT NULL
);

-- ══════════════════════════════════════════════
-- TABLE : scan_events (Journal de traçabilité)
-- ══════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS scan_events (
    id          SERIAL PRIMARY KEY,
    tag_id      VARCHAR(64) NOT NULL,
    machine_id  INTEGER REFERENCES machines(id),
    user_id     VARCHAR(255) NULL,
    event_type  VARCHAR(50) NOT NULL DEFAULT 'Scan',
    timestamp   TIMESTAMP DEFAULT NOW(),
    notes       TEXT NULL
);

-- ══════════════════════════════════════════════
-- TABLE : profiles (Utilisateurs / Techniciens)
-- ══════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS profiles (
    id         VARCHAR(255) PRIMARY KEY,
    email      VARCHAR(255) NOT NULL,
    full_name  VARCHAR(255),
    role       VARCHAR(50) DEFAULT 'Technician', -- Rôles supportés: Admin, Technician, Maintenance
    created_at TIMESTAMP DEFAULT NOW()
);

-- Index pour accélérer la recherche par tag_id (utilisé à chaque scan)
CREATE INDEX IF NOT EXISTS idx_machines_tag_id ON machines(tag_id);
CREATE INDEX IF NOT EXISTS idx_scan_events_tag_id ON scan_events(tag_id);
```

---

## Étape 2.3 — Insérer les Données Réelles de Production

Maintenant, insérez les **vraies machines de l'usine LEONI** avec leurs vrais codes EPC RFID :

```sql
-- EXEMPLE : Remplacez par les vraies machines et leurs vrais tag_id !
INSERT INTO machines (tag_id, name, department, status, installation_date, notes)
VALUES
  ('EPC_REEL_MACHINE_001', 'Nom Réel Machine 1', 'LTN1', 'Installed', '2025-01-15', 'Description réelle'),
  ('EPC_REEL_MACHINE_002', 'Nom Réel Machine 2', 'LTN2', 'Installed', '2025-02-20', 'Description réelle');
-- ... Ajoutez toutes les machines de l'usine
```

> ⚠️ **IMPORTANT** : Les `tag_id` doivent correspondre **exactement** à ce que le Zebra lira sur les tags RFID physiques collés aux machines. Scannez d'abord un tag avec le Zebra pour récupérer son EPC exact, puis insérez-le ici.

---

## Étape 2.4 — Modifier l'Application pour Pointer vers le VPS Local

**Un seul fichier à modifier** : `Helpers/Constants.cs`

```csharp
namespace LeoniRFID.Helpers;

public static class Constants
{
    // ── Phase 1 (COMMENTÉ) ──────────────────────────────
    // public const string SupabaseUrl      = "https://slxcwjgargafbvnitact.supabase.co";
    // public const string SupabaseAnonKey  = "sb_publishable_lfFMzw0_...";

    // ── Phase 2 — VPS Local LEONI (ACTIF) ────────────────
    public const string SupabaseUrl         = "http://192.168.1.100:8000";
    public const string SupabaseAnonKey     = "VOTRE_NOUVELLE_CLE_ANON_LOCALE";
    public const string SupabaseServiceRoleKey = "VOTRE_NOUVELLE_CLE_SERVICE_LOCALE";

    // ── Le reste ne change pas ──────────────────────────
    public const string RoleAdmin      = "Admin";
    public const string RoleTechnician = "Technician";

    public const string StatusInstalled   = "Installed";
    public const string StatusRemoved     = "Removed";
    public const string StatusMaintenance = "Maintenance";

    public static readonly string[] Departments = new[] { "LTN1", "LTN2", "LTN3" };

    public const string DataWedgeAction  = "com.symbol.datawedge.api.ACTION";
    public const string DataWedgeEpcData = "com.symbol.datawedge.data_string";
}
```

> 📝 **C'est le SEUL fichier modifié dans toute l'application.** Tout le reste du code (ViewModels, Services, Views) fonctionne exactement de la même manière car il référence `Constants.SupabaseUrl` partout.

---

## Étape 2.5 — Installer l'Application sur le Pistolet Zebra MC3300x

### A) Préparer le Zebra (une seule fois)

1. Sur le Zebra : *Paramètres > À propos du téléphone*
2. Touchez **"Build number" 7 fois** → "You are now a developer!"
3. Allez dans *Système > Developer options*
4. Activez **USB debugging**

### B) Brancher et Déployer

1. Connectez le Zebra au PC via **câble USB-C**.
2. Acceptez l'autorisation USB debugging sur le Zebra.
3. Dans Visual Studio, le Zebra apparaît dans la liste des périphériques :  
   **`Zebra Technologies MC3300x`**
4. Sélectionnez-le et appuyez sur **F5 (Démarrer)**.
5. Visual Studio compile et installe l'application directement sur le pistolet ! 🎉

---

## Étape 2.6 — Configurer DataWedge sur le Zebra

> ⚠️ Cette étape se fait **une seule fois** sur le Zebra. Voir le guide détaillé dans `IntegrationZebraRfid.md` (Section 4).

Résumé rapide :

1. Ouvrir l'app **DataWedge** sur le Zebra.
2. Créer un nouveau profil : **`LeoniRFID`**
3. Associer l'app : `com.leoni.rfid.production` / `*`
4. Configurer la sortie :

```text
├── Keystroke Output: ❌ Disabled
└── Intent Output:    ✅ Enabled
    ├── Action:   com.symbol.datawedge.api.ACTION
    ├── Category: android.intent.category.DEFAULT
    └── Delivery: Broadcast intent
```

---

## Étape 2.7 — Connecter le Zebra au Wi-Fi de l'Usine

1. Sur le Zebra : *Paramètres > Wi-Fi*
2. Connectez-vous au réseau interne de l'usine LEONI.
3. **Vérification** : Le Zebra doit pouvoir "pinger" le VPS local.
   - Ouvrez un navigateur sur le Zebra.
   - Allez à `http://192.168.1.100:8000` — vous devez voir une réponse JSON.

> ⚠️ Si le Zebra ne voit pas le VPS, demandez à l'équipe IT de LEONI de vérifier les règles du pare-feu (firewall) pour autoriser le port `8000`.

---

## Étape 2.8 — Test Final : Le Grand Moment ! 🎯

C'est le test ultime avec le vrai matériel et les vraies données :

1. Ouvrez l'application **LeoniRFID** sur l'écran du pistolet Zebra.
2. Connectez-vous avec un compte technicien réel.
3. Naviguez vers **"Scanner RFID"**.
4. Appuyez sur le bouton **"DÉMARRER SCAN RFID"**.
5. Pointez le Zebra vers une machine ayant un **tag RFID collé**.
6. **Appuyez sur la gâchette jaune** du pistolet. 🔫

### Résultat Attendu (en moins de 1 seconde) :
```text
Gâchette → Antenne RFID → DataWedge → Intent → BroadcastReceiver
→ RfidService → ScanViewModel → Supabase Local → Carte Machine affichée !
```

- L'écran du Zebra affiche la carte de la machine avec son nom et son statut.
- Le technicien peut alors cliquer **INSTALLER**, **RETIRER** ou **MAINT.** pour mettre à jour le statut.
- Un `ScanEvent` est enregistré dans la base de données locale pour la traçabilité.

---

## Étape 2.9 — Checklist de Validation Phase 2

| # | Test | Statut |
|---|------|--------|
| 1 | Supabase local tourne sur le VPS (Docker) | ⬜ |
| 2 | Tables `machines`, `scan_events`, `profiles` créées | ⬜ |
| 3 | Données réelles insérées dans Supabase local | ⬜ |
| 4 | `Constants.cs` pointe vers l'IP du VPS local | ⬜ |
| 5 | Application installée sur le Zebra via USB | ⬜ |
| 6 | DataWedge configuré avec le profil LeoniRFID | ⬜ |
| 7 | Zebra connecté au Wi-Fi interne de l'usine | ⬜ |
| 8 | Scan physique avec gâchette → machine trouvée | ⬜ |
| 9 | Changement de statut depuis le Zebra fonctionnel | ⬜ |
| 10 | ScanEvent enregistré dans Supabase local | ⬜ |

> ✅ **Si tous les tests sont ✅, le projet est en PRODUCTION !** 🎉

---

## 📊 Résumé des Différences entre les 2 Phases

| Élément | Phase 1 | Phase 2 |
|---------|---------|---------|
| **Fichier modifié** | Aucun | `Constants.cs` uniquement |
| **Base de données** | `https://slxcwjgargafbvnitact.supabase.co` | `http://192.168.1.100:8000` |
| **Données** | Mock (E200001234567890...) | Vrais EPC des machines |
| **Appareil** | Smartphone personnel / Émulateur | Zebra MC3300x |
| **Mode de scan** | Champ "Saisie manuelle EPC" | Gâchette physique RFID |
| **Réseau** | Internet public | Wi-Fi interne LEONI |
| **Confidentialité** | ⚠️ Données sur serveurs tiers | ✅ Données 100% internes |
| **Code C# modifié** | 0 ligne | 3 lignes (URL + clés) |

---

> **📝 Ce document constitue le plan opérationnel complet du PFE LeoniRFID, de la phase de développement logique jusqu'au déploiement en production industrielle.**
>
> *Encadré par : Oussama Souissi*
