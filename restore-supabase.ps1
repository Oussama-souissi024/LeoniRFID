# ══════════════════════════════════════════════════════════════════
# Script de restauration — Base PostgreSQL Supabase Local
# Restaure un dump créé par backup-supabase.ps1
# Utile après une corruption pg_toast ou une perte de données.
#
# Usage : .\restore-supabase.ps1 -BackupFile ".\backups\leoni_backup_2026-04-21_15-30.dump"
#   OU  : .\restore-supabase.ps1              (utilise la dernière sauvegarde)
# ══════════════════════════════════════════════════════════════════

param(
    [string]$BackupFile = ""
)

$SupabaseDockerDir = "C:\supabase-local\docker"
$BackupDir = "$PSScriptRoot\backups"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║   LEONI RFID — Restauration Supabase Local       ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

# ── Trouver le fichier de sauvegarde ──────────────────────────────
if ([string]::IsNullOrWhiteSpace($BackupFile)) {
    # Utiliser la dernière sauvegarde
    $latest = Get-ChildItem "$BackupDir\leoni_backup_*.dump" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) {
        Write-Host "❌ Aucune sauvegarde trouvée dans $BackupDir" -ForegroundColor Red
        Write-Host "   Lance d'abord : .\backup-supabase.ps1" -ForegroundColor Yellow
        exit 1
    }
    $BackupFile = $latest.FullName
}

if (-not (Test-Path $BackupFile)) {
    Write-Host "❌ Fichier introuvable : $BackupFile" -ForegroundColor Red
    exit 1
}

Write-Host "📦 Fichier de sauvegarde : $BackupFile" -ForegroundColor White
$size = (Get-Item $BackupFile).Length / 1KB
Write-Host "   Taille : $([math]::Round($size, 1)) Ko" -ForegroundColor DarkGray
Write-Host ""

# ── Confirmation ───────────────────────────────────────────────────
$confirm = Read-Host "⚠️  Cela va REMPLACER toutes les données actuelles. Continuer ? (oui/non)"
if ($confirm -ne "oui") {
    Write-Host "❌ Restauration annulée." -ForegroundColor Yellow
    exit 0
}

# ── Vérifier que Supabase est en cours ────────────────────────────
$dbRunning = docker compose -f "$SupabaseDockerDir\docker-compose.yml" ps 2>$null | Select-String "supabase-db.*Up"
if (-not $dbRunning) {
    Write-Host "❌ Supabase n'est pas en cours d'exécution." -ForegroundColor Red
    Write-Host "   Lance d'abord : .\start-supabase.ps1" -ForegroundColor Yellow
    exit 1
}

# ── Trouver le conteneur DB ───────────────────────────────────────
$dbContainer = docker compose -f "$SupabaseDockerDir\docker-compose.yml" ps --format "{{.Name}}" 2>$null | Where-Object { $_ -match "db" } | Select-Object -First 1
if (-not $dbContainer) {
    $dbContainer = docker ps --format "{{.Names}}" | Where-Object { $_ -match "supabase-db|db" } | Select-Object -First 1
}
if (-not $dbContainer) {
    Write-Host "❌ Conteneur PostgreSQL introuvable." -ForegroundColor Red
    exit 1
}

# ── Copier le dump dans le conteneur ──────────────────────────────
Write-Host "[1/3] Copie du dump dans le conteneur..." -ForegroundColor Yellow
docker cp $BackupFile "${dbContainer}:/tmp/leoni_restore.dump"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Échec de la copie vers le conteneur." -ForegroundColor Red
    exit 1
}

# ── Supprimer la base existante et recréer ────────────────────────
Write-Host "[2/3] Restauration de la base..." -ForegroundColor Yellow

# Terminer toutes les connexions actives sauf la nôtre
docker exec $dbContainer psql -U supabase_admin -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'postgres' AND pid <> pg_backend_pid();" 2>$null | Out-Null

# Restaurer le dump (format custom = pg_restore)
docker exec $dbContainer pg_restore -U supabase_admin -d postgres --clean --if-exists /tmp/leoni_restore.dump 2>$null

$restoreExit = $LASTEXITCODE

# Si pg_restore échoue avec supabase_admin, essayer avec postgres
if ($restoreExit -ne 0) {
    Write-Host "   Tentative avec l'utilisateur postgres..." -ForegroundColor DarkYellow
    docker exec $dbContainer pg_restore -U postgres -d postgres --clean --if-exists /tmp/leoni_restore.dump 2>$null
    $restoreExit = $LASTEXITCODE
}

# Nettoyer le dump temporaire
docker exec $dbContainer rm /tmp/leoni_restore.dump 2>$null | Out-Null

# ── Vérification ──────────────────────────────────────────────────
Write-Host "[3/3] Vérification..." -ForegroundColor Yellow

$tables = docker exec $dbContainer psql -U supabase_admin -d postgres -t -c "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';" 2>$null
$tableCount = if ($tables) { $tables.Trim() } else { "?" }

if ($restoreExit -eq 0 -or $tableCount -gt 0) {
    Write-Host ""
    Write-Host "✅ Restauration terminée !" -ForegroundColor Green
    Write-Host "   Tables public : $tableCount" -ForegroundColor White
    Write-Host ""
    Write-Host "   Vérifie les données dans Supabase Studio : http://localhost:8000" -ForegroundColor DarkGray
} else {
    Write-Host ""
    Write-Host "⚠️  La restauration a rencontré des erreurs (code: $restoreExit)." -ForegroundColor Red
    Write-Host "   Vérifie les tables dans Supabase Studio." -ForegroundColor Yellow
    Write-Host "   Si nécessaire, ré-exécute les étapes 6-9 du guide ChangeToLocaleDB.md" -ForegroundColor Yellow
}
