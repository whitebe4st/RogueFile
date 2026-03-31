// ╔════════════════════════════════════════════════════════════════╗
// ║  LEGACY FILE — kept for RPSGesture enum only.                ║
// ║  Gesture detection is now in SentisGestureDetector.cs        ║
// ╚════════════════════════════════════════════════════════════════╝
using UnityEngine;

namespace DetectiveGame.HandTracking
{
    public enum RPSGesture
    {
        None,
        Rock,      // 0 fingers extended
        Scissors,  // 2 fingers extended (index + middle)
        Paper      // 5 fingers extended
    }
}
