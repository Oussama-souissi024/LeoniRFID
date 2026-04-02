namespace LeoniRFID.Helpers;

public static class Constants
{
    // Commentaire pédagogique :
    // - Centralise les constantes applicatives (URLs, clés, rôles, statuts).
    // - Garder les constantes en un endroit facilite la maintenance et les tests.

    // ── Supabase (BaaS) : URL et clés publiques/privées
    public const string SupabaseUrl = "https://slxcwjgargafbvnitact.supabase.co";
    // Clé publique (anon) utilisée côté client. Ne pas confondre avec la clé de service (privée).
    public const string SupabaseAnonKey = "sb_publishable_lfFMzw0_GEFREdU-X-J_Iw_kHven22Z";
    // Clé de service (ne pas exposer côté client dans une app mobile publique).
    public const string SupabaseServiceRoleKey = "sb_secret_HvoLXCNtXOM4AnNZZrlVug_26YWZHgo";

    // ── Rôles utilisateurs
    public const string RoleAdmin      = "Admin";
    public const string RoleTechnician = "Technician";

    // ── Statuts Machine
    public const string StatusInstalled   = "Installed";
    public const string StatusRemoved     = "Removed";
    public const string StatusMaintenance = "Maintenance";

    // ── Départements (exemple statique)
    public static readonly string[] Departments = new[] { "LTN1", "LTN2", "LTN3" };

    // ── Intégration Zebra DataWedge (intents)
    public const string DataWedgeAction     = "com.symbol.datawedge.api.ACTION";
    public const string DataWedgeEpcData    = "com.symbol.datawedge.data_string";
}
