# ══════════════════════════════════════════════════════════════════
# Script d'arrêt propre — Supabase Local (Docker + WSL2)
# Arrête les conteneurs puis WSL2 proprement pour prévenir
# la corruption pg_toast du disque virtuel ext4.vhdx.
#
# Usage : .\stop-supabase.ps1
# ══════════════════════════════════════════════════════════════════

$SupabaseDockerDir = "C:\supabase-local\docker"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║   LEONI RFID — Arrêt Supabase Local              ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# ── Étape 1 : Arrêter les conteneurs proprement ───────────────────
Write-Host "[1/2] Arrêt des conteneurs Supabase..." -ForegroundColor Yellow
if (Test-Path $SupabaseDockerDir) {
    Push-Location $SupabaseDockerDir
    docker compose down
    Pop-Location
    Write-Host "   ✅ Conteneurs arrêtés." -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Dossier $SupabaseDockerDir introuvable — arrêt direct de WSL2." -ForegroundColor DarkYellow
}

# ── Étape 2 : Arrêter WSL2 proprement ─────────────────────────────
Write-Host "[2/2] Arrêt propre de WSL2 (sauvegarde le disque virtuel)..." -ForegroundColor Yellow
wsl --shutdown
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "✅ Supabase Local arrêté proprement !" -ForegroundColor Green
Write-Host "   (Cela prévient la corruption pg_toast du disque WSL2)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "💡 Pour relancer : .\start-supabase.ps1" -ForegroundColor DarkCyan
