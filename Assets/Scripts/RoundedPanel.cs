using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// Rounded backgrounds without any external sprites or textures, with an
    /// optional soft drop shadow drawn in the same mesh (no extra objects).
    /// </summary>
    [AddComponentMenu("UI/Fruit Tree/Rounded Panel")]
    public sealed class RoundedPanel : MaskableGraphic
    {
        private const int CornerSteps = 7;
        [SerializeField, Range(0f, 100f)] private float cornerRadius = 36f;
        [SerializeField] private Color shadowColor = Color.clear;
        [SerializeField] private Vector2 shadowOffset = new Vector2(0f, -6f);

        public float CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        /// <summary>A transparent colour (the default) draws no shadow.</summary>
        public Color ShadowColor
        {
            get => shadowColor;
            set { shadowColor = value; SetVerticesDirty(); }
        }

        public Vector2 ShadowOffset
        {
            get => shadowOffset;
            set { shadowOffset = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            float radius = Mathf.Min(cornerRadius, rect.width * .5f, rect.height * .5f);
            if (shadowColor.a > .001f)
            {
                // Two layers: a wider faint halo and a tighter darker core read as soft.
                Color halo = shadowColor;
                halo.a *= .45f;
                Rect wide = rect;
                wide.position += shadowOffset;
                wide.xMin -= 3f; wide.xMax += 3f; wide.yMin -= 4f;
                AddShape(helper, wide, radius + 3f, halo);
                Rect core = rect;
                core.position += shadowOffset * .6f;
                AddShape(helper, core, radius, shadowColor);
            }
            AddShape(helper, rect, radius, color);
        }

        private static void AddShape(VertexHelper helper, Rect rect, float radius, Color tint)
        {
            int first = helper.currentVertCount;
            var mid = UIVertex.simpleVert;
            mid.color = tint;
            mid.position = rect.center;
            helper.AddVert(mid);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 pivot;
                switch (corner)
                {
                    case 0: pivot = new Vector2(rect.xMax - radius, rect.yMin + radius); break;
                    case 1: pivot = new Vector2(rect.xMax - radius, rect.yMax - radius); break;
                    case 2: pivot = new Vector2(rect.xMin + radius, rect.yMax - radius); break;
                    default: pivot = new Vector2(rect.xMin + radius, rect.yMin + radius); break;
                }
                for (int step = 0; step <= CornerSteps; step++)
                {
                    float radians = (corner * 90f - 90f + step * (90f / CornerSteps)) * Mathf.Deg2Rad;
                    var vertex = UIVertex.simpleVert;
                    vertex.color = tint;
                    vertex.position = pivot + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                    helper.AddVert(vertex);
                }
            }
            int perimeter = 4 * (CornerSteps + 1);
            for (int i = 1; i <= perimeter; i++)
                helper.AddTriangle(first, first + i, first + i % perimeter + 1);
        }
    }
}
