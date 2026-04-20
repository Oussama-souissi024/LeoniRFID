# 🏭 Guide Complet — Passage de Supabase Cloud vers Supabase Locale

> **Public cible** : Étudiante PFE. Ce guide est conçu pour être suivi **pas à pas**, sans connaissances préalables en Docker ou en administration serveur.
>
> **Objectif** : Déployer Supabase en local sur un PC Windows pour valider le flux complet de l'application LeoniRFID avec un terminal Zebra MC3300x, **sans connexion internet**.

---

## 📋 Table des Matières

1. [Prérequis à installer](#étape-1--installer-les-prérequis)
2. [Cloner Supabase Docker](#étape-2--cloner-supabase-docker)
3. [Générer les clés JWT](#étape-3--générer-les-clés-jwt-sécurité)
4. [Configurer le fichier .env](#étape-4--configurer-le-fichier-env)
5. [Lancer Supabase](#étape-5--lancer-supabase-docker)
6. [Créer les tables (schéma SQL)](#étape-6--créer-le-schéma-de-la-base-de-données)
7. [Configurer les politiques RLS](#étape-7--configurer-les-politiques-rls-sécurité)
8. [Insérer les données de test](#étape-8--insérer-les-données-de-test)
9. [Créer les utilisateurs de test](#étape-9--créer-les-utilisateurs-de-test)
10. [Modifier le code de l'application](#étape-10--modifier-le-code-de-lapplication)
11. [Connecter le Zebra MC3300x](#étape-11--connecter-le-zebra-mc3300x)
12. [Tester le flux complet](#étape-12--tester-le-flux-complet)
13. [Dépannage](#-dépannage-problèmes-courants)
14. [Revenir au Cloud](#-comment-revenir-à-supabase-cloud)

---

## Contexte

L'application LeoniRFID utilise actuellement **Supabase Cloud** (`https://slxcwjgargafbvnitact.supabase.co`). LEONI a demandé de valider le flux avec une instance **locale** pour prouver que la solution fonctionne sur le réseau interne de l'usine.

### Ce qui change

| Aspect | Avant (Cloud) | Après (Local) |
|--------|--------------|---------------|
| **URL Supabase** | `https://slxcwjgargafbvnitact.supabase.co` | `http://192.168.1.122:8000` |
| **Clés API** | Fournies par Supabase Cloud | Générées localement (JWT) |
| **Connexion** | Internet obligatoire | Réseau WiFi local uniquement |
| **Protocole** | HTTPS (SSL) | HTTP (pas de SSL en local) |

### Ce qui ne change PAS

> [!TIP]
> **Aucune modification** de `SupabaseService.cs`, des Models, des ViewModels ou des Views n'est nécessaire ! Le SDK Supabase C# fonctionne de manière **identique** avec une instance locale — seules l'URL et les clés changent dans `Constants.cs`.

---

## Environnement Requis

| Composant | Version Minimum | Comment vérifier |
|-----------|----------------|-----------------|
| **Windows** | Windows 10/11 Pro (64-bit) | `winver` |
| **RAM** | 16 GB minimum | Gestionnaire des tâches |
| **Espace disque** | 10 GB libre | Explorateur de fichiers |
| **Docker Desktop** | v4.0+ | `docker --version` |
| **Git** | v2.0+ | `git --version` |
| **Visual Studio** | 2022 v17.8+ | Menu Aide → À propos |

---

## Étape 1 — Installer les Prérequis

### 1.1 Activer WSL 2 (Windows Subsystem for Linux)

Docker Desktop a besoin de WSL 2 pour fonctionner. Ouvre **PowerShell en Administrateur** et exécute :

```powershell
# Activer WSL et la plateforme de machine virtuelle
wsl --install
```

> [!IMPORTANT]
> **Redémarrer le PC** après cette commande. WSL 2 ne sera actif qu'après le redémarrage.

### 1.2 Installer Docker Desktop

1. Télécharger Docker Desktop depuis : [https://www.docker.com/products/docker-desktop/](https://www.docker.com/products/docker-desktop/)
2. Lancer l'installateur `.exe`
3. Cocher **"Use WSL 2 instead of Hyper-V"** pendant l'installation
4. Terminer l'installation et **redémarrer le PC** si demandé
5. Lancer Docker Desktop depuis le menu Démarrer
6. Attendre que l'icône dans la barre des tâches devienne **verte** (Docker Engine is running)

### 1.3 Vérifier que tout fonctionne

Ouvre **PowerShell** (pas en admin) et tape :

```powershell
docker --version
# Résultat attendu : Docker version 29.x.x (ou supérieur)

docker compose version
# Résultat attendu : Docker Compose version v2.x.x (ou supérieur)
```

Si tu vois les versions, **Docker est prêt** ✅

### 1.4 Installer Git (si pas déjà installé)

1. Télécharger depuis : [https://git-scm.com/download/win](https://git-scm.com/download/win)
2. Installer avec les options par défaut
3. Vérifier : `git --version`

---

## Étape 2 — Cloner Supabase Docker

Ouvre **PowerShell** et exécute :

```powershell
# Cloner le dépôt officiel Supabase (uniquement le dernier commit pour gagner du temps)
git clone --depth 1 https://github.com/supabase/supabase C:\supabase-local

# Aller dans le dossier Docker
cd C:\supabase-local\docker

# Copier le fichier d'environnement modèle
Copy-Item .env.example .env
```

> [!NOTE]
> Le dossier `C:\supabase-local\docker\` contient le fichier `docker-compose.yml` qui définit tous les services Supabase (PostgreSQL, GoTrue, PostgREST, Kong, Studio, etc.).

---

## Étape 3 — Générer les Clés JWT (Sécurité)

Supabase utilise des **JSON Web Tokens (JWT)** pour sécuriser les communications. Tu dois générer 3 clés :

| Clé | Rôle |
|-----|------|
| `JWT_SECRET` | Clé secrète qui signe tous les tokens |
| `ANON_KEY` | Token pour les opérations publiques (login, lecture) |
| `SERVICE_ROLE_KEY` | Token administrateur (contourne la sécurité RLS) |

### Option A — Utiliser le script automatique (RECOMMANDÉ)

Un script PowerShell a été préparé dans le projet. Depuis le dossier du projet LeoniRFID :

```powershell
cd "C:\Users\oussa\OneDrive\Desktop\.NET MAUI\LeoniRFID"
.\generate-jwt-keys.ps1
```

Le script va :
1. Générer un `JWT_SECRET` aléatoire de 64 caractères
2. Créer un `ANON_KEY` (JWT signé avec le rôle `anon`, expire dans 10 ans)
3. Créer un `SERVICE_ROLE_KEY` (JWT signé avec le rôle `service_role`, expire dans 10 ans)
4. Sauvegarder le tout dans `C:\supabase-keys.txt`

> [!IMPORTANT]
> **Note bien les 3 clés affichées dans le terminal !** Tu en auras besoin pour l'étape suivante.

### Option B — Utiliser les clés pré-générées

Si le script ne fonctionne pas, utilise ces clés déjà générées (valides jusqu'en 2036) :

```
JWT_SECRET=IiNFs2gjFHvG7I4FPBqBGqSgXAlVmq8zTQlZWMRta4U3fSf7axln98KMU6sfgbpr

ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoiYW5vbiJ9.9Ih9WSOMJxCeTSzYUmOTNYPcsm82suLqON5DeO0qgaI

SERVICE_ROLE_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoic2VydmljZV9yb2xlIn0.IPwblUTo1d53u6Zo48EaOgGqlfxb6xHwyDunhMYVo5E
```

---

## Étape 4 — Configurer le fichier `.env`

### Option A — Script automatique (RECOMMANDÉ)

Un script de configuration a été préparé :

```powershell
cd "C:\Users\oussa\OneDrive\Desktop\.NET MAUI\LeoniRFID"
.\configure-supabase-env.ps1
```

Ce script remplace automatiquement les valeurs dans le fichier `.env` avec les clés pré-générées et l'IP locale `192.168.1.122`.

### Option B — Configuration manuelle

Si tu préfères modifier manuellement, ouvre le fichier `C:\supabase-local\docker\.env` avec un éditeur de texte (VS Code, Notepad++) et remplace ces lignes :

```env
# ── SÉCURITÉ ──
POSTGRES_PASSWORD=LeoniRFID2024Prod
JWT_SECRET=IiNFs2gjFHvG7I4FPBqBGqSgXAlVmq8zTQlZWMRta4U3fSf7axln98KMU6sfgbpr

# ── CLÉS API ──
ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoiYW5vbiJ9.9Ih9WSOMJxCeTSzYUmOTNYPcsm82suLqON5DeO0qgaI
SERVICE_ROLE_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoic2VydmljZV9yb2xlIn0.IPwblUTo1d53u6Zo48EaOgGqlfxb6xHwyDunhMYVo5E

# ── RÉSEAU ──
SITE_URL=http://192.168.1.122:8000
API_EXTERNAL_URL=http://192.168.1.122:8000
SUPABASE_PUBLIC_URL=http://192.168.1.122:8000

# ── DASHBOARD ──
DASHBOARD_PASSWORD=LeoniAdmin2024
```

### Vérifier ton adresse IP locale

> [!WARNING]
> L'adresse IP `192.168.1.122` est celle du PC d'Oussama. **Ton PC peut avoir une IP différente !**

Pour trouver ton IP locale, exécute dans PowerShell :

```powershell
ipconfig | Select-String "IPv4"
```

Si ton IP est différente (ex: `192.168.1.50`), **remplace `192.168.1.122` par ton IP** dans :
1. Le fichier `.env` (les 3 lignes `SITE_URL`, `API_EXTERNAL_URL`, `SUPABASE_PUBLIC_URL`)
2. Le fichier `Constants.cs` (voir Étape 10)

---

## Étape 5 — Lancer Supabase Docker

### 5.1 Télécharger les images Docker

```powershell
cd C:\supabase-local\docker
docker compose pull
```

> [!NOTE]
> Cette commande télécharge ~3-4 GB d'images Docker. Cela peut prendre **10-20 minutes** selon ta connexion internet. Tu n'auras besoin d'internet **que pour cette étape**.

### 5.2 Démarrer tous les services

```powershell
docker compose up -d
```

Le flag `-d` (detached) lance les services en arrière-plan.

### 5.3 Vérifier que tout tourne

```powershell
docker compose ps
```

Tu devrais voir une liste de services avec le statut **"Up"** ou **"running"** :

```
NAME                    STATUS
supabase-auth           Up
supabase-db             Up
supabase-kong           Up
supabase-meta           Up
supabase-realtime       Up
supabase-rest           Up
supabase-storage        Up
supabase-studio         Up
...
```

> [!CAUTION]
> Si un service affiche **"Exited"** ou **"Restarting"**, consulte la section [Dépannage](#-dépannage-problèmes-courants) en bas du document.

### 5.4 Accéder à Supabase Studio

Ouvre ton navigateur et va sur : **http://localhost:8000**

- **Email** : `supabase`
- **Mot de passe** : `LeoniAdmin2024` (celui défini dans le `.env` à l'étape 4)

Tu devrais voir le tableau de bord Supabase Studio. **Supabase locale est opérationnelle !** 🎉

---

## Étape 6 — Créer le schéma de la base de données

### Comment accéder à l'éditeur SQL ?

1. Ouvre **Supabase Studio** dans le navigateur (`http://localhost:8000`)
2. Dans le menu de gauche, clique sur **SQL Editor** (icône `>_`)
3. Clique sur **"New Query"**
4. **Copie-colle** le script SQL complet ci-dessous
5. Clique sur le bouton vert **"Run"**

### Script SQL — Création des 5 tables

```sql
-- ═══════════════════════════════════════════════════════════════
-- SCHÉMA COMPLET LeoniRFID — Supabase Locale
-- Ce script crée les 5 tables + le trigger de création de profil
-- ═══════════════════════════════════════════════════════════════

-- ─── TABLE 1 : Profils utilisateurs ───
-- Liée à la table cachée auth.users (même UUID)
CREATE TABLE public.profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    full_name TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT 'Technician'
        CHECK (role IN ('Admin', 'Technician', 'Maintenance')),
    is_active BOOLEAN NOT NULL DEFAULT true,
    must_change_password BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ─── TABLE 2 : Départements (ateliers de l'usine) ───
CREATE TABLE public.departments (
    id SERIAL PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL DEFAULT '',
    description TEXT
);

-- ─── TABLE 3 : Machines industrielles ───
-- Chaque machine a un tag RFID (code EPC unique)
CREATE TABLE public.machines (
    id SERIAL PRIMARY KEY,
    tag_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL DEFAULT '',
    department TEXT NOT NULL DEFAULT '',
    status TEXT NOT NULL DEFAULT 'Running'
        CHECK (status IN ('Running', 'Broken', 'InMaintenance', 'Paused', 'Removed')),
    installation_date TIMESTAMPTZ NOT NULL DEFAULT now(),
    exit_date TIMESTAMPTZ,
    notes TEXT,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ─── TABLE 4 : Événements de scan RFID (traçabilité) ───
CREATE TABLE public.scan_events (
    id SERIAL PRIMARY KEY,
    tag_id TEXT NOT NULL DEFAULT '',
    machine_id INTEGER REFERENCES public.machines(id),
    user_id UUID REFERENCES auth.users(id),
    event_type TEXT NOT NULL DEFAULT 'Scan',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
    notes TEXT
);

-- ─── TABLE 5 : Sessions de maintenance ───
CREATE TABLE public.maintenance_sessions (
    id SERIAL PRIMARY KEY,
    machine_id INTEGER NOT NULL REFERENCES public.machines(id),
    technician_id UUID REFERENCES auth.users(id),
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ended_at TIMESTAMPTZ,
    duration_minutes DOUBLE PRECISION,
    notes TEXT
);

-- ─── TRIGGER : Créer automatiquement un profil quand un utilisateur s'inscrit ───
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO public.profiles (id, full_name, role, must_change_password)
    VALUES (
        NEW.id,
        COALESCE(NEW.raw_user_meta_data->>'full_name', ''),
        COALESCE(NEW.raw_user_meta_data->>'role', 'Technician'),
        true
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
```

**Résultat attendu** : `Success. No rows returned` — c'est normal et correct ✅

---

## Étape 7 — Configurer les politiques RLS (Sécurité)

Toujours dans le **SQL Editor**, crée une **nouvelle requête** et colle ce script :

```sql
-- ═══════════════════════════════════════════════════════════════
-- POLITIQUES RLS (Row Level Security)
-- Ces règles définissent QUI peut lire/écrire dans chaque table
-- ═══════════════════════════════════════════════════════════════

-- Activer RLS sur toutes les tables
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.machines ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.scan_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.maintenance_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.departments ENABLE ROW LEVEL SECURITY;

-- ─── Profiles ───
CREATE POLICY "Authenticated users can view all profiles"
    ON public.profiles FOR SELECT
    USING (auth.role() = 'authenticated');

CREATE POLICY "Users can update own profile"
    ON public.profiles FOR UPDATE
    USING (auth.uid() = id);

-- ─── Machines ───
CREATE POLICY "Authenticated users can view all machines"
    ON public.machines FOR SELECT
    USING (auth.role() = 'authenticated');

CREATE POLICY "Authenticated users can insert machines"
    ON public.machines FOR INSERT
    WITH CHECK (auth.role() = 'authenticated');

CREATE POLICY "Authenticated users can update machines"
    ON public.machines FOR UPDATE
    USING (auth.role() = 'authenticated');

CREATE POLICY "Authenticated users can delete machines"
    ON public.machines FOR DELETE
    USING (auth.role() = 'authenticated');

-- ─── Scan Events ───
CREATE POLICY "Authenticated users can view scan events"
    ON public.scan_events FOR SELECT
    USING (auth.role() = 'authenticated');

CREATE POLICY "Authenticated users can insert scan events"
    ON public.scan_events FOR INSERT
    WITH CHECK (auth.role() = 'authenticated');

-- ─── Maintenance Sessions ───
CREATE POLICY "Authenticated users can view maintenance"
    ON public.maintenance_sessions FOR SELECT
    USING (auth.role() = 'authenticated');

CREATE POLICY "Authenticated users can insert maintenance"
    ON public.maintenance_sessions FOR INSERT
    WITH CHECK (auth.role() = 'authenticated');

CREATE POLICY "Authenticated users can update maintenance"
    ON public.maintenance_sessions FOR UPDATE
    USING (auth.role() = 'authenticated');

-- ─── Departments ───
CREATE POLICY "Authenticated users can view departments"
    ON public.departments FOR SELECT
    USING (auth.role() = 'authenticated');
```

Clique **"Run"** → résultat : `Success` ✅

---

## Étape 8 — Insérer les données de test

Nouvelle requête SQL :

```sql
-- ═══════════════════════════════════════════════════════════════
-- DONNÉES DE TEST
-- ═══════════════════════════════════════════════════════════════

-- Départements LEONI
INSERT INTO public.departments (code, name, description) VALUES
    ('LTN1', 'LEONI Tunisie 1', 'Site de production principal'),
    ('LTN2', 'LEONI Tunisie 2', 'Site de production secondaire'),
    ('LTN3', 'LEONI Tunisie 3', 'Site de production tertiaire');

-- Machines de test avec des tags EPC fictifs
-- (les vrais tags seront ajoutés via le scan du Zebra MC3300x)
INSERT INTO public.machines (tag_id, name, department, status) VALUES
    ('E200001', 'Machine Coupe A1', 'LTN1', 'Running'),
    ('E200002', 'Machine Sertissage B2', 'LTN1', 'Running'),
    ('E200003', 'Machine Assemblage C3', 'LTN2', 'Running'),
    ('E200004', 'Presse Hydraulique D4', 'LTN2', 'Broken'),
    ('E200005', 'Robot Soudure E5', 'LTN3', 'Running');
```

Clique **"Run"** → résultat : `Success. 3 rows` + `Success. 5 rows` ✅

---

## Étape 9 — Créer les utilisateurs de test

### 9.1 Créer les comptes via Supabase Studio

1. Dans Supabase Studio, clique sur **Authentication** (icône 👤) dans le menu de gauche
2. Clique sur **"Add User"** → **"Create New User"**
3. Crée les 3 utilisateurs suivants :

| Email | Mot de passe | ⚠️ Cocher "Auto Confirm User" |
|-------|-------------|-------------------------------|
| `admin@leoni.com` | `Admin@1234` | ✅ OUI |
| `tech@leoni.com` | `Tech@1234` | ✅ OUI |
| `maintenance@leoni.com` | `Maint@1234` | ✅ OUI |

> [!CAUTION]
> **Tu DOIS cocher "Auto Confirm User"** pour chaque utilisateur ! Sinon le compte ne sera pas activé et le login échouera.

### 9.2 Configurer les rôles et désactiver `must_change_password`

Le trigger a créé les profils automatiquement, mais avec le rôle `Technician` par défaut.
Va dans le **SQL Editor** et exécute :

```sql
-- Donner le rôle Admin au premier utilisateur
UPDATE public.profiles
SET full_name = 'Admin LEONI',
    role = 'Admin',
    must_change_password = false
WHERE id = (SELECT id FROM auth.users WHERE email = 'admin@leoni.com');

-- Configurer le Technicien
UPDATE public.profiles
SET full_name = 'Tech Test',
    role = 'Technician',
    must_change_password = false
WHERE id = (SELECT id FROM auth.users WHERE email = 'tech@leoni.com');

-- Configurer l'Agent de Maintenance
UPDATE public.profiles
SET full_name = 'Agent Maintenance',
    role = 'Maintenance',
    must_change_password = false
WHERE id = (SELECT id FROM auth.users WHERE email = 'maintenance@leoni.com');
```

> [!NOTE]
> On met `must_change_password = false` pour les comptes de test afin d'éviter le flux "première connexion" pendant la démo. En production, ce flag serait `true`.

### 9.3 Vérifier les utilisateurs

1. Va dans **Table Editor** → table **`profiles`**
2. Tu devrais voir 3 lignes avec les bons noms et rôles

| full_name | role | is_active | must_change_password |
|-----------|------|-----------|---------------------|
| Admin LEONI | Admin | true | false |
| Tech Test | Technician | true | false |
| Agent Maintenance | Maintenance | true | false |

---

## Étape 10 — Modifier le code de l'application

### 10.1 Fichier `Constants.cs` (DÉJÀ FAIT ✅)

Le fichier `LeoniRFID/Helpers/Constants.cs` a déjà été modifié pour pointer vers l'instance locale :

```csharp
// ── Supabase LOCAL (Docker sur PC — IP LAN 192.168.1.122)
public const string SupabaseUrl = "http://192.168.1.122:8000";
public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
public const string SupabaseServiceRoleKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
```

> [!WARNING]
> **Si ton adresse IP est différente de `192.168.1.122`**, tu dois la changer ici ! Utilise `ipconfig | Select-String "IPv4"` dans PowerShell pour trouver la bonne IP.

### 10.2 Fichier `AndroidManifest.xml` (DÉJÀ FAIT ✅)

Le fichier `Platforms/Android/AndroidManifest.xml` a déjà été modifié pour autoriser les connexions HTTP (cleartext) :

```xml
<application android:usesCleartextTraffic="true" ... >
```

> [!NOTE]
> Cette modification est **nécessaire** car Supabase locale fonctionne en HTTP (pas HTTPS). Android bloque par défaut les connexions HTTP non sécurisées.

### 10.3 Aucune autre modification nécessaire

Les fichiers suivants ne changent **PAS** :
- ❌ `SupabaseService.cs` — reste identique
- ❌ `Models/*.cs` — restent identiques
- ❌ `ViewModels/*.cs` — restent identiques
- ❌ `Views/*.xaml` — restent identiques

---

## Étape 11 — Connecter le Zebra MC3300x

### 11.1 Réseau

Le terminal Zebra et le PC doivent être sur le **même réseau WiFi** :

```
┌─────────────────────────────────────────┐
│        Réseau WiFi Local LEONI          │
│                                         │
│  ┌──────────────────┐                   │
│  │ PC Windows       │                   │
│  │ IP: 192.168.1.122│                   │
│  │ Docker + Supabase│                   │
│  └────────┬─────────┘                   │
│           │  Port 8000                  │
│           │                             │
│  ┌────────▼─────────┐                   │
│  │ Zebra MC3300x    │                   │
│  │ WiFi connecté    │                   │
│  │ App LeoniRFID    │                   │
│  └──────────────────┘                   │
└─────────────────────────────────────────┘
```

### 11.2 Vérifier la connectivité

Depuis le Zebra (ou un smartphone de test), ouvre le navigateur et accède à :
```
http://192.168.1.122:8000
```

Si tu vois la page de login Supabase Studio, **la connexion réseau fonctionne** ✅

### 11.3 Déployer l'APK sur le Zebra

Depuis Visual Studio :
1. Connecte le Zebra en USB
2. Sélectionne le device dans la barre de déploiement
3. Lance en mode **Debug** (F5)

Ou compile un APK en Release :
```powershell
dotnet publish -f net9.0-android -c Release
```

---

## Étape 12 — Tester le flux complet

### Scénario de validation avec le Zebra MC3300x

Effectue ces tests **dans l'ordre** pour valider le flux complet :

#### Test 1 : Connexion (tous les rôles)

| # | Action | Résultat attendu |
|---|--------|-----------------|
| 1.1 | Login avec `admin@leoni.com` / `Admin@1234` | Dashboard + menus Admin/Maintenance visibles |
| 1.2 | Déconnexion | Retour page login |
| 1.3 | Login avec `tech@leoni.com` / `Tech@1234` | Dashboard + menus Admin/Maintenance **cachés** |
| 1.4 | Déconnexion | Retour page login |
| 1.5 | Login avec `maintenance@leoni.com` / `Maint@1234` | Dashboard + menu Maintenance visible, Admin **caché** |

#### Test 2 : Dashboard

| # | Action | Résultat attendu |
|---|--------|-----------------|
| 2.1 | Observer le Dashboard | 5 machines total, 4 Running, 1 Broken |
| 2.2 | Voir les compteurs par département | LTN1: 2, LTN2: 2, LTN3: 1 |

#### Test 3 : Scan RFID (avec le Zebra)

| # | Action | Résultat attendu |
|---|--------|-----------------|
| 3.1 | Login en tant que **Technicien** | OK |
| 3.2 | Aller sur la page Scanner | Page scan s'affiche |
| 3.3 | Scanner un tag RFID connu (ex: coller "E200001" sur un objet) | Machine "Machine Coupe A1" trouvée ✅ |
| 3.4 | Appuyer sur "Signaler Panne" | Statut → Broken 🔴 |
| 3.5 | Scanner un tag RFID **inconnu** | Formulaire "Enregistrer nouvelle machine" s'affiche |
| 3.6 | Remplir le formulaire et sauvegarder | Nouvelle machine créée avec statut ⏸️ Paused |

#### Test 4 : Workflow Maintenance complet

| # | Action | Résultat attendu |
|---|--------|-----------------|
| 4.1 | Login en tant que **Agent Maintenance** | OK |
| 4.2 | Aller sur la page Maintenance | Machine "E200004" (Broken) visible dans la liste |
| 4.3 | Sélectionner la machine + cliquer "Commencer Maintenance" | Statut → InMaintenance 🔧 + Chronomètre démarre |
| 4.4 | Attendre 30 secondes (le chrono tourne) | Timer affiche ~00:00:30 |
| 4.5 | Cliquer "Terminer Maintenance" | Statut → Running ✅ + Durée enregistrée |

#### Test 5 : Export Excel (Admin)

| # | Action | Résultat attendu |
|---|--------|-----------------|
| 5.1 | Login en tant que **Admin** | OK |
| 5.2 | Aller sur la page Rapports | Listes de machines et maintenances |
| 5.3 | Cliquer "Exporter Excel" | Fichier .xlsx généré avec 2 onglets |

#### Test 6 : Gestion Utilisateurs (Admin)

| # | Action | Résultat attendu |
|---|--------|-----------------|
| 6.1 | Aller sur "Gestion Utilisateurs" | Liste des 3 utilisateurs |
| 6.2 | Créer un nouveau compte | Compte créé dans Supabase locale |

---

## 🔧 Dépannage (Problèmes Courants)

### Problème : "Connection refused" depuis le Zebra

**Cause** : Le pare-feu Windows bloque les connexions entrantes sur le port 8000.

**Solution** :
```powershell
# Exécuter en PowerShell ADMINISTRATEUR
New-NetFirewallRule -DisplayName "Supabase Local" -Direction Inbound -Port 8000 -Protocol TCP -Action Allow
```

### Problème : Docker compose affiche "port already in use"

**Cause** : Un autre service utilise le port 8000.

**Solution** :
```powershell
# Voir qui utilise le port 8000
netstat -ano | findstr :8000

# Si c'est un autre programme, arrêter le process
Stop-Process -Id <PID_AFFICHÉ>
```

### Problème : "invalid API key" dans l'application

**Cause** : Les clés JWT dans `Constants.cs` ne correspondent pas au `JWT_SECRET` dans le `.env`.

**Solution** : Vérifier que les clés `ANON_KEY` et `SERVICE_ROLE_KEY` dans `Constants.cs` sont **exactement les mêmes** que celles dans `C:\supabase-local\docker\.env`.

### Problème : Les tables n'existent pas dans Supabase Studio

**Cause** : Le script SQL de l'étape 6 n'a pas été exécuté, ou a échoué.

**Solution** : Ré-exécuter le script SQL de l'étape 6 dans le SQL Editor.

### Problème : Login réussit mais le Dashboard est vide

**Cause** : Les données de test n'ont pas été insérées (étape 8).

**Solution** : Ré-exécuter le script SQL de l'étape 8.

### Problème : L'application ne se connecte pas du tout

**Vérifications** :
1. Docker Desktop est lancé ? (icône verte dans la barre des tâches)
2. `docker compose ps` — tous les services sont "Up" ?
3. L'IP dans `Constants.cs` correspond bien à l'IP du PC ? (`ipconfig`)
4. Le Zebra est sur le même réseau WiFi ?
5. Le pare-feu autorise le port 8000 ?

### Problème : Supabase Studio demande un mot de passe

**Solution** : Le mot de passe par défaut est **`LeoniAdmin2024`** (défini dans le `.env`).

---

## 🔄 Comment revenir à Supabase Cloud

Pour revenir à la version Cloud (après la validation locale), il suffit de modifier `Constants.cs` :

```csharp
// ── Supabase CLOUD (Internet)
public const string SupabaseUrl = "https://slxcwjgargafbvnitact.supabase.co";
public const string SupabaseAnonKey = "sb_publishable_lfFMzw0_GEFREdU-X-J_Iw_kHven22Z";
public const string SupabaseServiceRoleKey = "sb_secret_HvoLXCNtXOM4AnNZZrlVug_26YWZHgo";
```

Et retirer `android:usesCleartextTraffic="true"` du `AndroidManifest.xml` (optionnel, pour la sécurité).

---

## 📊 Commandes Docker utiles

| Commande | Description |
|----------|-------------|
| `docker compose up -d` | Démarrer tous les services |
| `docker compose down` | Arrêter tous les services |
| `docker compose ps` | Voir l'état des services |
| `docker compose logs -f` | Voir les logs en temps réel |
| `docker compose logs supabase-db` | Voir les logs de PostgreSQL |
| `docker compose restart` | Redémarrer tous les services |
| `docker compose down -v` | ⚠️ Arrêter ET supprimer les données |

> [!CAUTION]
> **Ne jamais utiliser `docker compose down -v`** sauf si tu veux **supprimer toutes les données** (tables, utilisateurs, machines) et repartir de zéro.

---

## 📐 Architecture Réseau

```
┌─────────────────────────────────────┐
│  PC Windows (192.168.1.122)         │
│                                     │
│  ┌───────────────────────────────┐  │
│  │  Docker Desktop (WSL2)        │  │
│  │                               │  │
│  │  ┌─────────────────────────┐  │  │
│  │  │ Supabase Local          │  │  │
│  │  │                         │  │  │
│  │  │  PostgreSQL   :5432     │  │  │
│  │  │  Kong API     :8000   ◄─┼──┼──┼── Zebra MC3300x (WiFi)
│  │  │  GoTrue Auth  :9999    │  │  │
│  │  │  Studio       :8000    │  │  │
│  │  │  PostgREST    :3000    │  │  │
│  │  │  Realtime     :4000    │  │  │
│  │  └─────────────────────────┘  │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

---

## ✅ Checklist de Validation Finale

Avant la présentation devant LEONI, vérifie que **tous** ces points sont cochés :

- [ ] Docker Desktop est installé et lancé
- [ ] `docker compose ps` — tous les services "Up"
- [ ] Supabase Studio accessible sur `http://localhost:8000`
- [ ] Les 5 tables créées (profiles, machines, scan_events, maintenance_sessions, departments)
- [ ] 3 départements insérés (LTN1, LTN2, LTN3)
- [ ] 5 machines de test insérées
- [ ] 3 utilisateurs créés (Admin, Tech, Maintenance)
- [ ] `Constants.cs` pointe vers l'IP locale
- [ ] `AndroidManifest.xml` autorise le cleartext HTTP
- [ ] L'app compile sans erreurs
- [ ] Le Zebra est sur le même WiFi que le PC
- [ ] Le pare-feu autorise le port 8000
- [ ] Login fonctionne avec les 3 rôles
- [ ] Le scan RFID enregistre les événements
- [ ] Le workflow Maintenance fonctionne (Broken → InMaintenance → Running)
- [ ] L'export Excel génère un fichier

---

*Guide créé par Oussama Souissi — Encadrant PFE LeoniRFID*
*Dernière mise à jour : Avril 2026*
