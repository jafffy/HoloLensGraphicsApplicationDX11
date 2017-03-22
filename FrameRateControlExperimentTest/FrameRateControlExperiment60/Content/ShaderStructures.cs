using System.Numerics;

namespace FrameRateControlExperiment60.Content
{
    /// <summary>
    /// Constant buffer used to send hologram position transform to the shader pipeline.
    /// </summary>
    internal struct ModelConstantBuffer
    {
        public Matrix4x4 model;
    }

    /// <summary>
    /// Used to send per-vertex data to the vertex shader.
    /// </summary>
    internal struct VertexUVNormal
    {
        public VertexUVNormal(Vector3 pos, Vector2 uv, Vector3 normal)
        {
            this.pos   = pos;
            this.uv = uv;
            this.normal = normal;
        }

        public Vector3 pos;
        public Vector2 uv;
        public Vector3 normal;
    };

    internal struct TangentVertex
    {
        public TangentVertex(Vector3 pos, Vector3 normal, 
            Vector3 tangent, Vector3 binormal, Vector2 uv)
        {
            this.pos = pos;
            this.normal = normal;
            this.tangent = tangent;
            this.binormal = binormal;
            this.uv = uv;
        }

        public Vector3 pos;
        public Vector3 normal;
        public Vector3 tangent;
        public Vector3 binormal;
        public Vector2 uv;
    }

    internal struct ColoredVertex
    {
        public ColoredVertex(Vector3 pos, Vector3 color)
        {
            this.pos = pos;
            this.color = color;
        }

        public Vector3 pos;
        public Vector3 color;
    }
}
