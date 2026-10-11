using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>Rounded backgrounds without any external sprites or textures.</summary>
    [AddComponentMenu("UI/Fruit Tree/Rounded Panel")]
    public sealed class RoundedPanel : MaskableGraphic
    {
        [SerializeField, Range(0f, 100f)] private float cornerRadius = 36f;
        public float CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            float radius = Mathf.Min(cornerRadius, rect.width * .5f, rect.height * .5f);
            var mid = UIVertex.simpleVert;
            mid.color = color;
            mid.position = rect.center;
            helper.AddVert(mid);

            const int cornerSteps = 7;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 pivot;
                switch (corner)
                {
                    case 0: pivot = new Vector2(rect.xMax-radius, rect.yMin+radius); break;
                    case 1: pivot = new Vector2(rect.xMax-radius, rect.yMax-radius); break;
                    case 2: pivot = new Vector2(rect.xMin+radius, rect.yMax-radius); break;
                    default: pivot = new Vector2(rect.xMin+radius, rect.yMin+radius); break;
                }
                for (int step = 0; step <= cornerSteps; step++)
                {
                    float radians = (corner * 90f - 90f + step * (90f / cornerSteps))
                        * Mathf.Deg2Rad;
                    var vertex = UIVertex.simpleVert;
                    vertex.color = color;
                    vertex.position = pivot + new Vector2(Mathf.Cos(radians),
                        Mathf.Sin(radians)) * radius;
                    helper.AddVert(vertex);
                }
            }
            int perimeter = 4 * (cornerSteps + 1);
            for (int i = 1; i <= perimeter; i++)
                helper.AddTriangle(0, i, i % perimeter + 1);
        }
    }
}
