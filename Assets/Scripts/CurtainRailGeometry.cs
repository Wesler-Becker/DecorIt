using System.Collections.Generic;
using UnityEngine;

// Metres. X runs along the rail; the channel opens towards -Y.
internal static class CurtainRailGeometry
{
    public const float Height = 0.012f;
    public const float Depth = 0.018f;
    public const float FabricGap = 0.020f;
    public const float CarrierInset = 0.002f;
    public const float HookDrop = FabricGap + CarrierInset;

    public static Mesh CreateProfile()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        const float wall = 0.0012f;
        const float lip = 0.0065f;
        Box(vertices, triangles, new Vector3(0, Height / 2 - wall / 2, 0), new Vector3(1, wall, Depth));
        for (int side = -1; side <= 1; side += 2)
        {
            Box(vertices, triangles, new Vector3(0, 0, side * (Depth / 2 - wall / 2)), new Vector3(1, Height, wall));
            Box(vertices, triangles, new Vector3(0, -Height / 2 + wall / 2, side * (Depth / 2 - lip / 2)), new Vector3(1, wall, lip));
        }
        var mesh = new Mesh { name = "Curtain rail downward C profile", hideFlags = HideFlags.HideAndDontSave };
        Apply(mesh, vertices, triangles);
        return mesh;
    }

    public static Mesh CreateBackplate()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        Box(vertices, triangles, Vector3.zero, Vector3.one);
        var mesh = new Mesh { name = "Double rail common backplate", hideFlags = HideFlags.HideAndDontSave };
        Apply(mesh, vertices, triangles);
        return mesh;
    }

    public static void Hook(List<Vector3> vertices, List<int> triangles, Matrix4x4 frame, Vector3 fabricPoint)
    {
        // Mushroom head, short neck and a closed D-shaped plastic eyelet.
        Tube(vertices, triangles, frame, new[] { new Vector3(0, -.0005f, 0), new Vector3(0, .0025f, 0) }, .0068f, 12);
        Tube(vertices, triangles, frame, new[] { new Vector3(0, -.0005f, 0), new Vector3(0, -.0065f, 0) }, .0018f, 6);
        var curve = new[] {
            new Vector3(-.0032f,-.0065f,0), new Vector3(.0032f,-.0065f,0),
            new Vector3(.00433f,-.007f,0), new Vector3(.0048f,-.0081f,0),
            new Vector3(.0048f,-.0155f,0), new Vector3(.003394f,-.018894f,0),
            new Vector3(.001837f,-.019935f,0), new Vector3(0,-.0203f,0),
            new Vector3(-.001837f,-.019935f,0), new Vector3(-.003394f,-.018894f,0),
            new Vector3(-.0048f,-.0155f,0), new Vector3(-.0048f,-.0081f,0),
            new Vector3(-.00433f,-.007f,0)
        };
        Tube(vertices, triangles, frame, curve, .0015f, 6, true);
        Vector3 bottom = frame.MultiplyPoint3x4(new Vector3(0, -HookDrop, 0));
        if ((bottom - fabricPoint).sqrMagnitude > .000001f)
            Tube(vertices, triangles, Matrix4x4.identity, new[] { bottom, fabricPoint }, .0007f, 6);
    }

    public static void Apply(Mesh mesh, List<Vector3> vertices, List<int> triangles)
    {
        mesh.Clear();
        mesh.indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private static void Tube(List<Vector3> vertices, List<int> triangles, Matrix4x4 frame, Vector3[] path, float radius, int sides, bool closed = false)
    {
        int first = vertices.Count;
        for (int i = 0; i < path.Length; i++)
        {
            int next = closed ? (i + 1) % path.Length : Mathf.Min(i + 1, path.Length - 1);
            int previous = closed ? (i + path.Length - 1) % path.Length : Mathf.Max(0, i - 1);
            Vector3 tangent = (path[next] - path[previous]).normalized;
            Vector3 across = Vector3.Cross(tangent, Vector3.forward).normalized;
            for (int j = 0; j < sides; j++)
            {
                float angle = j * Mathf.PI * 2 / sides;
                vertices.Add(frame.MultiplyPoint3x4(path[i] + radius * (across * Mathf.Cos(angle) + Vector3.forward * Mathf.Sin(angle))));
            }
        }
        for (int i = 0; i < (closed ? path.Length : path.Length - 1); i++)
            for (int j = 0; j < sides; j++)
            {
                int a = first + i * sides + j, b = first + i * sides + (j + 1) % sides;
                int c = first + ((i + 1) % path.Length) * sides + j;
                int d = first + ((i + 1) % path.Length) * sides + (j + 1) % sides;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        for (int j = 1; !closed && j < sides - 1; j++)
        {
            triangles.Add(first); triangles.Add(first + j); triangles.Add(first + j + 1);
            int end = first + (path.Length - 1) * sides;
            triangles.Add(end); triangles.Add(end + j + 1); triangles.Add(end + j);
        }
    }

    private static void Box(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
    {
        var corners = new[] {new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
        int[] faces = {0,3,2,1, 4,5,6,7, 0,4,7,3, 1,2,6,5, 3,7,6,2, 0,1,5,4};
        for (int f = 0; f < faces.Length; f += 4)
        {
            int start = vertices.Count;
            for (int j = 0; j < 4; j++) vertices.Add(center + Vector3.Scale(corners[faces[f+j]], size) * .5f);
            triangles.Add(start); triangles.Add(start+1); triangles.Add(start+2);
            triangles.Add(start); triangles.Add(start+2); triangles.Add(start+3);
        }
    }
}
