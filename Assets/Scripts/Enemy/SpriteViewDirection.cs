using UnityEngine;

/// <summary>
/// Welche Seite eines Charakters die Kamera sieht.
/// Left/Right = Blickrichtung des Charakters auf dem Bildschirm.
/// </summary>
public enum SpriteViewDirection
{
    Front,
    Back,
    Left,
    Right
}

public static class SpriteViewDirectionResolver
{
    public static SpriteViewDirection Resolve(
        Transform character,
        Vector3 viewerPosition)
    {
        Vector3 directionToViewer =
            viewerPosition - character.position;

        directionToViewer.y = 0f;

        if (directionToViewer.sqrMagnitude <= 0.001f)
        {
            return SpriteViewDirection.Front;
        }

        directionToViewer.Normalize();

        float forwardDot =
            Vector3.Dot(character.forward, directionToViewer);

        float rightDot =
            Vector3.Dot(character.right, directionToViewer);

        if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
        {
            return forwardDot >= 0f
                ? SpriteViewDirection.Front
                : SpriteViewDirection.Back;
        }

        // Steht die Kamera rechts vom Charakter,
        // läuft er auf dem Bildschirm nach rechts.
        return rightDot >= 0f
            ? SpriteViewDirection.Right
            : SpriteViewDirection.Left;
    }
}
