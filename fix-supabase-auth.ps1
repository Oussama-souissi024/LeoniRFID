# ══════════════════════════════════════════════════════════════════
# Script de RÉPARATION — Erreur 504 / SASL Auth Failed
# Corrige l'incohérence des mots de passe des rôles PostgreSQL
# quand le .env a été modifié après le premier docker compose up.
#
# Usage : .\fix-supabase-auth.ps1
#
# Problème : GoTrue (auth) ne peut pas se connecter à PostgreSQL car
# les rôles internes (supabase_auth_admin, etc.) ont encore l'ancien
# mot de passe généré lors du premier démarrage.
# ══════════════════════════════════════════════════════════════════

param(
    [string]$SupabaseDir = "C:\Users\oussa\supabase-local\docker"
)

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Red
Write-Host "║   LEONI RFID — Réparation Auth Supabase Local     ║" -ForegroundColor Red
Write-Host "║   Erreur 504 / SASL Auth Failed                   ║" -ForegroundColor Red
Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Red
Write-Host ""

# ── Vérifier que le dossier existe ────────────────────────────────
if (-not (Test-Path $SupabaseDir)) {
    Write-Host "❌ Dossier $SupabaseDir introuvable." -ForegroundColor Red
    Write-Host "   Vérifie le chemin du dépôt Supabase Docker." -ForegroundColor Yellow
    exit 1
}

# ── Lire le mot de passe du .env ──────────────────────────────────
$envFile = Join-Path $SupabaseDir ".env"
if (-not (Test-Path $envFile)) {
    Write-Host "❌ Fichier .env introuvable dans $SupabaseDir" -ForegroundColor Red
    exit 1
}

$envContent = Get-Content $envFile -Raw
$passwordMatch = [regex]::Match($envContent, 'POSTGRES_PASSWORD=(.+?)[\r\n]')
if (-not $passwordMatch.Success) {
    Write-Host "❌ POSTGRES_PASSWORD non trouvé dans le .env" -ForegroundColor Red
    exit 1
}
$POSTGRES_PASSWORD = $passwordMatch.Groups[1].Value.Trim()
Write-Host "📝 Mot de passe trouvé dans .env : $($POSTGRES_PASSWORD.Substring(0,4))..." -ForegroundColor DarkGray

# ══════════════════════════════════════════════════════════════════
# ÉTAPE 1 : Arrêter PROPREMENT tous les conteneurs
# ══════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "[1/6] Arrêt propre de tous les conteneurs..." -ForegroundColor Yellow
Push-Location $SupabaseDir
docker compose down
Pop-Location

# ══════════════════════════════════════════════════════════════════
# ÉTAPE 2 : Arrêter WSL2 proprement
# ══════════════════════════════════════════════════════════════════
Write-Host "[2/6] Arrêt propre de WSL2..." -ForegroundColor Yellow
wsl --shutdown
Start-Sleep -Seconds 5

# ══════════════════════════════════════════════════════════════════
# ÉTAPE 3 : Redémarrer Docker Desktop
# ══════════════════════════════════════════════════════════════════
Write-Host "[3/6] Redémarrage de Docker Desktop..." -ForegroundColor Yellow
$dockerDesktopPath = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
if (Test-Path $dockerDesktopPath) {
    Start-Process $dockerDesktopPath
} else {
    Write-Host "   ⚠️  Docker Desktop introuvable, démarre-le manuellement." -ForegroundColor DarkYellow
}

Write-Host "   ⏳ En attente de Docker Engine..." -ForegroundColor DarkGray
$timeout = 0
do {
    Start-Sleep -Seconds 5
    $timeout += 5
    if ($timeout -ge 120) {
        Write-Host "❌ Docker Desktop n'a pas démarré dans les 120 secondes." -ForegroundColor Red
        exit 1
    }
    try { $ready = (docker info 2>$null) -match "Server Version" } catch { $ready = $false }
} until ($ready)
Write-Host "   ✅ Docker Engine prêt !" -ForegroundColor Green

# ══════════════════════════════════════════════════════════════════
# ÉTAPE 4 : Démarrer UNIQUEMENT la DB d'abord
# ══════════════════════════════════════════════════════════════════
Write-Host "[4/6] Démarrage de PostgreSQL seul..." -ForegroundColor Yellow
Push-Location $SupabaseDir
docker compose up -d db
Pop-Location

# Attendre que la DB soit healthy
Write-Host "   ⏳ En attente de PostgreSQL..." -ForegroundColor DarkGray
$dbReady = $false
$timeout = 0
do {
    Start-Sleep -Seconds 3
    $timeout += 3
    try {
        $status = docker inspect supabase-db --format "{{.State.Health.Status}}" 2>$null
        if ($status -eq "healthy") { $dbReady = $true }
    } catch {}
} until ($dbReady -or $timeout -ge 60)

if (-not $dbReady) {
    Write-Host "❌ PostgreSQL n'est pas devenu healthy." -ForegroundColor Red
    Write-Host "   Logs : docker compose logs db" -ForegroundColor Yellow
    exit 1
}
Write-Host "   ✅ PostgreSQL est healthy !" -ForegroundColor Green

# ══════════════════════════════════════════════════════════════════
# ÉTAPE 5 : Corriger les mots de passe de TOUS les rôles PostgreSQL
# ══════════════════════════════════════════════════════════════════
Write-Host "[5/6] Correction des mots de passe PostgreSQL..." -ForegroundColor Yellow

