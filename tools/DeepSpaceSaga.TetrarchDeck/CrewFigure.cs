using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DeepSpaceSaga.TetrarchDeck;

// The motion asset contains a neutral body, sampled skinned poses, a neck attachment
// and fabric swatches in bind space. Every direction uses the same 3D step.
internal sealed class CrewFigure : IDisposable
{
    private const int Width = 144, Height = 188, OriginX = 72, OriginY = 155;
    private const float Supersample = 4, ModelToTiles = 2 / 1.34f;
    private readonly Bitmap layer = new(Width, Height, PixelFormat.Format32bppPArgb);
    private readonly Vector3[][] poses;
    private readonly ushort[] faces, normalGroup;
    private readonly Vector3[] vertices, projected, normals;
    private readonly float[] lighting, depth = new float[Width * Height];
    private readonly int[] pixels = new int[Width * Height], shades;
    private readonly byte[] cloth;
    private readonly int vertexCount, texels;
    private readonly float stride;

    public CrewFigure(string file)
    {
        using var reader = new BinaryReader(File.OpenRead(file));
        if (new string(reader.ReadChars(8)) != "DSSWALK1") throw new InvalidDataException("Некорректная модель экипажа.");
        vertexCount = reader.ReadInt32();
        int triangles = reader.ReadInt32(), samples = reader.ReadInt32();
        texels = reader.ReadInt32();
        int colors = reader.ReadInt32(), groups = reader.ReadInt32();
        stride = reader.ReadSingle();
        faces = new ushort[triangles * 3];
        normalGroup = new ushort[vertexCount];
        for (int i = 0; i < faces.Length; i++) faces[i] = reader.ReadUInt16();
        for (int i = 0; i < vertexCount; i++) normalGroup[i] = reader.ReadUInt16();
        shades = new int[colors * 128];
        for (int i = 0; i < colors; i++)
        {
            int red = reader.ReadByte(), green = reader.ReadByte(), blue = reader.ReadByte();
            for (int j = 0; j < 128; j++)
            {
                float light = .52f + j * (.84f / 127);
                shades[i * 128 + j] = Color.FromArgb(Math.Min(255, (int)(red * light)),
                    Math.Min(255, (int)(green * light)), Math.Min(255, (int)(blue * light))).ToArgb();
            }
        }
        cloth = reader.ReadBytes(triangles * texels * texels);
        poses = new Vector3[samples + 1][];
        for (int f = 0; f < poses.Length; f++)
        {
            poses[f] = new Vector3[vertexCount + 1];
            for (int i = 0; i <= vertexCount; i++)
                poses[f][i] = new Vector3(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16()) / 10000;
        }
        vertices = new Vector3[vertexCount + 1];
        projected = new Vector3[vertexCount + 1];
        normals = new Vector3[groups];
        lighting = new float[vertexCount];
    }

    public void Draw(Graphics g, Bitmap heads, Walker person, PointF feet, double time)
    {
        BuildPose(person, time);
        PaintUniform();
        g.DrawImage(layer, feet.X - OriginX / Supersample, feet.Y - OriginY / Supersample,
            Width / Supersample, Height / Supersample);

        Vector3 neck = projected[vertexCount];
        var attachment = new PointF(feet.X + (neck.X - OriginX) / Supersample, feet.Y + (neck.Y - OriginY) / Supersample);
        float cos = MathF.Cos(person.Heading), sin = MathF.Sin(person.Heading);
        float side = cos - sin;
        bool rear = cos + sin < 0;
        float height = DeckPainter.HumanPixels * .182f;
        float width = height * (side < 0 ? -1 : 1);
        PointF[] dest = [new(attachment.X - width / 2, attachment.Y - height * .91f),
            new(attachment.X + width / 2, attachment.Y - height * .91f),
            new(attachment.X - width / 2, attachment.Y + height * .09f)];
        g.DrawImage(heads, dest, new Rectangle(person.HeadColumn * 64, rear ? 64 : 0, 64, 64), GraphicsUnit.Pixel);
    }

