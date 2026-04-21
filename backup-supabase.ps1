# ══════════════════════════════════════════════════════════════════
# Script de sauvegarde — Base PostgreSQL Supabase Local
# Exporte un dump complet de la base pour pouvoir restaurer
# en cas de corruption pg_toast ou de perte de données.
#
# Usage : .\backup-supabase.ps1
#
# Les sauvegardes sont stockées dans : .\backups\
# Fichier nommé : leoni_backup_YYYY-MM-DD_HH-mm.dump
# ══════════════════════════════════════════════════════════════════

$SupabaseDockerDir = "C:\supabase-local\docker"
$BackupDir = "$PSScriptRoot\backups"
$Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm"
$BackupFile = "$BackupDir\leoni_backup_$Timestamp.dump"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   LEONI RFID — Sauvegarde Supabase Local         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── Créer le dossier de sauvegarde ────────────────────────────────
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    Write-Host "📁 Dossier de sauvegarde créé : $BackupDir" -ForegroundColor DarkGray
}

# ── Vérifier que Supabase est en cours ────────────────────────────
$dbRunning = docker compose -f "$SupabaseDockerDir\docker-compose.yml" ps 2>$null | Select-String "supabase-db.*Up"
if (-not $dbRunning) {
    Write-Host "❌ Supabase n'est pas en cours d'exécution." -ForegroundColor Red
    Write-Host "   Lance d'abord : .\start-supabase.ps1" -ForegroundColor Yellow
    exit 1
}

# ── Exporter le dump via docker exec ──────────────────────────────
Write-Host "💾 Export de la base PostgreSQL..." -ForegroundColor Yellow

# Trouver le nom exact du conteneur DB
$dbContainer = docker compose -f "$SupabaseDockerDir\docker-compose.yml" ps --format "{{.Name}}" 2>$null | Where-Object { $_ -match "db" } | Select-Object -First 1

if (-not $dbContainer) {
    # Fallback : chercher par le nom du service
    $dbContainer = docker ps --format "{{.Names}}" | Where-Object { $_ -match "supabase-db|db" } | Select-Object -First 1
}

if (-not $dbContainer) {
    Write-Host "❌ Conteneur PostgreSQL introuvable." -ForegroundColor Red
    exit 1
}

Write-Host "   Conteneur : $dbContainer" -ForegroundColor DarkGray

# Exécuter pg_dump dans le conteneur
docker exec $dbContainer pg_dump -U supabase_admin -F c -f /tmp/leoni_backup.dump postgres 2>$null

if ($LASTEXITCODE -ne 0) {
    # Essayer avec l'utilisateur postgres par défaut
    docker exec $dbContainer pg_dump -U postgres -F c -f /tmp/leoni_backup.dump postgres 2>$null
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ pg_dump a échoué. Vérifie les logs : docker compose logs supabase-db" -ForegroundColor Red
    exit 1
}

# Copier le dump depuis le conteneur vers l'hôte
docker cp "${dbContainer}:/tmp/leoni_backup.dump" $BackupFile

if ($LASTEXITCODE -eq 0 -and (Test-Path $BackupFile)) {
    $size = (Get-Item $BackupFile).Length / 1KB
    Write-Host ""
    Write-Host "✅ Sauvegarde réussie !" -ForegroundColor Green
    Write-Host "   Fichier : $BackupFile" -ForegroundColor White
    Write-Host "   Taille  : $([math]::Round($size, 1)) Ko" -ForegroundColor White
    Write-Host ""

    # Nettoyer le dump temporaire dans le conteneur
    docker exec $dbContainer rm /tmp/leoni_backup.dump 2>$null | Out-Null

    # ── Rotation : Garder uniquement les 10 dernières sauvegardes ──
    $backups = Get-ChildItem "$BackupDir\leoni_backup_*.dump" | Sort-Object LastWriteTime -Descending
    if ($backups.Count -gt 10) {
        $toDelete = $backups | Select-Object -Skip 10
        foreach ($old in $toDelete) {
            Remove-Item $old.FullName -Force
            Write-Host "   🗑️ Supprimé ancienne sauvegarde : $($old.Name)" -ForegroundColor DarkGray
        }
    }
} else {
    Write-Host "❌ Échec de la copie du dump depuis le conteneur." -ForegroundColor Red
    exit 1
}

# ── Comment restaurer ─────────────────────────────────────────────
Write-Host "💡 Pour restaurer une sauvegarde :" -ForegroundColor DarkCyan
Write-Host "   .\restore-supabase.ps1 -BackupFile `"$BackupFile`"" -ForegroundColor DarkCyan
