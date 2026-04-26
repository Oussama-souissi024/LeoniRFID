using Android.App;
using Android.Content;

namespace LeoniRFID.Platforms.Android;

/// <summary>
/// 🎓 Pédagogie PFE : Intégration Matérielle Native Android (BroadcastReceiver)
/// 
/// Ce composant Android "écoute" les Intents envoyés par DataWedge quand le
/// lecteur RFID UHF du Zebra MC3300xR capture un tag.
/// 
/// ⚠️ IMPORTANT : L'action Intent ici DOIT correspondre EXACTEMENT à celle
/// configurée dans le profil DataWedge sur le terminal Zebra.
/// 
/// Configuration DataWedge requise :
///   Profile → Intent Output → Action : "com.leoni.rfid.SCAN"
///   Profile → Intent Output → Delivery : "Broadcast Intent"
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { DataWedgeIntentReceiver.DATAWEDGE_ACTION },
    Categories = new[] { "android.intent.category.DEFAULT" })]
public class DataWedgeIntentReceiver : BroadcastReceiver
{
    // 🎓 Action Intent personnalisée — doit matcher le profil DataWedge
    public const string DATAWEDGE_ACTION = "com.leoni.rfid.SCAN";

    // 🎓 Clés standard DataWedge pour extraire les données du scan
    private const string EXTRA_DATA_STRING = "com.symbol.datawedge.data_string";
    private const string EXTRA_SOURCE      = "com.symbol.datawedge.source";
    private const string EXTRA_LABEL_TYPE  = "com.symbol.datawedge.label_type";

    /// <summary>
    /// Événement statique déclenché quand un tag est scanné.
    /// Le RfidService s'y abonne pour propager vers le ViewModel.
    /// </summary>
    public static event EventHandler<string>? TagReceived;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != DATAWEDGE_ACTION) return;

        // Extraire la donnée du scan (EPC pour RFID, code pour barcode)
        string? data = intent.GetStringExtra(EXTRA_DATA_STRING);

        // Fallback : certaines versions DataWedge utilisent d'autres extras
        if (string.IsNullOrEmpty(data))
        {
            var bundle = intent.Extras;
            if (bundle != null)
            {
                foreach (var key in bundle.KeySet() ?? [])
                {
                    if (key.Contains("data", StringComparison.OrdinalIgnoreCase))
                    {
                        data = bundle.GetString(key);
                        if (!string.IsNullOrEmpty(data)) break;
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(data))
        {
            // Nettoyer l'EPC (enlever espaces, retours à la ligne)
            data = data.Trim().Replace("\n", "").Replace("\r", "");
            TagReceived?.Invoke(this, data);
        }
    }
}
