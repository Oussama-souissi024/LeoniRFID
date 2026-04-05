namespace LeoniRFID.Services;

// 🎓 Pédagogie PFE : Le Principe d'Abstraction (Interface)
// Une interface ("I" devant le nom) définit un CONTRAT : elle dit QUOI faire, mais pas COMMENT.
// Cela permet de remplacer facilement l'implémentation réelle (lecteur Zebra physique)
// par une simulation (pour tester sur un PC sans lecteur RFID).
// C'est le principe d'Inversion de Dépendance (le "D" de SOLID).
public interface IRfidService
{
    // Événement déclenché automatiquement quand un tag RFID est détecté
    event EventHandler<string> TagScanned;
    bool IsListening { get; }
    void StartListening();
    void StopListening();
    void SimulateScan(string epc);  // For development/testing
}
