using UrhoCustomObj.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;

namespace UrhoCustomObj.Components
{
    public class Mesh
    {
        public class Coordinate
        {
            public float X;
            public float Y;
            public float Z;

            public Coordinate(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        public class Color
        {
            public float R;
            public float G;
            public float B;

            public Color(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
            }
        }

        public class Triangle
        {
            public uint I1, In1, I2, In2, I3, In3;

            public Triangle(uint i1, uint in1, uint i2, uint in2, uint i3, uint in3)
            {
                I1 = i1;
                In1 = in1;
                I2 = i2;
                In2 = in2;
                I3 = i3;
                In3 = in3;
            }
        }

        public List<Coordinate> Vertices { get; set; }

        public List<Coordinate> Normals { get; set; }

        public List<Color> Colors { get; set; }

        public List<Triangle> Triangles { get; set; }

        public struct VertexData
        {
            public float vx;
            public float vy;
            public float vz;
            public float nx;
            public float ny;
            public float nz;
            public uint color;
        };

        public Mesh()
        {
            Vertices = new List<Coordinate>();
            Normals = new List<Coordinate>();
            Colors = new List<Color>();
            Triangles = new List<Triangle>();
        }

        public bool Load(string filename, bool fromResource)
        {
            Vertices.Clear();
            Normals.Clear();
            Colors.Clear();
            Triangles.Clear();

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
                            int ret = scan.Parse(line, "vn %f %f %f");
                            if (ret > 0)
                            {
                                Normals.Add(new Coordinate((float)scan.Results[0], (float)scan.Results[1], (float)scan.Results[2]));
                            }
                        }
                        else if (line[1] == ' ')
                        {
                            int ret = scan.Parse(line, "v %f %f %f %f %f %f");
                            if (ret > 0)
                            {
                                Vertices.Add(new Coordinate((float)scan.Results[0], (float)scan.Results[1], (float)scan.Results[2]));
                            }
                            if (ret == 6)
                            {
                                Colors.Add(new Color((float)scan.Results[3], (float)scan.Results[4], (float)scan.Results[5]));
                            }
                        }
                    }
                    else if (line[0] == 'f')
                    {
                        int ret = scan.Parse(line, "f %u//%u %u//%u %u//%u");
                        if (ret > 0)
                        {
                            Triangles.Add(new Triangle
                                ((uint)scan.Results[0] - 1,
                                (uint)scan.Results[1] - 1,
                                (uint)scan.Results[2] - 1,
                                (uint)scan.Results[3] - 1,
                                (uint)scan.Results[4] - 1,
                                (uint)scan.Results[5] - 1));
                        }
                    }

                }
            }
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
