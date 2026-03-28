namespace LeoniRFID.Helpers;

public static class Constants
{
    // ── Supabase ─────────────────────────────────────────────────────────
    public const string SupabaseUrl = "https://slxcwjgargafbvnitact.supabase.co";
    public const string SupabaseAnonKey = "sb_publishable_lfFMzw0_GEFREdU-X-J_Iw_kHven22Z";
    public const string SupabaseServiceRoleKey = "sb_secret_HvoLXCNtXOM4AnNZZrlVug_26YWZHgo";

    // ── Rôles ─────────────────────────────────────────────────────────────
    public const string RoleAdmin      = "Admin";
    public const string RoleTechnician = "Technician";

    // ── Statuts Machine ───────────────────────────────────────────────────
    public const string StatusInstalled   = "Installed";
    public const string StatusRemoved     = "Removed";
    public const string StatusMaintenance = "Maintenance";

    // ── Départements ──────────────────────────────────────────────────────
    public static readonly string[] Departments = ["LTN1", "LTN2", "LTN3"];

    // ── Zebra DataWedge ───────────────────────────────────────────────────
    public const string DataWedgeAction     = "com.symbol.datawedge.api.ACTION";
    public const string DataWedgeEpcData    = "com.symbol.datawedge.data_string";
}
