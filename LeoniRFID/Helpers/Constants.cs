namespace LeoniRFID.Helpers;

public static class Constants
{
    // ── API ───────────────────────────────────────────────────────────────────
    public const string ApiBaseUrl = "https://api.leoni-rfid.local/api/v1/";

    // ── Auth ──────────────────────────────────────────────────────────────────
    public const string GoogleClientId = "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com";
    public const string SessionTokenKey = "leoni_session_token";
    public const string CurrentUserKey  = "leoni_current_user";

    // ── Database ──────────────────────────────────────────────────────────────
    public const string DatabaseFilename = "leoni_rfid.db3";
    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

    // ── Departments ───────────────────────────────────────────────────────────
    public static readonly string[] Departments = ["LTN1", "LTN2", "LTN3"];

    // ── Machine Statuses ──────────────────────────────────────────────────────
    public const string StatusInstalled   = "Installed";
    public const string StatusRemoved     = "Removed";
    public const string StatusMaintenance = "Maintenance";

    // ── Roles ─────────────────────────────────────────────────────────────────
    public const string RoleAdmin      = "Admin";
    public const string RoleTechnician = "Technician";

    // ── Zebra DataWedge ───────────────────────────────────────────────────────
    public const string DataWedgeAction      = "com.symbol.datawedge.api.ACTION";
    public const string DataWedgeCategoryDefault = "android.intent.category.DEFAULT";
    public const string DataWedgeScanResult  = "com.symbol.datawedge.data.RESULT_ACTION";
    public const string DataWedgeEpcData     = "com.symbol.datawedge.data_string";
    public const string DataWedgeLabelType   = "com.symbol.datawedge.label_type";

    // ── Test Accounts ─────────────────────────────────────────────────────────
    public const string TestAdminEmail     = "admin@leoni.com";
    public const string TestAdminPassword  = "Admin@1234";
    public const string TestTechEmail      = "tech@leoni.com";
    public const string TestTechPassword   = "Tech@1234";
}
