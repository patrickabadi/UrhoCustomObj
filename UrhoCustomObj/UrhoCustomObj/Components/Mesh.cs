using UrhoCustomObj.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;
using SkiaSharp;
using Urho.Urho2D;
using Urho;
using System.Linq;

namespace UrhoCustomObj.Components
{
    public class Mesh
    {
        public class Triangle
        {
            public uint I1, In1, I2, In2, I3, In3;
            public uint Iu1, Iu2, Iu3;

            public bool IsNormalAligned => I1 == In1 && I2 == In2 && I3 == In3;

            public Triangle(uint i1, uint iu1, uint in1, uint i2, uint iu2, uint in2, uint i3, uint iu3, uint in3)
            {
                I1 = i1;
                Iu1 = iu1;
                In1 = in1;
                I2 = i2;
                Iu2 = iu2;
                In2 = in2;
                I3 = i3;
                Iu3 = iu3;
                In3 = in3;
            }
        }

        public List<Vector3> Vertices { get; set; }

        public List<Vector2> UV { get; set; }

        public List<Vector3> Normals { get; set; }

        public List<Color> Colors { get; set; }

        public List<Triangle> Triangles { get; set; }

        public Mesh()
        {
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Colors = new List<Color>();
            Triangles = new List<Triangle>();
            UV = new List<Vector2>();
        }

        public bool Load(string filename, bool fromResource)
        {
            Vertices.Clear();
            Normals.Clear();
            Colors.Clear();
            Triangles.Clear();
            UV.Clear();

            try
            {
                using (var t = new ScopeTimer("Mesh.Load"))
                {
                    if (fromResource)
                    {
                        var assembly = typeof(Mesh).GetTypeInfo().Assembly;
                        using (var fileStream = assembly.GetManifestResourceStream(filename))
                        {
                            LoadMeshStream(fileStream);
                        }
                    }
                    else
                    {
                        if (File.Exists(filename) == false)
                            return false;

                        using (var fileStream = File.OpenRead(filename))
                        {
                            LoadMeshStream(fileStream);
                        }
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("Mesh.Load exp: " + exp);
                return false;
            }

            Debug.WriteLine($"Loaded with {Vertices.Count} Vertices, {Normals.Count} Normals, {Colors.Count} Colors, {Triangles.Count} Triangles");

            return true;
        }

        private void LoadMeshStream(Stream fileStream)
        {
            int triangleMode = -1;
            int ret;

            var alignedIndexes = true;
            var scan = new ScanFormatted();

            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 4096))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.Length < 2)
                        continue;

