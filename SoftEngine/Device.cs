using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;

namespace SoftEngine
{
    public class Device
    {
        private byte[] backBuffer;
        private float[] zBuffer;
        private int renderWidth;
        private int renderHeight;

        public Device(int width, int height)
        {
            renderWidth = width;
            renderHeight = height;
            backBuffer = new byte[width * height * 4];
            zBuffer = new float[width * height];
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (int index = 0; index < backBuffer.Length; index += 4)
            {
                backBuffer[index] = b;
                backBuffer[index + 1] = g;
                backBuffer[index + 2] = r;
                backBuffer[index + 3] = a;
            }

            for (int index = 0; index < zBuffer.Length; index++)
            {
                zBuffer[index] = float.MaxValue;
            }
        }

        public byte[] GetBuffer()
        {
            return backBuffer;
        }

        public void PutPixel(int x, int y, float z, SharpDX.Color4 color)
        {
            if (x >= 0 && x < renderWidth && y >= 0 && y < renderHeight)
            {
                int index = x + y * renderWidth;

                if (zBuffer[index] > z)
                {
                    zBuffer[index] = z;

                    int colorIndex = index * 4;
                    backBuffer[colorIndex] = (byte)(color.Blue * 255);
                    backBuffer[colorIndex + 1] = (byte)(color.Green * 255);
                    backBuffer[colorIndex + 2] = (byte)(color.Red * 255);
                    backBuffer[colorIndex + 3] = (byte)(color.Alpha * 255);
                }
            }
        }

        public Vertex Project(Vertex vertex, SharpDX.Matrix transMatrix, SharpDX.Matrix worldMatrix)
        {
            SharpDX.Vector3 point = SharpDX.Vector3.TransformCoordinate(vertex.Coordinates, transMatrix);
            SharpDX.Vector3 worldPoint = SharpDX.Vector3.TransformCoordinate(vertex.Coordinates, worldMatrix);
            SharpDX.Vector3 transformedNormal = SharpDX.Vector3.TransformNormal(vertex.Normal, worldMatrix);

            float x = point.X * renderWidth + renderWidth / 2.0f;
            float y = -point.Y * renderHeight + renderHeight / 2.0f;

            return new Vertex
            {
                Coordinates = new SharpDX.Vector3(x, y, point.Z),
                Normal = transformedNormal,
                WorldCoordinates = worldPoint,
                TextureCoordinates = vertex.TextureCoordinates 
            };
        }

        private float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Math.Clamp(gradient, 0.0f, 1.0f);
        }

        public float ComputeIllumination(SharpDX.Vector3 vertexPosition, SharpDX.Vector3 normal, SharpDX.Vector3 lightPosition)
        {
            SharpDX.Vector3 lightDirection = lightPosition - vertexPosition;
            lightDirection.Normalize();
            normal.Normalize();

            float cosTheta = SharpDX.Vector3.Dot(normal, lightDirection);
            return Math.Max(0, cosTheta);
        }

        private void ProcessScanLine(int y, Vertex va, Vertex vb, Vertex vc, Vertex vd, SharpDX.Vector3 lightPos, Texture texture)
        {
            var gradient1 = va.Coordinates.Y != vb.Coordinates.Y ? (y - va.Coordinates.Y) / (vb.Coordinates.Y - va.Coordinates.Y) : 1;
            var gradient2 = vc.Coordinates.Y != vd.Coordinates.Y ? (y - vc.Coordinates.Y) / (vd.Coordinates.Y - vc.Coordinates.Y) : 1;

            int sx = (int)Interpolate(va.Coordinates.X, vb.Coordinates.X, gradient1);
            int ex = (int)Interpolate(vc.Coordinates.X, vd.Coordinates.X, gradient2);

            float z1 = Interpolate(va.Coordinates.Z, vb.Coordinates.Z, gradient1);
            float z2 = Interpolate(vc.Coordinates.Z, vd.Coordinates.Z, gradient2);

            float u1 = Interpolate(va.TextureCoordinates.X, vb.TextureCoordinates.X, gradient1);
            float u2 = Interpolate(vc.TextureCoordinates.X, vd.TextureCoordinates.X, gradient2);
            float v1 = Interpolate(va.TextureCoordinates.Y, vb.TextureCoordinates.Y, gradient1);
            float v2 = Interpolate(vc.TextureCoordinates.Y, vd.TextureCoordinates.Y, gradient2);

            float nl1 = ComputeIllumination(InterpolateVector(va.WorldCoordinates, vb.WorldCoordinates, gradient1), InterpolateVector(va.Normal, vb.Normal, gradient1), lightPos);
            float nl2 = ComputeIllumination(InterpolateVector(vc.WorldCoordinates, vd.WorldCoordinates, gradient2), InterpolateVector(vc.Normal, vd.Normal, gradient2), lightPos);

            if (sx > ex)
            {
                var temp = sx; sx = ex; ex = temp;
                var tempZ = z1; z1 = z2; z2 = tempZ;
                var tempNl = nl1; nl1 = nl2; nl2 = tempNl;
                var tempU = u1; u1 = u2; u2 = tempU;
                var tempV = v1; v1 = v2; v2 = tempV;
            }

            for (int x = sx; x < ex; x++)
            {
                float gradient = (float)(x - sx) / (ex - sx);

                float z = Interpolate(z1, z2, gradient);
                float ndl = Interpolate(nl1, nl2, gradient);

                float u = Interpolate(u1, u2, gradient);
                float v = Interpolate(v1, v2, gradient);

                SharpDX.Color4 textureColor = texture != null ? texture.Map(u, v) : SharpDX.Color4.White;

                float intensity = Math.Clamp(ndl + 0.1f, 0.0f, 1.0f);
                var finalColor = new SharpDX.Color4(textureColor.Red * intensity, textureColor.Green * intensity, textureColor.Blue * intensity, 1.0f);

                PutPixel(x, y, z, finalColor);
            }
        }

