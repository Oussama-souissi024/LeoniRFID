@echo off
REM Script de lancement LEONI RFID pour Windows
REM Dépendances: ADB, Émulateur Android ou Appareil USB connecté

chcp 65001 > nul
echo.
echo ╔════════════════════════════════════════════════╗
echo ║     LEONI RFID - Launcher Android (.NET 9)    ║
echo ╚════════════════════════════════════════════════╝
echo.

REM Vérifier si on est au bon endroit
if not exist "LeoniRFID.csproj" (
    echo [ERREUR] Vous devez être dans le dossier LeoniRFID_Production
    echo Commande: cd C:\Users\PCHP\Documents\LeoniRFID_Production
    pause
    exit /b 1
)

echo [1/3] Nettoyage et compilation...
dotnet clean -c Release > nul 2>&1
dotnet build -c Release -f net9.0-android --no-restore

if errorlevel 1 (
    echo.
    echo [ERREUR] Compilation échouée!
    pause
    exit /b 1
)

echo.
echo [✓] Compilation réussie!
echo.
echo [2/3] Vérification de l'émulateur...
adb devices -l > nul 2>&1

if errorlevel 1 (
    echo.
    echo [ERREUR] ADB non trouvé ou émulateur non lancé!
    echo.
    echo Solutions:
    echo  1. Lancez l'émulateur via Android Studio
    echo  2. OU connectez un appareil Android via USB
    echo  3. Puis relancez ce script
    echo.
    pause
    exit /b 1
)

echo [✓] Émulateur/Appareil détecté!
echo.
echo [3/3] Déploiement de l'application...
echo.

dotnet build -t:Run -c Release -f net9.0-android --no-restore --force

if errorlevel 1 (
    echo.
    echo [ERREUR] Déploiement échoué!
    pause
    exit /b 1
)

echo.
echo ╔════════════════════════════════════════════════╗
echo ║    ✓ APPLICATION LANCÉE AVEC SUCCÈS!          ║
echo ╚════════════════════════════════════════════════╝
echo.
echo Identifiants de test:
echo   Admin: admin@leoni.com / Admin@1234
echo   Tech:  tech@leoni.com / Tech@1234
echo.
echo Logs: adb logcat -s LeoniRFID
echo.
pause
