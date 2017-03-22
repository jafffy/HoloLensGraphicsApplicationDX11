using System;
using System.Numerics;
using PixelShaderExperiment.Common;
using Windows.UI.Input.Spatial;

namespace PixelShaderExperiment.Content
{
    /// <summary>
    /// This sample renderer instantiates a basic rendering pipeline.
    /// </summary>
    internal class MultiPassRenderer : Disposer
    {
        private DeviceResources deviceResources;
        
        private SharpDX.Direct3D11.InputLayout inputLayout;
        private SharpDX.Direct3D11.Buffer vertexBuffer;
        private SharpDX.Direct3D11.VertexShader vertexShader;
        private SharpDX.Direct3D11.GeometryShader geometryShader;
        private SharpDX.Direct3D11.PixelShader pixelShader;
        private SharpDX.Direct3D11.Buffer modelConstantBuffer;
        
        private ModelConstantBuffer modelConstantBufferData;
        private int vertexCount = 0;
        private Vector3 position = new Vector3(0.0f, 0.0f, -2.0f);
        
        private bool loadingComplete = false;

        private bool usingVprtShaders = false;

        /// <summary>
        /// Loads vertex and pixel shaders from files and instantiates the plane geometry.
        /// </summary>
        public MultiPassRenderer(DeviceResources deviceResources)
        {
            this.deviceResources = deviceResources;

            this.CreateDeviceDependentResourcesAsync();
        }

        /// <summary>
        /// Called once per frame, rotates the cube and calculates the model and view matrices.
        /// </summary>
        public void Update(StepTimer timer)
        {
            Matrix4x4 modelTransform = Matrix4x4.Identity;
            
            this.modelConstantBufferData.model = Matrix4x4.Transpose(modelTransform);
            
            if (!loadingComplete)
            {
                return;
            }
            
            var context = this.deviceResources.D3DDeviceContext;
            
            context.UpdateSubresource(ref this.modelConstantBufferData, this.modelConstantBuffer);
        }

        /// <summary>
        /// Renders one frame using the vertex and pixel shaders.
        /// On devices that do not support the D3D11_FEATURE_D3D11_OPTIONS3::
        /// VPAndRTArrayIndexFromAnyShaderFeedingRasterizer optional feature,
        /// a pass-through geometry shader is also used to set the render 
        /// target array index.
        /// </summary>
        public void Render()
        {
            if (!this.loadingComplete)
            {
                return;
            }

            var context = this.deviceResources.D3DDeviceContext;
            
            int stride = SharpDX.Utilities.SizeOf<VertexPositionColor>();
            int offset = 0;
            var bufferBinding = new SharpDX.Direct3D11.VertexBufferBinding(this.vertexBuffer, stride, offset);
            context.InputAssembler.SetVertexBuffers(0, bufferBinding);
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;
            context.InputAssembler.InputLayout = this.inputLayout;
            
            context.VertexShader.SetShader(this.vertexShader, null, 0);
            context.VertexShader.SetConstantBuffers(0, this.modelConstantBuffer);

            if (!this.usingVprtShaders)
            {
                context.GeometryShader.SetShader(this.geometryShader, null, 0);
            }
            
            context.PixelShader.SetShader(this.pixelShader, null, 0);
            
            context.DrawInstanced(vertexCount, 2, 0, 0);
        }

        /// <summary>
        /// Creates device-based resources to store a constant buffer, cube
        /// geometry, and vertex and pixel shaders. In some cases this will also 
        /// store a geometry shader.
        /// </summary>
        public async void CreateDeviceDependentResourcesAsync()
        {
            ReleaseDeviceDependentResources();

            usingVprtShaders = deviceResources.D3DDeviceSupportsVprt;

            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            
            var vertexShaderFileName = usingVprtShaders ? "Content\\Shaders\\VPRTVertexShader.cso" : "Content\\Shaders\\VertexShader.cso";
            
            var vertexShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(vertexShaderFileName));
            
            vertexShader = this.ToDispose(new SharpDX.Direct3D11.VertexShader(
                deviceResources.D3DDevice,
                vertexShaderByteCode));

            SharpDX.Direct3D11.InputElement[] vertexDesc =
            {
                new SharpDX.Direct3D11.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,  0, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
                new SharpDX.Direct3D11.InputElement("COLOR",    0, SharpDX.DXGI.Format.R32G32B32_Float, 12, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
            };

            inputLayout = this.ToDispose(new SharpDX.Direct3D11.InputLayout(
                deviceResources.D3DDevice,
                vertexShaderByteCode,
                vertexDesc));

            if (!usingVprtShaders)
            {
                var geometryShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\GeometryShader.cso"));
                
                geometryShader = this.ToDispose(new SharpDX.Direct3D11.GeometryShader(
                    deviceResources.D3DDevice,
                    geometryShaderByteCode));
            }
            
            var pixelShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\PixelShader.cso"));
            
            pixelShader = this.ToDispose(new SharpDX.Direct3D11.PixelShader(
                deviceResources.D3DDevice,
                pixelShaderByteCode));

            VertexPositionColor[] planeVertices =
            {
                new VertexPositionColor(new Vector3( 1.0f,  1.0f, 0.0f), new Vector3(1, 1, 1)),
                new VertexPositionColor(new Vector3( 1.0f, -1.0f, 0.0f), new Vector3(1, 1, 1)),
                new VertexPositionColor(new Vector3(-1.0f, -1.0f, 0.0f), new Vector3(1, 1, 1)),
            };

            vertexCount = planeVertices.Length;

            vertexBuffer = this.ToDispose(SharpDX.Direct3D11.Buffer.Create(
                deviceResources.D3DDevice,
                SharpDX.Direct3D11.BindFlags.VertexBuffer,
                planeVertices));
            
            modelConstantBuffer = this.ToDispose(SharpDX.Direct3D11.Buffer.Create(
                deviceResources.D3DDevice,
                SharpDX.Direct3D11.BindFlags.ConstantBuffer,
                ref modelConstantBufferData));
            
            loadingComplete = true;
        }

        /// <summary>
        /// Releases device-based resources.
        /// </summary>
        public void ReleaseDeviceDependentResources()
        {
            loadingComplete = false;
            usingVprtShaders = false;
            this.RemoveAndDispose(ref vertexShader);
            this.RemoveAndDispose(ref inputLayout);
            this.RemoveAndDispose(ref pixelShader);
            this.RemoveAndDispose(ref geometryShader);
            this.RemoveAndDispose(ref modelConstantBuffer);
            this.RemoveAndDispose(ref vertexBuffer);
        }

        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
    }
}
