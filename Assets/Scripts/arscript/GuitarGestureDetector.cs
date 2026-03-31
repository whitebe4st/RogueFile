// ╔════════════════════════════════════════════════════════════════╗
// ║  LEGACY FILE — kept for the GuitarChord enum only.           ║
// ║  Gesture detection is now in SentisGestureDetector.cs        ║
// ╚════════════════════════════════════════════════════════════════╝
using UnityEngine;

namespace DetectiveGame.HandTracking
{
    public enum GuitarChord
    {
        None,
        A_Major, // Open Hand (Paper)
        B_Minor, // Fist (Rock)
        C_Major, // Scissors
        D_Major, // Index
        E_Major, // Shaka
        F_Major, // 3 Fingers
        G_Major  // 4 Fingers
    }
}