    private void BuildPose(Walker person, double time)
    {
        var neutral = poses[0];
        int count = poses.Length - 1;
        float frame = person.Distance / stride * count;
        int whole = (int)MathF.Floor(frame);
        var a = poses[1 + whole % count];
        var b = poses[1 + (whole + 1) % count];
        float fraction = frame - whole;
        float weight = person.WalkWeight;
        float angle = (float)(time * (1.61 + .057 * (person.Index % 5)) + person.Index * 2.39996);
        float sway = (1 - weight) * (.021f * MathF.Sin(angle) + .004f * MathF.Sin(angle * .49f));
        float breath = (1 - weight) * .006f * MathF.Sin(angle * 1.34f);
        float cos = MathF.Cos(person.Heading), sin = MathF.Sin(person.Heading);
        float lowest = float.MaxValue;
        for (int i = 0; i <= vertexCount; i++)
        {
            var p = Vector3.Lerp(neutral[i], Vector3.Lerp(a[i], b[i], fraction), weight);
            float upper = Math.Clamp((neutral[i].Z - .16f) / .99f, 0, 1);
            p.Y *= person.Female ? 1.02f - .12f * upper : 1.10f;
            p.Y += sway * upper;
            p.Z += breath * upper * upper;
            vertices[i] = p;
            if (i < vertexCount) lowest = Math.Min(lowest, p.Z);
        }
        Array.Clear(normals);
        for (int i = 0; i < faces.Length; i += 3)
        {
            int j = faces[i], k = faces[i + 1], l = faces[i + 2];
            var n = Vector3.Cross(vertices[k] - vertices[j], vertices[l] - vertices[j]);
            normals[normalGroup[j]] += n;
            normals[normalGroup[k]] += n;
            normals[normalGroup[l]] += n;
        }
        var light = new Vector3(-.37f * cos + .60f * sin, -.37f * sin - .60f * cos, .71f);
        for (int i = 0; i <= vertexCount; i++)
        {
            var p = vertices[i];
            float x = p.X * cos + p.Y * sin, y = p.X * sin - p.Y * cos, z = p.Z - lowest;
            projected[i] = new(OriginX + (x - y) * (16 * ModelToTiles * Supersample),
                OriginY + ((x + y) * 8 - z * 17.5f) * (ModelToTiles * Supersample), x + y + z * .914f);
            if (i == vertexCount) continue;
            var n = normals[normalGroup[i]];
            float dot = Vector3.Dot(n, light) / MathF.Sqrt(Math.Max(n.LengthSquared(), 1e-12f));
            lighting[i] = Math.Clamp(41 + 86 * dot, 0, 127);
        }
    }

    private void PaintUniform()
    {
        Array.Clear(pixels);
        Array.Fill(depth, float.NegativeInfinity);
        for (int i = 0; i < faces.Length; i += 3)
        {
            int ia = faces[i], ib = faces[i + 1], ic = faces[i + 2];
            Vector3 a = projected[ia], ab = projected[ib] - a, ac = projected[ic] - a;
            float area = ab.X * ac.Y - ac.X * ab.Y;
            if (Math.Abs(area) < .015f) continue;
            int minX = Math.Max(0, (int)MathF.Floor(Math.Min(a.X, Math.Min(a.X + ab.X, a.X + ac.X))));
            int maxX = Math.Min(Width - 1, (int)MathF.Ceiling(Math.Max(a.X, Math.Max(a.X + ab.X, a.X + ac.X))));
            int minY = Math.Max(0, (int)MathF.Floor(Math.Min(a.Y, Math.Min(a.Y + ab.Y, a.Y + ac.Y))));
            int maxY = Math.Min(Height - 1, (int)MathF.Ceiling(Math.Max(a.Y, Math.Max(a.Y + ab.Y, a.Y + ac.Y))));
            float du = ac.Y / area, dv = -ab.Y / area;
            float lu = lighting[ib] - lighting[ia], lv = lighting[ic] - lighting[ia];
            int tex = i / 3 * texels * texels;
            for (int y = minY; y <= maxY; y++)
            {
                float dy = y + .5f - a.Y;
                float u = (minX + .5f - a.X) * du - ac.X / area * dy;
                float v = (minX + .5f - a.X) * dv + ab.X / area * dy;
                for (int x = minX; x <= maxX; x++, u += du, v += dv)
                {
                    if (u < 0 || v < 0 || u + v > 1) continue;
                    int index = y * Width + x;
                    float z = a.Z + u * ab.Z + v * ac.Z;
                    if (z <= depth[index]) continue;
                    depth[index] = z;
                    int tx = (int)(u * (texels - 1) + .5f), ty = (int)(v * (texels - 1) + .5f);
                    int color = cloth[tex + ty * texels + tx];
                    pixels[index] = shades[color * 128 + (int)(lighting[ia] + lu * u + lv * v)];
                }
            }
        }
        var data = layer.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try { Marshal.Copy(pixels, 0, data.Scan0, pixels.Length); }
        finally { layer.UnlockBits(data); }
    }

    public void Dispose() => layer.Dispose();
}
