# 🚀 COMMANDES RAPIDES - CHEAT SHEET

## ⚡ LES 3 MANIÈRES LES PLUS RAPIDES

### 1️⃣ BATCH SCRIPT (Recommandé)
```batch
run-app.bat
```
*Double-cliquez sur le fichier dans l'explorateur*

### 2️⃣ VISUAL STUDIO
```
F5
```
*Appuyez simplement sur F5 après avoir sélectionné net9.0-android*

### 3️⃣ POWERSHELL
```powershell
cd C:\Users\PCHP\Documents\LeoniRFID_Production
.\run-app.bat
```

---

## 📋 COMMANDES COMPLÈTES

### Nettoyer
```powershell
dotnet clean -c Release
```

### Compiler
```powershell
dotnet build -c Release -f net9.0-android --no-restore
```

### Compiler + Lancer
```powershell
dotnet build -t:Run -c Release -f net9.0-android --no-restore --force
```

### Lancer avec MAUI CLI
```powershell
dotnet maui run -c Release -f net9.0-android
```

### Voir les Logs
```powershell
adb logcat -s LeoniRFID
```

### Lister Émulateurs
```powershell
emulator -list-avds
```

### Lancer Émulateur (exemple)
```powershell
emulator -avd Pixel_4_API_30 -no-boot-anim
```

### Vérifier Appareils Connectés
```powershell
adb devices -l
```

### Réinitialiser l'App
```powershell
adb uninstall com.leoni.rfid.production
dotnet build -t:Run -c Release -f net9.0-android --no-restore --force
```

---

## 🔐 IDENTIFIANTS RAPIDES

```
ADMIN
━━━━━━━━━━━━━━━━━━━━━━
Email: admin@leoni.com
Pass:  Admin@1234

TECH
━━━━━━━━━━━━━━━━━━━━━━
Email: tech@leoni.com
Pass:  Tech@1234
```

---

## 📱 RÉSULTATS ATTENDUS

```
✅ Splash screen 1-2 sec
✅ Login page charge
✅ Connexion valide
✅ Dashboard affiche 42 machines
✅ Navigation fonctionne
✅ Déconnexion OK
```

---

## 🎯 WORKFLOW RAPIDE

```
1. Lancer run-app.bat
   ↓
2. Attendre compilation (60s)
   ↓
3. Sélectionner émulateur
   ↓
4. Attendre déploiement (30s)
   ↓
5. Login: admin@leoni.com / Admin@1234
   ↓
6. Dashboard s'affiche
   ↓
7. ✅ SUCCESS!
```

---

**Temps total: ~3-4 minutes pour première exécution**
**Exécutions suivantes: ~1-2 minutes**

