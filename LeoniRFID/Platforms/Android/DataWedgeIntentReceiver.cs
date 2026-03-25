using Android.App;
using Android.Content;

namespace LeoniRFID.Platforms.Android;

/// <summary>
/// Receives intents from Zebra DataWedge for RFID scanning.
/// Requires DataWedge profile to be configured with Intent Output.
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
