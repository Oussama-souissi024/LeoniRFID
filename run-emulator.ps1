# Script pour compiler et lancer l'application sur l'émulateur Android
# Usage: .\run-emulator.ps1

Write-Host "================================" -ForegroundColor Cyan
Write-Host "LEONI RFID - Build & Run" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Étape 1: Nettoyer et compiler
Write-Host "[1/3] Compilation du projet..." -ForegroundColor Yellow
dotnet clean -c Release
dotnet build -c Release -f net9.0-android

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ La compilation a échoué!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Compilation réussie!" -ForegroundColor Green
Write-Host ""

# Étape 2: Vérifier les émulateurs disponibles
Write-Host "[2/3] Vérification des émulateurs disponibles..." -ForegroundColor Yellow
$emulators = adb devices -l | Select-String "device"

if ($emulators.Count -eq 0) {
    Write-Host "⚠️  Aucun émulateur ou appareil détecté!" -ForegroundColor Yellow
    Write-Host "Assurez-vous que:" -ForegroundColor Yellow
    Write-Host "  - Un émulateur Android est lancé" -ForegroundColor Yellow
    Write-Host "  - OU un appareil Android est connecté via USB" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Émulateurs/Appareils détectés:" -ForegroundColor Green
$emulators | ForEach-Object { Write-Host "  - $_" }
Write-Host ""

# Étape 3: Compiler et déployer
Write-Host "[3/3] Déploiement sur l'émulateur..." -ForegroundColor Yellow
dotnet build -t:Run -c Release -f net9.0-android

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Application lancée avec succès!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📝 Identifiants de test:" -ForegroundColor Cyan
    Write-Host "   Admin: admin@leoni.com / Admin@1234" -ForegroundColor Gray
    Write-Host "   Tech:  tech@leoni.com / Tech@1234" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "❌ Le déploiement a échoué!" -ForegroundColor Red
    exit 1
}