                    if (line[0] == 'v')
                    {
                        if (line[1] == 'n')
                        {
                            ret = scan.Parse(line, "vn %f %f %f");
                            if (ret > 0)
                            {
                                Normals.Add(new Vector3((float)scan.Results[0], (float)scan.Results[1], (float)scan.Results[2]));
                            }
                        }
                        else if (line[1] == 't')
                        {
                            ret = scan.Parse(line, "vt %f %f");
                            if (ret > 0)
                            {
                                UV.Add(new Vector2((float)scan.Results[0], (float)scan.Results[1]));
                            }
                        }
                        else if (line[1] == ' ')
                        {
                            ret = scan.Parse(line, "v %f %f %f %f %f %f");
                            if (ret > 0)
                            {
                                Vertices.Add(new Vector3((float)scan.Results[0], (float)scan.Results[1], (float)scan.Results[2]));
                            }
                            if (ret == 6)
                            {
                                Colors.Add(new Color((float)scan.Results[3], (float)scan.Results[4], (float)scan.Results[5]));
                            }
                        }
                    }
                    else if (line[0] == 'f')
                    {
                        Triangle tri = null;

                        // triangleMode
                        // -1: unknown so try stuff
                        // 1: vertex and uv included
                        // 2: vertex and normal included
                        // 3: vertex and uv and normal included
                        switch (triangleMode)
                        {
                            case -1:
                                // trying vertex and normal only
                                ret = scan.Parse(line, "f %u//%u %u//%u %u//%u");
                                if (ret == 6)
                                {
                                    triangleMode = 2;
                                    tri = new Triangle
                                        ((uint)scan.Results[0] - 1,
                                        0,
                                        (uint)scan.Results[1] - 1,
                                        (uint)scan.Results[2] - 1,
                                        0,
                                        (uint)scan.Results[3] - 1,
                                        (uint)scan.Results[4] - 1,
                                        0,
                                        (uint)scan.Results[5] - 1);
                                }
                                else
                                {
                                    // trying vertex and uv only
                                    ret = scan.Parse(line, "f %u/%u %u/%u %u/%u");
                                    if(ret == 6)
                                    {
                                        triangleMode = 1;
                                        tri = new Triangle
                                            ((uint)scan.Results[0] - 1,
                                            (uint)scan.Results[1] - 1,
                                            0,
                                            (uint)scan.Results[2] - 1,
                                            (uint)scan.Results[3] - 1,
                                            0,
                                            (uint)scan.Results[4] - 1,
                                            (uint)scan.Results[5] - 1,
                                            0);
                                    }
                                    else
                                    {
                                        // default to the whole thing
                                        triangleMode = 3;
                                        ret = scan.Parse(line, "f %u/%u/%u %u/%u/%u %u/%u/%u");
                                        if(ret == 9)
                                        {
                                            tri = new Triangle
                                            ((uint)scan.Results[0] - 1,
                                            (uint)scan.Results[1] - 1,
                                            (uint)scan.Results[2] - 1,
                                            (uint)scan.Results[3] - 1,
                                            (uint)scan.Results[4] - 1,
                                            (uint)scan.Results[5] - 1,
                                            (uint)scan.Results[6] - 1,
                                            (uint)scan.Results[7] - 1,
                                            (uint)scan.Results[8] - 1);
                                        }
                                    }
                                }
                                break;
                            case 1:
                                ret = scan.Parse(line, "f %u/%u %u/%u %u/%u");
                                if (ret == 6)
                                {
                                    tri = new Triangle
                                        ((uint)scan.Results[0] - 1,
                                        (uint)scan.Results[1] - 1,
                                        0,
                                        (uint)scan.Results[2] - 1,
                                        (uint)scan.Results[3] - 1,
                                        0,
                                        (uint)scan.Results[4] - 1,
                                        (uint)scan.Results[5] - 1,
                                        0);
                                }
                                break;
                            case 2:
                                ret = scan.Parse(line, "f %u//%u %u//%u %u//%u");
                                if (ret == 6)
                                {
                                    tri = new Triangle
                                        ((uint)scan.Results[0] - 1,
                                        0,
                                        (uint)scan.Results[1] - 1,
                                        (uint)scan.Results[2] - 1,
                                        0,
                                        (uint)scan.Results[3] - 1,
                                        (uint)scan.Results[4] - 1,
                                        0,
                                        (uint)scan.Results[5] - 1);
                                }
                                break;
                            case 3:
                                ret = scan.Parse(line, "f %u/%u/%u %u/%u/%u %u/%u/%u");
                                if (ret == 9)
                                {
                                    tri = new Triangle
                                    ((uint)scan.Results[0] - 1,
                                    (uint)scan.Results[1] - 1,
                                    (uint)scan.Results[2] - 1,
                                    (uint)scan.Results[3] - 1,
                                    (uint)scan.Results[4] - 1,
                                    (uint)scan.Results[5] - 1,
                                    (uint)scan.Results[6] - 1,
                                    (uint)scan.Results[7] - 1,
                                    (uint)scan.Results[8] - 1);
                                }
                                break;
                            default:
                                break;
                        }

                        if(tri != null)
                        {
                            if (!tri.IsNormalAligned)
                                alignedIndexes = false;

                            Triangles.Add(tri);
                        }                        
                    }
                }
            }

            // I'm sure I could do this part better where I could preserve all the data and just add new vertices instead
            if (!alignedIndexes)
            {
                AlignNormals();
            }

            Debug.WriteLine($"Loaded with {Vertices.Count} Vertices, {Normals.Count} Normals, {Colors.Count} Colors, {Triangles.Count} Triangles");

        }

        private void AlignNormals()
        {
            using (var t = new ScopeTimer("Mesh.AlignNormals"))
            {
                var alignedNormals = new Vector3[Vertices.Count];

                foreach (var tri in Triangles)
                {
                    UpdateNormal(ref alignedNormals, (int)tri.I1, (int)tri.In1);
                    UpdateNormal(ref alignedNormals, (int)tri.I2, (int)tri.In2);
                    UpdateNormal(ref alignedNormals, (int)tri.I3, (int)tri.In3);
                }

                Normals = alignedNormals.ToList();
            }
        }

        private void UpdateNormal(ref Vector3[] alignedNormals, int vIndex, int nIndex)
        {
            var n = Normals[nIndex];

            var an = alignedNormals[vIndex];
            if (an != null)
            {
                // average the two
                an = (an + n) * 0.5f;
                an.NormalizeFast();
            }
            else
            {
                an = n;
            }

            alignedNormals[vIndex] = an;
        }

        public Urho.Urho2D.Texture2D LoadTexture(string filename, bool fromResource)
        {
            SKBitmap bitmap;

            if(fromResource)
            {
                var assembly = typeof(Mesh).GetTypeInfo().Assembly;
                using (var fileStream = assembly.GetManifestResourceStream(filename))
                {
                    using (var skStream = new SKManagedStream(fileStream))
                    {
                        bitmap = SKBitmap.Decode(skStream);
                    }
                }                
            }
            else
            {
                bitmap = SKBitmap.Decode(filename);
            }

            byte[] skiaImgBytes;
            using (var flipped = new SKBitmap(bitmap.Width, bitmap.Height))
            {
                using (var canvas = new SKCanvas(flipped))
                {
                    canvas.Scale(1.0f, -1.0f, (float)bitmap.Width / 2f, (float)bitmap.Height / 2f); // flip image along y-axis
                    canvas.DrawBitmap(bitmap, 0, 0);
                    canvas.Flush();

                    var image = SKImage.FromBitmap(flipped);
                    var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
                    skiaImgBytes = data.ToArray();
                }
            }

            // Create UrhoSharp Texture2D      
            Texture2D text = new Texture2D();
            text.Load(new MemoryBuffer(skiaImgBytes));

            return text;
        }

        public Urho.VertexBuffer.PositionNormalColor[] GetVertextData()
        {
            var data = new Urho.VertexBuffer.PositionNormalColor[Vertices.Count];

            for(int i=0; i<Vertices.Count; i++)
            {
                var v = Vertices[i];
                var n = Normals[i];

                Urho.Color clr = Urho.Color.Green;
                if (Vertices.Count == Colors.Count)
                {
                    var c = Colors[i];
                    clr = Urho.Color.FromByteFormat((byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255), 255);
                }

                var d = new Urho.VertexBuffer.PositionNormalColor();

                d.Position = new Urho.Vector3(v.X, v.Y, v.Z);
                d.Normal = new Urho.Vector3(n.X, n.Y, n.Z);
                d.Color = clr.ToUInt();
  
                data[i] = d;
            }

            return data;
        }

        public Urho.VertexBuffer.PositionNormalColorTexcoord[] GetTexturedVertextData()
        {
            var data = new Urho.VertexBuffer.PositionNormalColorTexcoord[Vertices.Count];

            for (int i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                var n = Normals[i];
                var t = UV[i];

                //Urho.Color clr = Urho.Color.Green;
                //if (Vertices.Count == Colors.Count)
                //{
                //    var c = Colors[i];
                //    clr = Urho.Color.FromByteFormat((byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255), 255);
                //}

                var d = new Urho.VertexBuffer.PositionNormalColorTexcoord();

                d.Position = new Urho.Vector3(v.X, v.Y, v.Z);
                d.Normal = new Urho.Vector3(n.X, n.Y, n.Z);
                d.Color = 0;
                d.TexCoord = new Urho.Vector2(t.X, t.Y);

                data[i] = d;
            }

            return data;
        }

        public uint[] GetIndexData()
        {
            var data = new uint[3 * Triangles.Count];

            for(int i=0; i<Triangles.Count; i++)
            {
                int idx = 3 * i;

                data[idx + 0] = Triangles[i].I1;
                data[idx + 1] = Triangles[i].I2;
                data[idx + 2] = Triangles[i].I3;
            }

            return data;
        }

        public Urho.BoundingBox GetBoundingBox()
        {
            float minx, miny, minz, maxx, maxy, maxz;

            minx = miny = minz = float.MaxValue;
            maxx = maxy = maxz = float.MinValue;

            foreach(var v in Vertices)
            {
                minx = Math.Min(minx, v.X);
                miny = Math.Min(miny, v.Y);
                minz = Math.Min(minz, v.Z);
                maxx = Math.Max(maxx, v.X);
                maxy = Math.Max(maxy, v.Y);
                maxz = Math.Max(maxz, v.Z);
            }

            return new Urho.BoundingBox(
                new Urho.Vector3(minx, miny, minz), 
                new Urho.Vector3(maxx, maxy, maxz));
        }
    }
}
