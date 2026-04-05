using Android.App;
using Android.Content;

namespace LeoniRFID.Platforms.Android;

/// <summary>
/// 🎓 Pédagogie PFE : Intégration Matérielle Native Android (BroadcastReceiver)
/// Ce composant très spécifique à Android "écoute" les messages internes (Intents)
/// du système d'exploitation. Quand le lecteur physique (Zebra) scanne un tag,
/// l'appli système Zebra (DataWedge) diffuse un Intent. Ce code le capte
/// pour récupérer le code EPC sans avoir besoin d'un SDK complexe.
/// C'est une façon très élégante d'utiliser les capacités du terminal industriel.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilterAttribute(new string[] { "com.symbol.datawedge.api.ACTION" }, Categories = new string[] { "android.intent.category.DEFAULT" })]
public class DataWedgeIntentReceiver : BroadcastReceiver
{
    public static event EventHandler<string>? TagReceived;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent == null || intent.Action != "com.symbol.datawedge.api.ACTION") return;

        // DataWedge EPC RFID uses specific extra keys
        // Note: For newer Zebra devices, the key might vary based on DataWedge version.
        // Usually, the EPC is in "com.symbol.datawedge.data_string"
        
        string epc = intent.GetStringExtra("com.symbol.datawedge.data_string") ?? string.Empty;

        if (!string.IsNullOrEmpty(epc))
        {
            TagReceived?.Invoke(this, epc);
        }
    }
}
