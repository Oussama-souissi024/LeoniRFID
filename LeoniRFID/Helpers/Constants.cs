namespace LeoniRFID.Helpers;

public static class Constants
{
    // 🎓 Pédagogie PFE : Centraliser la configuration magique
    // Au lieu d'avoir l'URL et les clés de la base de données dispersées dans 10 fichiers différents,
    // on centralise tout ici. C'est l'essence même du développement propre (DRY : Don't Repeat Yourself).

    // ── Supabase LOCAL (Docker sur PC — IP LAN 192.168.1.122)
    // 🎓 Pédagogie PFE : Pour la validation avec le Zebra MC3300x sur le réseau local de LEONI.
    // Le Zebra doit être connecté au même réseau WiFi que ce PC.
    // Pour repasser en Cloud : remplacer l'URL et les clés par celles de slxcwjgargafbvnitact.supabase.co
    public const string SupabaseUrl = "http://192.168.1.122:8000";
    // Clé publique (anon) — générée localement pour l'instance Docker
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoiYW5vbiJ9.9Ih9WSOMJxCeTSzYUmOTNYPcsm82suLqON5DeO0qgaI";
    // Clé de service (service_role) — générée localement pour l'instance Docker
    public const string SupabaseServiceRoleKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoic2VydmljZV9yb2xlIn0.IPwblUTo1d53u6Zo48EaOgGqlfxb6xHwyDunhMYVo5E";

    // ── Rôles utilisateurs
    public const string RoleAdmin       = "Admin";
    public const string RoleTechnician  = "Technician";
    public const string RoleMaintenance = "Maintenance";

    // ── Statuts Équipement (Maquette LEONI réelle)
    public const string StatusActive            = "Active";            // ✅ Active
    public const string StatusPassive           = "Passive";           // ⏸️ Passive
    public const string StatusDefect            = "Defect";            // 🔴 Defect
    public const string StatusScrapped          = "Scrapped";          // ❌ Scrapped
    public const string StatusTransferDone      = "TransferDone";      // 🔄 Transfer Done
    public const string StatusTransferOngoing   = "TransferOngoing";   // 🔃 Transfer Ongoing
    public const string StatusTransferAvailable = "TransferAvailable"; // 📦 Transfer Available

    // Aliases de compatibilité pour le workflow de maintenance
    public const string StatusRunning       = StatusActive;
    public const string StatusBroken        = StatusDefect;
    public const string StatusInMaintenance = "InMaintenance";  // 🔧 Maintenance en cours (workflow interne)
    public const string StatusPaused        = StatusPassive;
    public const string StatusRemoved       = StatusScrapped;

    // ── Plants / Sites (Maquette LEONI réelle)
    public static readonly string[] Plants = new[] { "MH", "SB", "MS", "MN", "LTN1", "LTN2", "LTN3" };
    public static readonly string[] Departments = Plants; // Alias de compatibilité

    // ── Intégration Zebra DataWedge (intents)
    public const string DataWedgeAction     = "com.symbol.datawedge.api.ACTION";
    public const string DataWedgeEpcData    = "com.symbol.datawedge.data_string";
}
