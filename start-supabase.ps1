# ══════════════════════════════════════════════════════════════════
# Script de démarrage robuste — Supabase Local (Docker + WSL2)
# Prévient la corruption pg_toast en arrêtant proprement WSL2
# avant chaque démarrage.
#
# Usage : .\start-supabase.ps1
# ══════════════════════════════════════════════════════════════════

$SupabaseDockerDir = "C:\supabase-local\docker"
$DockerDesktopPath = "C:\Program Files\Docker\Docker\Docker Desktop.exe"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   LEONI RFID — Démarrage Supabase Local         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── Étape 1 : Vérifier que le dossier Docker existe ──────────────
if (-not (Test-Path $SupabaseDockerDir)) {
    Write-Host "❌ Dossier $SupabaseDockerDir introuvable." -ForegroundColor Red
    Write-Host "   As-tu cloné le dépôt Supabase ? (Étape 2 du guide)" -ForegroundColor Yellow
    exit 1
}

# ── Étape 2 : Arrêter proprement les conteneurs (si en cours) ────
Write-Host "[1/5] Arrêt propre des conteneurs..." -ForegroundColor Yellow
Push-Location $SupabaseDockerDir
try {
    docker compose down 2>$null
} catch {
    # Ignorer si les conteneurs n'étaient pas lancés
}
Pop-Location

# ── Étape 3 : Arrêter WSL2 proprement (prévient la corruption) ───
Write-Host "[2/5] Arrêt propre de WSL2 (prévient corruption pg_toast)..." -ForegroundColor Yellow
wsl --shutdown
Start-Sleep -Seconds 3

# ── Étape 4 : Lancer Docker Desktop ──────────────────────────────
Write-Host "[3/5] Lancement de Docker Desktop..." -ForegroundColor Yellow

# Vérifier si Docker Desktop est déjà en cours
$dockerRunning = $false
try {
    $dockerInfo = docker info 2>$null
    if ($dockerInfo -match "Server Version") {
        $dockerRunning = $true
    }
} catch {}

if (-not $dockerRunning) {
    if (Test-Path $DockerDesktopPath) {
        Start-Process $DockerDesktopPath
    } else {
        Write-Host "❌ Docker Desktop introuvable à : $DockerDesktopPath" -ForegroundColor Red
        Write-Host "   Vérifie que Docker Desktop est installé." -ForegroundColor Yellow
        exit 1
    }

    # Attendre que Docker soit prêt (max 120 secondes)
    Write-Host "   ⏳ En attente de Docker Engine..." -ForegroundColor DarkGray
    $timeout = 0
    do {
        Start-Sleep -Seconds 5
        $timeout += 5
        if ($timeout -ge 120) {
            Write-Host "❌ Docker Desktop n'a pas démarré dans les 120 secondes." -ForegroundColor Red
            Write-Host "   Lance Docker Desktop manuellement et relance ce script." -ForegroundColor Yellow
            exit 1
        }
        try {
            $ready = (docker info 2>$null) -match "Server Version"
        } catch {
            $ready = $false
        }
    } until ($ready)
}

Write-Host "   ✅ Docker Engine prêt !" -ForegroundColor Green

# ── Étape 5 : Lancer Supabase ────────────────────────────────────
Write-Host "[4/5] Lancement des services Supabase..." -ForegroundColor Yellow
Push-Location $SupabaseDockerDir
docker compose up -d
Pop-Location

# ── Étape 6 : Vérifier que tout est opérationnel ─────────────────
Write-Host "[5/5] Vérification des services..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

$services = docker compose -f "$SupabaseDockerDir\docker-compose.yml" ps --format "table" 2>$null
$failed = docker compose -f "$SupabaseDockerDir\docker-compose.yml" ps --format "{{.Status}}" 2>$null | Where-Object { $_ -notmatch "Up|running|healthy" -and $_ -ne "" }

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "⚠️  Certains services ne sont pas démarrés :" -ForegroundColor Red
    Write-Host $services
    Write-Host ""
    Write-Host "   Essaie : docker compose logs <nom_du_service>" -ForegroundColor Yellow
    Write-Host "   Ou consulte la section Dépannage dans ChangeToLocaleDB.md" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   ✅ SUPABASE LOCAL EST OPÉRATIONNEL !          ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Host "   Studio  : http://localhost:8000" -ForegroundColor White
    Write-Host "   Login    : supabase" -ForegroundColor White
    Write-Host "   Password : LeoniAdmin2024" -ForegroundColor White
    Write-Host ""
    Write-Host "   Pour l'application LeoniRFID, vérifie que Constants.cs" -ForegroundColor DarkGray
    Write-Host "   pointe vers l'IP locale du PC (ipconfig)." -ForegroundColor DarkGray
    Write-Host ""
}

# ── Astuce : Arrêt propre ─────────────────────────────────────────
Write-Host "💡 Pour arrêter proprement (éviter la corruption) :" -ForegroundColor DarkCyan
Write-Host "   .\stop-supabase.ps1" -ForegroundColor DarkCyan
Write-Host "   OU manuellement :" -ForegroundColor DarkCyan
Write-Host "   cd C:\supabase-local\docker ; docker compose down ; wsl --shutdown" -ForegroundColor DarkCyan