# Liste de tous les rôles Supabase qui utilisent POSTGRES_PASSWORD
$roles = @(
    "postgres",
    "supabase_admin",
    "supabase_auth_admin",
    "supabase_storage_admin",
    "supabase_functions_admin",
    "supabase_replication_admin",
    "authenticator",
    "anon",
    "authenticated",
    "service_role",
    "dashboard_user"
)

foreach ($role in $roles) {
    $escapedPwd = $POSTGRES_PASSWORD -replace "'", "''"
    $result = docker exec supabase-db psql -U supabase_admin -d postgres -c "ALTER USER ""$role"" WITH PASSWORD '$escapedPwd';" 2>&1
    if ($result -match "ALTER ROLE" -or $result -match "ALTER USER") {
        Write-Host "   ✅ Mot de passe corrigé pour : $role" -ForegroundColor Green
    } elseif ($result -match "does not exist") {
        Write-Host "   ⚠️  Rôle inexistant (ignoré) : $role" -ForegroundColor DarkYellow
    } else {
        # Essayer avec postgres
        $result2 = docker exec supabase-db psql -U postgres -d postgres -c "ALTER USER ""$role"" WITH PASSWORD '$escapedPwd';" 2>&1
        if ($result2 -match "ALTER ROLE" -or $result2 -match "ALTER USER") {
            Write-Host "   ✅ Mot de passe corrigé pour : $role" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Impossible de modifier : $role ($($result2.Trim()))" -ForegroundColor DarkYellow
        }
    }
}

# ══════════════════════════════════════════════════════════════════
# ÉTAPE 6 : Démarrer TOUS les autres services
# ══════════════════════════════════════════════════════════════════
Write-Host "[6/6] Démarrage de tous les services Supabase..." -ForegroundColor Yellow
Push-Location $SupabaseDir
docker compose up -d
Pop-Location

# Attendre que les services critiques soient prêts
Write-Host "   ⏳ En attente des services..." -ForegroundColor DarkGray
Start-Sleep -Seconds 15

# Vérifier l'état
$allServices = docker compose -f "$SupabaseDir\docker-compose.yml" ps --format "{{.Name}}\t{{.Status}}" 2>$null
$failed = $allServices | Where-Object { $_ -match "Created|Exited|Error" -and $_ -ne "" }

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "⚠️  Certains services ne sont pas démarrés. Relance..." -ForegroundColor DarkYellow
    Push-Location $SupabaseDir
    Start-Sleep -Seconds 10
    docker compose up -d --no-deps kong auth rest studio storage meta functions supavisor realtime 2>$null
    Pop-Location
    Start-Sleep -Seconds 15
}

# ══════════════════════════════════════════════════════════════════
# TEST D'AUTHENTIFICATION
# ══════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "🧪 Test d'authentification admin@leoni.com..." -ForegroundColor Cyan

# Lire l'anon key du .env
$anonKeyMatch = [regex]::Match($envContent, 'ANON_KEY=(.+?)[\r\n]')
$anonKey = if ($anonKeyMatch.Success) { $anonKeyMatch.Groups[1].Value.Trim() } else { "" }

if ([string]::IsNullOrWhiteSpace($anonKey)) {
    Write-Host "   ⚠️  ANON_KEY non trouvé dans .env, test ignoré." -ForegroundColor DarkYellow
} else {
    try {
        $body = '{"email":"admin@leoni.com","password":"Admin@1234"}'
        $headers = @{"apikey"=$anonKey}
        $resp = Invoke-RestMethod -Uri "http://localhost:8000/auth/v1/token?grant_type=password" -Method Post -ContentType "application/json" -Headers $headers -Body $body -ErrorAction Stop
        Write-Host "   ✅ AUTHENTIFICATION RÉUSSIE !" -ForegroundColor Green
        Write-Host "   Token obtenu pour : $($resp.user.email)" -ForegroundColor White
    } catch {
        $err = $_.Exception.Message
        if ($err -match "504|timeout|SASL") {
            Write-Host "   ❌ Échec — Toujours erreur SASL/504" -ForegroundColor Red
            Write-Host ""
            Write-Host "   🔧 Solution nucléaire (réinitialisation complète) :" -ForegroundColor Yellow
            Write-Host "   1. docker compose down -v  (⚠️ supprime les données)" -ForegroundColor Yellow
            Write-Host "   2. docker compose up -d     (recrée tout avec le bon .env)" -ForegroundColor Yellow
            Write-Host "   3. Re-exécuter init-schema.sql + créer les utilisateurs" -ForegroundColor Yellow
        } elseif ($err -match "invalid_credentials") {
            Write-Host "   ⚠️  Connexion OK mais identifiants invalides." -ForegroundColor DarkYellow
            Write-Host "   Les utilisateurs doivent être créés (voir init-schema.sql)" -ForegroundColor DarkYellow
        } else {
            Write-Host "   ❌ Erreur : $err" -ForegroundColor Red
        }
    }
}

# ── Résumé final ──────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Résumé de la réparation                         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Mot de passe synchronisé pour les rôles :" -ForegroundColor White
Write-Host "   postgres, supabase_admin, supabase_auth_admin," -ForegroundColor DarkGray
Write-Host "   supabase_storage_admin, authenticator, etc." -ForegroundColor DarkGray
Write-Host ""
Write-Host "   Si l'auth fonctionne ✅ → problème résolu !" -ForegroundColor Green
Write-Host "   Si l'auth échoue encore ❌ → voir solution nucléaire ci-dessus" -ForegroundColor Red
Write-Host ""
