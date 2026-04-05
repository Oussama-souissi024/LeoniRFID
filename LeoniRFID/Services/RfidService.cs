namespace LeoniRFID.Services;

/// <summary>
/// 🎓 Pédagogie PFE : Intégration Matérielle (Hardware)
/// Ce service fait le lien entre le lecteur RFID physique (Zebra MC3300x)
/// et notre application .NET MAUI. Sur un vrai appareil Zebra, l'application
/// DataWedge envoie un "Intent Android" contenant le code EPC du tag scanné.
/// Sur un PC de développement (sans lecteur), on utilise SimulateScan().
/// </summary>
public class RfidService : IRfidService
{
    public event EventHandler<string>? TagScanned;
    public bool IsListening { get; private set; }

    /// <summary>
    /// Start listening for DataWedge scan intents.
    /// The actual BroadcastReceiver is registered on Android platform.
    /// </summary>
    public void StartListening()
    {
        IsListening = true;
        // 🎓 Pédagogie PFE : Compilation Conditionnelle (#if ANDROID)
        // Le code entre #if ANDROID et #endif ne sera compilé QUE pour Android.
        // Sur Windows ou iOS, ce bloc est totalement ignoré par le compilateur.
        // C'est ainsi qu'on écrit du code multi-plateforme dans .NET MAUI.
#if ANDROID
        // S'abonner aux scans physiques venant du lecteur Zebra
        LeoniRFID.Platforms.Android.DataWedgeIntentReceiver.TagReceived += OnTagReceived;
#endif
    }

    public void StopListening()
    {
        IsListening = false;
#if ANDROID
        LeoniRFID.Platforms.Android.DataWedgeIntentReceiver.TagReceived -= OnTagReceived;
#endif
    }

    private void OnTagReceived(object? sender, string epc)
    {
        TagScanned?.Invoke(this, epc.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Simulate an RFID scan — use for testing on non-Zebra devices.
    /// </summary>
    public void SimulateScan(string epc)
    {
        if (!string.IsNullOrWhiteSpace(epc))
            TagScanned?.Invoke(this, epc.Trim().ToUpperInvariant());
    }
}
