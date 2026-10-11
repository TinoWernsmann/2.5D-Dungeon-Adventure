using UnityEngine;

public static class BillboardUtility
{
    /// <summary>
    /// Richtet ein Sprite parallel zur Kamera-Ebene aus.
    /// Die sichtbare Vorderseite eines SpriteRenderers zeigt in lokale -Z Richtung.
    /// Deshalb muss transform.forward VON der Kamera weg zeigen –
    /// LookRotation(richtungZurKamera) würde die Rückseite zeigen (Sprite gespiegelt).
    /// </summary>
    public static void FaceCamera(
        Transform billboard,
        Camera camera,
        bool keepUpright = true)
    {
        if (billboard == null || camera == null)
        {
            return;
        }

        Vector3 viewDirection =
            camera.transform.forward;

        if (keepUpright)
        {
            viewDirection.y = 0f;
        }

        if (viewDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        billboard.rotation =
            Quaternion.LookRotation(
                viewDirection.normalized,
                Vector3.up
            );
    }
}
