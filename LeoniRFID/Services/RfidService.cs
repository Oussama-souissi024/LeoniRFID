namespace LeoniRFID.Services;

/// <summary>
/// RFID Service — reads EPC tags via Zebra DataWedge intent API.
/// On physical Zebra MC3300x: DataWedge broadcasts an Android intent with EPC data.
/// In development (non-Zebra): uses SimulateScan() for testing.
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
#if ANDROID
        // DataWedgeIntentReceiver registered in MainActivity / Platforms/Android
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
