using FrameRateControlExperiment60.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Spatial;

namespace FrameRateControlExperiment60.Content
{
    internal class WaveFrontModelRenderer : Disposer
    {
        private DeviceResources deviceResources;

        private SharpDX.Direct3D11.InputLayout inputLayout;
        private SharpDX.Direct3D11.Buffer vertexBuffer;
        private SharpDX.Direct3D11.Buffer indexBuffer;
        private SharpDX.Direct3D11.VertexShader vertexShader;
        private SharpDX.Direct3D11.GeometryShader geometryShader;
        private SharpDX.Direct3D11.PixelShader pixelShader;
        private SharpDX.Direct3D11.Buffer modelConstantBuffer;

        private ModelConstantBuffer modelConstantBufferData;
        private int indexCount;
        private Vector3 position = new Vector3(0, 0, 2);

        private bool loadingComplete = false;

        private bool usingVprtShaders = false;

        public WaveFrontModelRenderer(DeviceResources deviceResources)
        {
            this.deviceResources = deviceResources;

            this.CreateDeviceDependentResourcesAsync();

            while (!loadingComplete) ;
        }

        public void PositionHologram(SpatialPointerPose pointerPose)
        {
            if (null != pointerPose)
            {
                Vector3 headPosition = pointerPose.Head.Position;
                Vector3 headDirection = pointerPose.Head.ForwardDirection;

                float distanceFromUser = 2.0f;
                Vector3 gazeAtTwoMeters = headPosition + (distanceFromUser * headDirection);

                this.position = gazeAtTwoMeters;
            }
        }

        public void Update(StepTimer timer)
        {
            Matrix4x4 modelTranslation = Matrix4x4.CreateTranslation(position);

            Matrix4x4 modelTransform = modelTranslation;

            modelConstantBufferData.model = Matrix4x4.Transpose(modelTransform);

            if (!loadingComplete)
            {
                return;
            }

            var context = deviceResources.D3DDeviceContext;
            context.UpdateSubresource(ref modelConstantBufferData, modelConstantBuffer);
        }

        public void Render()
        {
            if (!loadingComplete)
                return;

            var context = deviceResources.D3DDeviceContext;

            int stride = SharpDX.Utilities.SizeOf<VertexUVNormal>();
            int offset = 0;
            var bufferBinding = new SharpDX.Direct3D11.VertexBufferBinding(vertexBuffer, stride, offset);
            context.InputAssembler.SetVertexBuffers(0, bufferBinding);
            context.InputAssembler.SetIndexBuffer(
                indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0
                );
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.InputAssembler.InputLayout = inputLayout;

            context.VertexShader.SetShader(vertexShader, null, 0);
            context.VertexShader.SetConstantBuffers(0, modelConstantBuffer);

            if (!usingVprtShaders)
            {
                context.GeometryShader.SetShader(geometryShader, null, 0);
            }

            context.PixelShader.SetShader(pixelShader, null, 0);

            context.DrawIndexedInstanced(indexCount, 2, 0, 0, 0);
        }

        public async void CreateDeviceDependentResourcesAsync()
        {
            ReleaseDeviceDependentResources();

            usingVprtShaders = deviceResources.D3DDeviceSupportsVprt;

            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            var vertexShaderFileName = usingVprtShaders ? "Content\\Shaders\\VPRTVertexShaderWithNormal.cso" : "Content\\Shaders\\VertexShaderWithNormal.cso";

            var vertexShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(vertexShaderFileName));

            vertexShader = ToDispose(new SharpDX.Direct3D11.VertexShader(
                deviceResources.D3DDevice, vertexShaderByteCode));

            SharpDX.Direct3D11.InputElement[] vertexDesc =
            {
                new SharpDX.Direct3D11.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,  0, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
                new SharpDX.Direct3D11.InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 12, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
                new SharpDX.Direct3D11.InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 20, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
            };

            inputLayout = ToDispose(new SharpDX.Direct3D11.InputLayout(
                deviceResources.D3DDevice,
                vertexShaderByteCode,
                vertexDesc));

            if (!usingVprtShaders)
            {
                var geometryShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\GeometryShader.cso"));

                geometryShader = ToDispose(new SharpDX.Direct3D11.GeometryShader(
                    deviceResources.D3DDevice,
                    geometryShaderByteCode));
            }

            WaveFrontModel[] waveFrontModels = WaveFrontModel.CreateFromObj("Assets\\bunny.obj");
            Debug.Assert(waveFrontModels.Length > 0);

            var accumulatedWaveFrontModel = waveFrontModels[0];
            
            if (waveFrontModels.Length > 1)
            {
                for (int i = 1; i < waveFrontModels.Length; ++i)
                {
                    accumulatedWaveFrontModel += waveFrontModels[i];
                }
            }

            vertexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(
                deviceResources.D3DDevice,
                SharpDX.Direct3D11.BindFlags.VertexBuffer,
                accumulatedWaveFrontModel.VertexData.ToArray()));

            indexCount = accumulatedWaveFrontModel.IndexData.Count;

            indexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(
                deviceResources.D3DDevice,
                SharpDX.Direct3D11.BindFlags.IndexBuffer,
                accumulatedWaveFrontModel.IndexData.ToArray()));

            modelConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(
                deviceResources.D3DDevice,
                SharpDX.Direct3D11.BindFlags.ConstantBuffer,
                ref modelConstantBufferData));

            loadingComplete = true;
        }

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
            this.RemoveAndDispose(ref indexBuffer);
        }

        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
    }
}