        private SharpDX.Vector3 InterpolateVector(SharpDX.Vector3 min, SharpDX.Vector3 max, float gradient)
        {
            return new SharpDX.Vector3(
                Interpolate(min.X, max.X, gradient),
                Interpolate(min.Y, max.Y, gradient),
                Interpolate(min.Z, max.Z, gradient)
            );
        }

        public void DrawTriangle(Vertex v1, Vertex v2, Vertex v3, SharpDX.Vector3 lightPos, Texture texture)
        {
            if (v1.Coordinates.Y > v2.Coordinates.Y) { var temp = v1; v1 = v2; v2 = temp; }
            if (v2.Coordinates.Y > v3.Coordinates.Y) { var temp = v2; v2 = v3; v3 = temp; }
            if (v1.Coordinates.Y > v2.Coordinates.Y) { var temp = v1; v1 = v2; v2 = temp; }

            float dP1P2, dP1P3;

            if (v2.Coordinates.Y - v1.Coordinates.Y > 0) dP1P2 = (v2.Coordinates.X - v1.Coordinates.X) / (v2.Coordinates.Y - v1.Coordinates.Y);
            else dP1P2 = 0;

            if (v3.Coordinates.Y - v1.Coordinates.Y > 0) dP1P3 = (v3.Coordinates.X - v1.Coordinates.X) / (v3.Coordinates.Y - v1.Coordinates.Y);
            else dP1P3 = 0;

            if (dP1P2 > dP1P3)
            {
                for (int y = (int)v1.Coordinates.Y; y <= (int)v3.Coordinates.Y; y++)
                {
                    if (y < v2.Coordinates.Y) ProcessScanLine(y, v1, v3, v1, v2, lightPos, texture);
                    else ProcessScanLine(y, v1, v3, v2, v3, lightPos, texture);
                }
            }
            else
            {
                for (int y = (int)v1.Coordinates.Y; y <= (int)v3.Coordinates.Y; y++)
                {
                    if (y < v2.Coordinates.Y) ProcessScanLine(y, v1, v2, v1, v3, lightPos, texture);
                    else ProcessScanLine(y, v2, v3, v1, v3, lightPos, texture);
                }
            }
        }

