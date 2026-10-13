using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Four quiet, low-cost garden silhouettes. Each theme is combined into the
    /// same static scenery mesh; no extra renderers, textures or colliders.
    /// All props stay beyond the picking area of the central fruit tree.
    /// </summary>
    public static class EnvironmentGardenBuilder
    {
        public static void Japanese(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(30f, -.16f, 1.8f, 36, .45f,
                new Color(.64f, .75f, .66f));
            mesh.RingPath(10.6f, .43f, 36, new Color(.79f, .75f, .67f));
            Pond(mesh, 14.8f, 0f, new Color(.63f, .84f, .82f));
            // A quiet footbridge, with a pair of stone garden lanterns.
            mesh.Box(new Vector3(14.8f, .38f, 0f),
                new Vector3(1.3f, .18f, 4.6f), new Color(.65f, .50f, .37f));
            mesh.Box(new Vector3(14.05f, .70f, 0f),
                new Vector3(.10f, .65f, 4.7f), new Color(.55f, .40f, .31f));
            mesh.Box(new Vector3(15.55f, .70f, 0f),
                new Vector3(.10f, .65f, 4.7f), new Color(.55f, .40f, .31f));
            Lantern(mesh, -13f, 4f, false);
            Lantern(mesh, -12f, -5f, false);
            for (int i = 0; i < 11; i++)
            {
                float a = (i + .2f) * Mathf.PI * 2f / 11f;
                Tree(mesh, a, 21f, new Color(.72f, .74f, .62f),
                    i % 3 == 0 ? new Color(.92f, .72f, .79f) :
                        new Color(.55f, .73f, .55f));
            }
        }

        public static void Chinese(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(32f, -.13f, 2f, 40, 1.1f,
                new Color(.72f, .74f, .64f));
            mesh.RingPath(11.3f, .56f, 40, new Color(.81f, .75f, .61f));
            // A distant round moon gate and two small pagoda-roof silhouettes.
            Color stone = new Color(.85f, .79f, .67f);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                mesh.Box(new Vector3(Mathf.Sin(a) * 1.95f,
                    2.2f + Mathf.Cos(a) * 1.95f, 20f),
                    new Vector3(.52f, .52f, .55f), stone);
            }
            Pavilion(mesh, -18f, -8f);
            Pavilion(mesh, 18f, 8f);
            for (int i = 0; i < 10; i++)
            {
                float a = (i + .33f) * Mathf.PI * 2f / 10f;
                Tree(mesh, a, 24f, new Color(.55f, .61f, .37f),
                    new Color(.51f, .72f, .40f));
            }
        }

        public static void Lakeside(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(32f, -.22f, 1.35f, 40, .2f,
                new Color(.60f, .74f, .70f));
            // Shallow shoreline beyond the play area, visible at every orbit.
            mesh.RingPath(19f, 3.5f, 56, new Color(.52f, .79f, .82f));
            mesh.RingPath(15.4f, .42f, 56, new Color(.83f, .86f, .69f));
            for (int i = 0; i < 26; i++)
            {
                float a = (i + .18f) * Mathf.PI * 2f / 26f;
                float r = 14.35f + (i % 3) * .18f;
                mesh.Cone(new Vector3(Mathf.Cos(a) * r, .025f,
                    Mathf.Sin(a) * r), .07f, .9f + (i % 4) * .15f, 4,
                    new Color(.48f, .67f, .43f));
            }
            for (int i = 0; i < 9; i++)
                Tree(mesh, (i + .44f) * Mathf.PI * 2f / 9f, 26f,
                    new Color(.37f, .55f, .43f), new Color(.49f, .67f, .54f));
        }

        public static void Evening(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(30f, -.12f, 2.25f, 38, .75f,
                new Color(.47f, .46f, .60f));
            mesh.RingPath(10.8f, .58f, 44, new Color(.70f, .59f, .55f));
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Lantern(mesh, Mathf.Cos(a) * 13f, Mathf.Sin(a) * 13f, true);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = (i + .25f) * Mathf.PI * 2f / 12f;
                Tree(mesh, a, 21f, new Color(.40f, .40f, .42f),
                    new Color(.40f, .55f, .48f));
            }
        }

        private static void Tree(EnvironmentMeshBuilder mesh, float angle,
            float radius, Color trunk, Color leaves)
        {
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            mesh.Box(new Vector3(x, .77f, z),
                new Vector3(.22f, 1.54f, .22f), trunk);
            mesh.Cone(new Vector3(x, 1.12f, z), .91f, 2.1f, 6, leaves);
        }

        private static void Pond(EnvironmentMeshBuilder mesh,
            float x, float z, Color water)
        {
            mesh.Quad(new Vector3(x - 2.5f, .025f, z - 2.5f),
                new Vector3(x - 2.5f, .025f, z + 2.5f),
                new Vector3(x + 2.5f, .025f, z + 2.5f),
                new Vector3(x + 2.5f, .025f, z - 2.5f), water);
        }

        private static void Lantern(EnvironmentMeshBuilder mesh, float x,
            float z, bool warmLight)
        {
            Color stone = warmLight ? new Color(.40f, .39f, .42f) :
                new Color(.65f, .67f, .61f);
            mesh.Box(new Vector3(x, .78f, z),
                new Vector3(.16f, 1.55f, .16f), stone);
            mesh.Box(new Vector3(x, 1.63f, z),
                new Vector3(.36f, .36f, .36f),
                warmLight ? new Color(1f, .77f, .50f) :
                    new Color(.83f, .84f, .76f));
            mesh.Cone(new Vector3(x, 1.84f, z), .36f, .3f, 5, stone);
        }

        private static void Pavilion(EnvironmentMeshBuilder mesh, float x,
            float z)
        {
            Color wood = new Color(.57f, .38f, .31f);
            mesh.Box(new Vector3(x - 1f, 1.05f, z),
                new Vector3(.18f, 2.1f, .18f), wood);
            mesh.Box(new Vector3(x + 1f, 1.05f, z),
                new Vector3(.18f, 2.1f, .18f), wood);
            mesh.Box(new Vector3(x, 2.08f, z),
                new Vector3(2.45f, .2f, 1.8f), wood);
            mesh.Cone(new Vector3(x, 2.20f, z), 1.65f, .8f, 8,
                new Color(.67f, .38f, .32f));
        }
    }
}
