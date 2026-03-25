namespace LeoniRFID.Services;

public interface IRfidService
{
    event EventHandler<string> TagScanned;
    bool IsListening { get; }
    void StartListening();
    void StopListening();
    void SimulateScan(string epc);  // For development/testing
}
