/// <summary>
/// Eine Komponente, deren Zustand im Spielstand gespeichert wird.
/// Das GameObject braucht zusätzlich eine <see cref="SaveableEntity"/> (eindeutige ID).
///
/// Der Zustand wird als JSON-Text übergeben. Üblich ist ein privates
/// [Serializable] struct, das mit JsonUtility umgewandelt wird.
/// </summary>
public interface ISaveable
{
    string CaptureState();

    void RestoreState(string state);
}

/// <summary>
/// Wird aufgerufen, nachdem ALLE Zustände eines Spielstands wiederhergestellt wurden.
/// Für Logik, die vom Zustand anderer Objekte abhängt (z.B. Musik nach Boss-Sieg).
/// </summary>
public interface ISaveLoadListener
{
    void OnGameLoaded();
}
