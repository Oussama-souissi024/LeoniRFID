# ✅ CHECKLIST PRÉ-LANCEMENT

## 1️⃣ Vérification de l'Environnement
- [ ] Android SDK installé (API 26+)
- [ ] Émulateur Android créé et disponible
- [ ] Visual Studio 2026 avec support MAUI
- [ ] PowerShell ou Terminal PowerShell ouvert

## 2️⃣ État du Code
- [ ] ✅ Compilation réussie (vérifiée)
- [ ] ✅ Pas d'erreurs de compilation
- [ ] ✅ Toutes les dépendances résolues
- [ ] ✅ Microsoft.Extensions.Http ajouté

## 3️⃣ Configuration du Projet
- [ ] ✅ Target Framework: net9.0-android
- [ ] ✅ Namespaces corrects
- [ ] ✅ Services correctement injectés
- [ ] ✅ ViewModels liés aux Pages

## 4️⃣ Base de Données
- [ ] ✅ DatabaseService initialisé dans LoginPage
- [ ] ✅ Utilisateurs de test créés automatiquement:
  - admin@leoni.com / Admin@1234 (Admin)
  - tech@leoni.com / Tech@1234 (Technician)
- [ ] ✅ SQLite configuré (DatabasePath correct)
- [ ] ✅ Tables: User, Department, Machine, ScanEvent

## 5️⃣ Permissions Android
- [ ] ✅ INTERNET
- [ ] ✅ ACCESS_NETWORK_STATE
- [ ] ✅ READ_EXTERNAL_STORAGE
- [ ] ✅ WRITE_EXTERNAL_STORAGE
- [ ] ✅ CAMERA

## 6️⃣ Services Déclarés
- [ ] ✅ DatabaseService (Singleton)
- [ ] ✅ AuthService (Singleton)
- [ ] ✅ IRfidService -> RfidService (Singleton)
- [ ] ✅ SyncService (Singleton)
- [ ] ✅ ExcelService (Singleton)
- [ ] ✅ ApiService avec HttpClient (Transient)

## 7️⃣ ViewModels Déclarés
- [ ] ✅ LoginViewModel
- [ ] ✅ DashboardViewModel
- [ ] ✅ ScanViewModel
- [ ] ✅ MachineDetailViewModel
- [ ] ✅ AdminViewModel
- [ ] ✅ ReportViewModel

## 8️⃣ Pages Déclarés
- [ ] ✅ LoginPage
- [ ] ✅ DashboardPage
- [ ] ✅ ScanPage
- [ ] ✅ MachineDetailPage
- [ ] ✅ AdminPage
- [ ] ✅ ReportPage

---

## 🚀 PRÊT À LANCER!

Si toutes les cases sont cochées ✅, vous pouvez lancer:

### Méthode 1 (Automatisée)
```powershell
cd C:\Users\PCHP\Documents\LeoniRFID_Production
.\run-emulator.ps1
```

### Méthode 2 (Visual Studio)
- F5 ou Clic sur ▶️

### Méthode 3 (Terminal)
```powershell
cd C:\Users\PCHP\Documents\LeoniRFID_Production
dotnet build -t:Run -c Release -f net9.0-android
```

---

## 📱 LORS DU LANCEMENT

L'application devrait:
1. Afficher l'écran de connexion (LoginPage)
2. Pré-remplir la base de données avec les utilisateurs de test
3. Accepter les identifiants de test
4. Afficher le Dashboard après connexion réussie
5. Permettre le scan RFID (si le matériel Zebra est disponible)

## 🔍 EN CAS DE PROBLÈME

```powershell
# Voir les logs
adb logcat -s LeoniRFID

# Redémarrer l'émulateur
adb reboot

# Réinstaller l'app
dotnet build -t:Run -c Release -f net9.0-android --no-restore --force
```

---

**Bon test! 🎉**
