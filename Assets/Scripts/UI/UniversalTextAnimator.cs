using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class UniversalTextAnimator : MonoBehaviour
{
    public enum TextEffect { None, Scribble, Wave, Floating }

    [Header("General Settings")]
    public TextEffect effect = TextEffect.Scribble;
    public float speed = 10f;
    public float magnitude = 1f;

    private TMP_Text textComponent;
    private TMP_TextInfo textInfo;
    private Mesh mesh;
    private Vector3[] vertices;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        while (true)
        {
            if (effect == TextEffect.None)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            textComponent.ForceMeshUpdate();
            textInfo = textComponent.textInfo;
            mesh = textComponent.mesh;
            vertices = textInfo.meshInfo[0].vertices;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                    continue;

                int vertexIndex = charInfo.vertexIndex;
                Vector3 offset = Vector3.zero;

                switch (effect)
                {
                    case TextEffect.Scribble:
                        // Each char jitters independently
                        offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * magnitude;
                        break;

                    case TextEffect.Wave:
                        // Smooth wave effect
                        float waveY = Mathf.Sin(Time.time * speed + i * 0.5f) * magnitude * 2f;
                        offset = new Vector3(0, waveY, 0);
                        break;
                    
                    case TextEffect.Floating:
                        // Slower, individual floating
                        float floatX = Mathf.Sin(Time.time * speed * 0.5f + i) * magnitude;
                        float floatY = Mathf.Cos(Time.time * speed * 0.7f + i) * magnitude;
                        offset = new Vector3(floatX, floatY, 0);
                        break;
                }

                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] += offset;
                }
            }

            mesh.vertices = vertices;
            textComponent.canvasRenderer.SetMesh(mesh);

            if (effect == TextEffect.Scribble)
                yield return new WaitForSeconds(0.05f); // Jitter frequency
            else
                yield return null;
        }
    }
}
