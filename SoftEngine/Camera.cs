namespace SoftEngine
{
    public class Camera
    {
        public SharpDX.Vector3 Position { get; set; }
        public SharpDX.Vector3 Target { get; set; }
    }

    public struct Face
    {
        public int A;
        public int B;
        public int C;
    }

    public struct Vertex
    {
        public SharpDX.Vector3 Coordinates;
        public SharpDX.Vector3 Normal;
        public SharpDX.Vector3 WorldCoordinates;
        public SharpDX.Vector2 TextureCoordinates; 
    }

    public class Mesh
    {
        public string Name { get; set; }
        public SharpDX.Vector3[] Vertices { get; set; }
        public SharpDX.Vector3[] Normals { get; set; }
        public SharpDX.Vector2[] TextureCoordinates { get; set; }
        public Face[] Faces { get; set; }
        public SharpDX.Vector3 Position { get; set; }
        public SharpDX.Vector3 Rotation { get; set; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new SharpDX.Vector3[verticesCount];
            Normals = new SharpDX.Vector3[verticesCount];
            TextureCoordinates = new SharpDX.Vector2[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }
    }
}