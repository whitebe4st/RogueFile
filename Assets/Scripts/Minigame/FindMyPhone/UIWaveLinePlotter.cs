using UnityEngine;
using UnityEngine.UI.Extensions;

public class UIWaveLinePlotter : MonoBehaviour
{
    public UILineRenderer targetLine;
    public UILineRenderer playerLine;

    [Range(32, 1024)] public int samples = 256;
    [Range(0.05f, 0.49f)] public float yScale = 0.35f;

    public void Draw(float[] target, float[] player)
    {
        int n = Mathf.Min(samples, target.Length, player.Length);

        targetLine.Points = BuildPoints(target, n);
        playerLine.Points = BuildPoints(player, n);

        targetLine.SetAllDirty();
        playerLine.SetAllDirty();
    }

    Vector2[] BuildPoints(float[] wave, int n)
    {
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float x = i / (float)(n - 1);        // 0..1 (ใช้กับ Relative Size)
            float y = 0.5f + wave[i] * yScale;   // กลางกรอบ
            pts[i] = new Vector2(x, y);
        }
        return pts;
    }

    public void ShowLines(bool show)
    {
        if (targetLine) targetLine.gameObject.SetActive(show);
        if (playerLine) playerLine.gameObject.SetActive(show);
    }

    public void ClearLines()
    {
        if (targetLine)
        {
            targetLine.Points = new Vector2[0];
            targetLine.SetAllDirty();
        }
        if (playerLine)
        {
            playerLine.Points = new Vector2[0];
            playerLine.SetAllDirty();
        }
    }
}
