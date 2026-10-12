using System;

/// <summary>
/// Sorgt dafür, dass der Aktiv-Zustand (und ob es zerstört wurde) eines Objekts gespeichert wird.
///
/// Auf einem GameObject-Feld: das referenzierte Objekt (z.B. eine Tür, die per SetActive
/// geöffnet/geschlossen wird).
/// Auf einer Klasse: jedes Objekt mit dieser Komponente (z.B. NPCs, die nach dem Dialog verschwinden).
///
/// Das Setup-Tool (Tools → Save System → Setup Open Scene) gibt diesen Objekten
/// automatisch eine <see cref="SaveableEntity"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
public class SaveActiveStateAttribute : Attribute
{
}