        public void Render(Camera camera, Mesh[] meshes, Texture texture)
        {
            SharpDX.Matrix viewMatrix = SharpDX.Matrix.LookAtLH(camera.Position, camera.Target, SharpDX.Vector3.Up);
            SharpDX.Matrix projectionMatrix = SharpDX.Matrix.PerspectiveFovLH(0.78f, (float)renderWidth / renderHeight, 0.01f, 1.0f);

            SharpDX.Vector3 lightPosition = new SharpDX.Vector3(0.0f, 10.0f, 10.0f);

            foreach (Mesh mesh in meshes)
            {
                SharpDX.Matrix worldMatrix = SharpDX.Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) * SharpDX.Matrix.Translation(mesh.Position);
                SharpDX.Matrix transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                Parallel.For(0, mesh.Faces.Length, faceIndex =>
                {
                    Face face = mesh.Faces[faceIndex];

                    Vertex vA = new Vertex { Coordinates = mesh.Vertices[face.A], Normal = mesh.Normals[face.A], TextureCoordinates = mesh.TextureCoordinates[face.A] };
                    Vertex vB = new Vertex { Coordinates = mesh.Vertices[face.B], Normal = mesh.Normals[face.B], TextureCoordinates = mesh.TextureCoordinates[face.B] };
                    Vertex vC = new Vertex { Coordinates = mesh.Vertices[face.C], Normal = mesh.Normals[face.C], TextureCoordinates = mesh.TextureCoordinates[face.C] };

                    SharpDX.Vector3 worldPointA = SharpDX.Vector3.TransformCoordinate(vA.Coordinates, worldMatrix);
                    SharpDX.Vector3 worldPointB = SharpDX.Vector3.TransformCoordinate(vB.Coordinates, worldMatrix);
                    SharpDX.Vector3 worldPointC = SharpDX.Vector3.TransformCoordinate(vC.Coordinates, worldMatrix);

                    SharpDX.Vector3 edge1 = worldPointB - worldPointA;
                    SharpDX.Vector3 edge2 = worldPointC - worldPointA;

                    SharpDX.Vector3 faceNormal = SharpDX.Vector3.Cross(edge1, edge2);
                    faceNormal.Normalize();

                    SharpDX.Vector3 cameraDirection = worldPointA - camera.Position;
                    cameraDirection.Normalize();

                    if (SharpDX.Vector3.Dot(faceNormal, cameraDirection) >= 0)
                    {
                        return; 
                    }

                    Vertex vertexA = Project(vA, transformMatrix, worldMatrix);
                    Vertex vertexB = Project(vB, transformMatrix, worldMatrix);
                    Vertex vertexC = Project(vC, transformMatrix, worldMatrix);

                    lock (backBuffer)
                    {
                        DrawTriangle(vertexA, vertexB, vertexC, lightPosition, texture);
                    }
                });
            }
        }

        public Mesh LoadOBJFromFile(string fileName)
        {
            var vertices = new System.Collections.Generic.List<SharpDX.Vector3>();
            var normalsTemp = new System.Collections.Generic.List<SharpDX.Vector3>();
            var uvsTemp = new System.Collections.Generic.List<SharpDX.Vector2>();
            var faces = new System.Collections.Generic.List<Face>();

            var finalNormalsMap = new System.Collections.Generic.Dictionary<int, SharpDX.Vector3>();
            var finalUVsMap = new System.Collections.Generic.Dictionary<int, SharpDX.Vector2>();

            string[] lines = File.ReadAllLines(fileName);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                if (parts[0] == "v")
                {
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    vertices.Add(new SharpDX.Vector3(x, y, z));
                }
                else if (parts[0] == "vt") 
                {
                    float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float v = 1.0f - float.Parse(parts[2], CultureInfo.InvariantCulture);
                    uvsTemp.Add(new SharpDX.Vector2(u, v));
                }
                else if (parts[0] == "vn")
                {
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    normalsTemp.Add(new SharpDX.Vector3(x, y, z));
                }
                else if (parts[0] == "f")
                {
                    int vA = int.Parse(parts[1].Split('/')[0]) - 1;
                    int vB = int.Parse(parts[2].Split('/')[0]) - 1;
                    int vC = int.Parse(parts[3].Split('/')[0]) - 1;

                    faces.Add(new Face { A = vA, B = vB, C = vC });

                    string[] p1Sub = parts[1].Split('/');

                    if (p1Sub.Length > 1 && !string.IsNullOrEmpty(p1Sub[1]))
                    {
                        int tA = int.Parse(p1Sub[1]) - 1;
                        int tB = int.Parse(parts[2].Split('/')[1]) - 1;
                        int tC = int.Parse(parts[3].Split('/')[1]) - 1;

                        finalUVsMap[vA] = uvsTemp[tA];
                        finalUVsMap[vB] = uvsTemp[tB];
                        finalUVsMap[vC] = uvsTemp[tC];
                    }

                    if (p1Sub.Length > 2 && !string.IsNullOrEmpty(p1Sub[2]))
                    {
                        int nA = int.Parse(p1Sub[2]) - 1;
                        int nB = int.Parse(parts[2].Split('/')[2]) - 1;
                        int nC = int.Parse(parts[3].Split('/')[2]) - 1;

                        finalNormalsMap[vA] = normalsTemp[nA];
                        finalNormalsMap[vB] = normalsTemp[nB];
                        finalNormalsMap[vC] = normalsTemp[nC];
                    }
                }
            }

            Mesh mesh = new Mesh("BlenderModel", vertices.Count, faces.Count);
            mesh.Vertices = vertices.ToArray();
            mesh.Faces = faces.ToArray();

            mesh.Normals = new SharpDX.Vector3[vertices.Count];
            mesh.TextureCoordinates = new SharpDX.Vector2[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                if (finalNormalsMap.ContainsKey(i)) mesh.Normals[i] = finalNormalsMap[i];
                if (finalUVsMap.ContainsKey(i)) mesh.TextureCoordinates[i] = finalUVsMap[i];
            }

            mesh.Position = SharpDX.Vector3.Zero;
            mesh.Rotation = SharpDX.Vector3.Zero;

            return mesh;
        }
    }
}