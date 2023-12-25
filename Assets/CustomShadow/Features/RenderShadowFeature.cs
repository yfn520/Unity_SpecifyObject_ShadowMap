using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class RenderShadowFeature : ScriptableRendererFeature
{
    public class RenderShadowPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "RenderShadow";
        ProfilingSampler m_ProfilingSampler;
        
       RenderTargetHandle m_MainLightShadowmap;
       RenderTexture m_MainLightShadowmapTexture;
       
       private Matrix4x4 _viewMatrix;
       private Matrix4x4 _projMatrix;
       private Matrix4x4 _vpMatrix;
       private Matrix4x4 _lightMatrix;
       
       private int m_resolution;
       private bool m_softShadow;
       public float m_depthBias = 1;
       public float m_normalBias = 1;
       
       // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public void SetUp(int resolution, bool softShadow, float depthBias, float normalBias, 
            Matrix4x4 viewMatrix, Matrix4x4 projMatrix, Matrix4x4 vpMatrix, Matrix4x4 lightMatrix)
        {
            m_MainLightShadowmap.Init("CustomShadowMap");

            m_resolution = resolution;
            m_softShadow = softShadow;
            
            m_depthBias = depthBias;
            m_normalBias = normalBias;

            _viewMatrix = viewMatrix;
            _projMatrix = projMatrix;
            _vpMatrix = vpMatrix;
            _lightMatrix = lightMatrix;
        }
        
        public static Vector4 GetShadowBias(float biasX, float biasY, Matrix4x4 lightProjectionMatrix, float shadowResolution, bool supportsSoftShadows)
        {
            float frustumSize = 2.0f / lightProjectionMatrix.m00;
            
            // depth and normal bias scale is in shadowmap texel size in world space
            float texelSize = frustumSize / shadowResolution;
            float depthBias = -biasX * texelSize;
            float normalBias = -biasY * texelSize;

            if (supportsSoftShadows)
            {
                // TODO: depth and normal bias assume sample is no more than 1 texel away from shadowmap
                // This is not true with PCF. Ideally we need to do either
                // cone base bias (based on distance to center sample)
                // or receiver place bias based on derivatives.
                // For now we scale it by the PCF kernel size (5x5)
                const float kernelRadius = 2.5f;
                depthBias *= kernelRadius;
                normalBias *= kernelRadius;
            }

            return new Vector4(depthBias, normalBias, 0.0f, 0.0f);
        }
        
        public static void SetupShadowCasterConstantBuffer(CommandBuffer cmd, Matrix4x4 lightL2W, Vector4 shadowBias)
        {
            Vector3 lightDirection = -lightL2W.GetColumn(2);
            cmd.SetGlobalVector("_ShadowBias", shadowBias);
            cmd.SetGlobalVector("_LightDirection", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0.0f));
        }

         void SetupMainLightShadowReceiverConstants(CommandBuffer cmd)
        {
            var ShadowParams = Shader.PropertyToID("_Custom_MainLightShadowParams");
            var ShadowOffset0 = Shader.PropertyToID("_Custom_MainLightShadowOffset0");
            var ShadowOffset1 = Shader.PropertyToID("_Custom_MainLightShadowOffset1");
            var ShadowOffset2 = Shader.PropertyToID("_Custom_MainLightShadowOffset2");
            var ShadowOffset3 = Shader.PropertyToID("_Custom_MainLightShadowOffset3");
            var ShadowmapSize = Shader.PropertyToID("_Custom_MainLightShadowmapSize");
            
            float invShadowAtlasWidth = 1.0f / m_resolution;
            float invShadowAtlasHeight = 1.0f / m_resolution;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            float softShadowsProp = m_softShadow ? 1.0f : 0.0f;
            
            cmd.SetGlobalVector(ShadowParams, new Vector4(CustomShadowMgr.Instance.MainLight.shadowStrength, softShadowsProp, 0.0f, 0.0f));

            var m_SupportsBoxFilterForShadows = Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch;
            // Inside shader soft shadows are controlled through global keyword.
            // If any additional light has soft shadows it will force soft shadows on main light too.
            // As it is not trivial finding out which additional light has soft shadows, we will pass main light properties if soft shadows are supported.
            // This workaround will be removed once we will support soft shadows per light.
            if (m_softShadow)
            {
                if (m_SupportsBoxFilterForShadows)
                {
                    cmd.SetGlobalVector(ShadowOffset0,
                        new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(ShadowOffset1,
                        new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(ShadowOffset2,
                        new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(ShadowOffset3,
                        new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                }

                // Currently only used when !SHADER_API_MOBILE but risky to not set them as it's generic
                // enough so custom shaders might use it.
                cmd.SetGlobalVector(ShadowmapSize, new Vector4(invShadowAtlasWidth,
                    invShadowAtlasHeight,
                    m_resolution, m_resolution));
            }
        }
         
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_MainLightShadowmap.id, m_resolution,m_resolution, 16, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
          // atlasTextureId = new RenderTargetIdentifier(atlasHandle.id);
           // Any temporary textures that were not explicitly released will be removed after camera is done rendering

           // setup attachments
           ConfigureTarget(m_MainLightShadowmap.Identifier()); // unity treats shadowmap as color attachment at beginning
           ConfigureClear(ClearFlag.All, Color.black);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
         //   StencilMask(context);
            // setup attachments
        //    ConfigureTarget(m_MainLightShadowmap.Identifier()); // unity treats shadowmap as color attachment at beginning
        //    ConfigureClear(ClearFlag.All, Color.black);

            var camViewMatrix = renderingData.cameraData.camera.worldToCameraMatrix;
            var camProjMatrix = renderingData.cameraData.camera.projectionMatrix;
            
            var renders = CustomShadowMgr.Instance.ShadowCasters;
            
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.Clear();
                // foreach (var renderer in renders)
                // {
                // //    cmd.DrawRenderer(renderer,  renderer.sharedMaterial, 0, renderer.sharedMaterial.FindPass("ShadowCaster"));
                //     cmd.DrawRenderer(renderer, _stencilMaterial);
                // }
                
                cmd.SetViewProjectionMatrices(_viewMatrix, _projMatrix);
               
               cmd.SetGlobalFloat("texTexel", 1.0f/m_resolution);
               cmd.SetGlobalMatrix("shadowVPMat", _vpMatrix);
               //cmd.SetGlobalMatrix("shadowProjMat", Matrix4x4.identity);
               cmd.SetGlobalTexture("CustomShadowMap", m_MainLightShadowmap.Identifier());
               //
               context.ExecuteCommandBuffer(cmd);
               cmd.Clear();
               
               var shadowBias = GetShadowBias(m_depthBias, m_normalBias, _projMatrix, m_resolution, false);
               SetupShadowCasterConstantBuffer(cmd, _lightMatrix, shadowBias);
               SetupMainLightShadowReceiverConstants(cmd);
               
               CoreUtils.SetKeyword(cmd, "_USE_SOFT_SHADOW", m_softShadow);
               
               foreach (var renderer in renders)
               {
                   cmd.DrawRenderer(renderer,  renderer.sharedMaterial, 0, renderer.sharedMaterial.FindPass("ShadowCaster"));
               }
               
                cmd.SetViewProjectionMatrices(camViewMatrix, camProjMatrix);
                context.ExecuteCommandBuffer(cmd);
            }
            
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_MainLightShadowmap.id);
             if (m_MainLightShadowmapTexture)
             {
                 RenderTexture.ReleaseTemporary(m_MainLightShadowmapTexture);
                 m_MainLightShadowmapTexture = null;
             }
        }
    }
    
    RenderShadowPass m_ScriptablePass;

    public int shadowResolution = 512;
    public bool softShadow = true;
    public float depthBias = 1.0f;
    public float normalBias = 1.0f;
    
    public override void Create()
    {
        m_ScriptablePass = new RenderShadowPass();
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        //DPManager.Instance.RegisterFeature(this);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the rendereronce per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (CustomShadowMgr.Instance == null || !CustomShadowMgr.Instance.IsValid)
            return;
        
        m_ScriptablePass.SetUp(shadowResolution, softShadow, depthBias, normalBias,
        CustomShadowMgr.Instance.ViewMatrix, CustomShadowMgr.Instance.ProjMatrix, CustomShadowMgr.Instance.VPMatrix, CustomShadowMgr.Instance.LightMatrix);
        
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


