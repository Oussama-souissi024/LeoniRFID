# 🚀 GUIDE DE DÉMARRAGE - LeoniRFID

## ✅ État du Projet
- **Compilation**: ✅ Réussie (net9.0-android)
- **Dépendances**: ✅ Correctes (Microsoft.Extensions.Http ajouté)
- **Configuration**: ✅ Complète

## 🎯 Prérequis
1. Android SDK installé (API 26+)
2. Émulateur Android lancé **OU** appareil connecté via USB
3. Visual Studio 2026 avec support MAUI

## 🚀 Pour Lancer l'Application

### Option 1: Via PowerShell (Recommandé)
```powershell
cd C:\Users\PCHP\Documents\LeoniRFID_Production
.\run-emulator.ps1
```

### Option 2: Via Visual Studio
1. Sélectionnez **net9.0-android** comme Target Framework
2. Cliquez sur le bouton **Exécuter** (▶️) ou appuyez sur **F5**
3. Sélectionnez l'émulateur dans la boîte de dialogue

### Option 3: Via Terminal PowerShell
```powershell
cd C:\Users\PCHP\Documents\LeoniRFID_Production
dotnet build -t:Run -c Release -f net9.0-android
```

## 🔐 Identifiants de Test

### Compte Administrateur
- **Email**: `admin@leoni.com`
- **Mot de passe**: `Admin@1234`
- **Accès**: Toutes les fonctionnalités incluant l'Administration

### Compte Technicien
- **Email**: `tech@leoni.com`
- **Mot de passe**: `Tech@1234`
- **Accès**: Scan, Dashboard, Rapports

## 📱 Test sur l'Émulateur

### Si l'Émulateur n'est Pas Lancé
```powershell
# Lister les émulateurs disponibles
emulator -list-avds

# Lancer un émulateur (exemple: Pixel_4_API_30)
emulator -avd Pixel_4_API_30 -no-boot-anim
```

### Vérifier la Connexion
```powershell
adb devices -l
```

Vous devriez voir quelque chose comme:
```
List of attached devices
emulator-5554          device product:sdk_google_phone_x86 model:Android_SDK_built_for_x86 device:generic_x86 transport_id:1
```

## 🔍 Dépannage

### Erreur: "No devices found"
- Lancez l'émulateur via Android Studio
- Ou connectez un appareil physique via USB

### Erreur: "Build failed"
- Exécutez: `dotnet clean -c Release`
- Puis: `dotnet build -c Release -f net9.0-android`

### L'Application Crash au Démarrage
- Vérifiez les logs: `adb logcat | findstr LeoniRFID`
- Vérifiez que la base de données SQLite est initialisée correctement

## 📋 Fonctionnalités à Tester

### 1. Authentification
- [ ] Connexion avec admin@leoni.com / Admin@1234
- [ ] Connexion avec tech@leoni.com / Tech@1234
- [ ] Message d'erreur pour identifiants incorrects
- [ ] Déconnexion depuis le menu

### 2. Dashboard (Tech)
- [ ] Affichage des machines par département
- [ ] Statistiques en temps réel

### 3. Scan RFID (Tech)
- [ ] Réception des données Zebra DataWedge
- [ ] Enregistrement des machines scannées

### 4. Rapports (Admin/Tech)
- [ ] Export Excel
- [ ] Affichage des statistiques

### 5. Administration (Admin)
- [ ] Gestion des utilisateurs
- [ ] Gestion des machines
- [ ] Synchronisation des données

## 📊 Logs Android
Pour voir les logs de l'application:
```powershell
adb logcat -s LeoniRFID
```

## ✨ Notes Importantes
- ✅ Tous les compilateurs sans erreurs
- ✅ Base de données SQLite auto-initialisée
- ✅ Utilisateurs de test pré-créés
- ✅ Support Zebra DataWedge intégré
- ✅ Sécurité: Stockage sécurisé des sessions

Bonne chance! 🎉
