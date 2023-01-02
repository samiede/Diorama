using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DiscontinuitySourceRF : ScriptableRendererFeature
{
    class DiscontinuitySourcePass : ScriptableRenderPass
    {
        int kDepthBufferBits = 32;
        private RTHandle DiscontinuityAttachmentHandle { get; set; }
        internal RenderTextureDescriptor Descriptor { get; private set; }

        private FilteringSettings _mFilteringSettings;
        private const string m_ProfilerTag = "Discontinuity Prepass";
        readonly ShaderTagId m_ShaderTagId = new("Outline");

        public DiscontinuitySourcePass(RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            _mFilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RTHandle outlineAttachmentHandle)
        {
            DiscontinuityAttachmentHandle = outlineAttachmentHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            baseDescriptor.depthBufferBits = kDepthBufferBits;
            Descriptor = baseDescriptor;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(Shader.PropertyToID(DiscontinuityAttachmentHandle.name), Descriptor, FilterMode.Point);
            ConfigureTarget(DiscontinuityAttachmentHandle);
            ConfigureClear(ClearFlag.All, Color.black);
            // ConfigureInput(ScriptableRenderPassInput.Normal);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }
            
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            
            using (new ProfilingScope(cmd, new ProfilingSampler(m_ProfilerTag)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
                
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _mFilteringSettings);

                cmd.SetGlobalTexture("_DiscontinuityTexture", DiscontinuityAttachmentHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
    }
    
    class TransparentDiscontinuitySourcePass : ScriptableRenderPass
    {
        int kDepthBufferBits = 32;
        private RTHandle TransparentDiscontinuityAttachmentHandle { get; set; }
        private RTHandle TransparentDepthAttachmentHandle { get; set; }
        private RenderTextureDescriptor Descriptor { get; set; }

        private FilteringSettings _mFilteringSettings;
        private const string m_ProfilerTag = "Transparent Discontinuity Prepass";
        readonly ShaderTagId m_ShaderTagId = new("OutlineTransparent");
        
        
        // string depthTextureName = "_CameraDepthTexture";
        
        // private int m_BufferId = Shader.PropertyToID("_BufferName");
        // private RTHandle m_BufferRT;


        public TransparentDiscontinuitySourcePass(RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            _mFilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RTHandle outlineAttachmentHandle, RTHandle customDepthAttachmentHandle)
        {
            TransparentDiscontinuityAttachmentHandle = outlineAttachmentHandle;
            TransparentDepthAttachmentHandle = customDepthAttachmentHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.ARGBFloat;
            baseDescriptor.depthBufferBits = kDepthBufferBits;
            Descriptor = baseDescriptor;
            
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            
            
            cmd.GetTemporaryRT(Shader.PropertyToID(TransparentDiscontinuityAttachmentHandle.name), Descriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(Shader.PropertyToID(TransparentDepthAttachmentHandle.name), Descriptor, FilterMode.Bilinear);
            
            // ConfigureTarget(TransparentDiscontinuityAttachmentHandle, TransparentDepthAttachmentHandle);
            ConfigureTarget(TransparentDiscontinuityAttachmentHandle);
            ConfigureClear(ClearFlag.All, Color.black);
            ConfigureInput(ScriptableRenderPassInput.Depth);
            

        }


        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }
            
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            
            using (new ProfilingScope(cmd, new ProfilingSampler(m_ProfilerTag)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                // drawSettings.perObjectData = PerObjectData.None;
                
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _mFilteringSettings);

                cmd.SetGlobalTexture("_TransparentDiscontinuityTexture", TransparentDiscontinuityAttachmentHandle);
                cmd.SetGlobalTexture("_TransparentDepthSourceTexture", TransparentDepthAttachmentHandle);

                
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            base.OnCameraCleanup(cmd);
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(TransparentDiscontinuityAttachmentHandle.name));
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(TransparentDepthAttachmentHandle.name));
        }
        
    }


    private DiscontinuitySourcePass _discontinuitySourcePass;
    private TransparentDiscontinuitySourcePass _transparentDiscontinuitySourcePass;
    private RTHandle _discontinuitySourceTexture;
    private RTHandle _transparentDiscontinuitySourceTexture;
    private RTHandle _transparentDepthSourceTexture;
    public RenderPassEvent renderPassEvent;
    public RenderPassEvent transparentRenderPassEvent;

    public override void Create()
    {
        _discontinuitySourcePass = new DiscontinuitySourcePass(RenderQueueRange.opaque, -1)
        {
            renderPassEvent = renderPassEvent
        };
        _discontinuitySourceTexture = RTHandles.Alloc("_DiscontinuityTexture", name: "_DiscontinuityTexture");
        
        _transparentDiscontinuitySourcePass = new TransparentDiscontinuitySourcePass(RenderQueueRange.all, -1)
        {
            renderPassEvent = transparentRenderPassEvent
        };
        
        _transparentDiscontinuitySourceTexture = RTHandles.Alloc("_TransparentDiscontinuityTexture", name: "_TransparentDiscontinuityTexture");
        _transparentDepthSourceTexture = RTHandles.Alloc("_TransparentDepthSourceTexture", name: "_TransparentDepthSourceTexture");
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        _discontinuitySourcePass.Setup(renderingData.cameraData.cameraTargetDescriptor, _discontinuitySourceTexture);
        renderer.EnqueuePass(_discontinuitySourcePass);
        _transparentDiscontinuitySourcePass.Setup(renderingData.cameraData.cameraTargetDescriptor, _transparentDiscontinuitySourceTexture, _transparentDepthSourceTexture);
        renderer.EnqueuePass(_transparentDiscontinuitySourcePass);
    }
}

