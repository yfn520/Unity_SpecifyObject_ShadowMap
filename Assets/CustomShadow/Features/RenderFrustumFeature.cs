using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderFrustumFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "RenderFrustum";
        ProfilingSampler m_ProfilingSampler;
        
        // private Vector3 frustumPos;
        //
        // private Quaternion frustumRot;
        //
        // private float size;

        private Mesh mesh;
        public Material mat;
        
        public float aabb_extends = 0; //用于扩大渲染的范围，保证不被裁剪

        private Matrix4x4 _frustumMatrix;
        public void Setup(Mesh meshFilter, Material matrial, Matrix4x4 frustumMatrix)
        {
            if(meshFilter == null)
                return;
            
            mesh = meshFilter;
            mat = matrial;
            _frustumMatrix = frustumMatrix;
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

       // void CaculateVP()
       // {
       //     var m_light = GameObject.FindObjectOfType<Light>();
       //     // if (_renderers.Count == 0)
       //     // {
       //     var _renderers = new List<MeshRenderer>(GameObject.FindObjectsOfType<MeshRenderer>());
       //
       //     var layer = LayerMask.NameToLayer("CustomShadow");
       //     
       //     for (int i = _renderers.Count - 1; i >= 0; i--)
       //     {
       //         if (_renderers[i].gameObject.layer != layer)
       //         {
       //             _renderers.RemoveAt(i);
       //         }
       //     }
       //     // DirectionalLight
       //     if ( _renderers.Count == 0)
       //     {
       //         return;
       //     }
       //  
       //     var worldBounds = new Bounds();
       //
       //     var center = Vector3.zero;
       //  
       //     foreach (var meshRenderer in _renderers)
       //     {
       //         var mBounds = meshRenderer.bounds;
       //         center += mBounds.center;
       //     }
       //
       //     center /= _renderers.Count;
       //
       //     worldBounds.center = center;
       //     foreach (var meshRenderer in _renderers)
       //     {
       //         var mBounds = meshRenderer.bounds;
       //         worldBounds.Encapsulate(mBounds);
       //     }
       //
       //     frustumPos = worldBounds.center;
       //     frustumRot = m_light.transform.rotation;
       //     
       //     var radius = Vector3.Distance(worldBounds.center, worldBounds.max) * 2 + aabb_extends;
       //     size = radius;
       // }
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (mesh == null || mat == null)
                return;
            
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.Clear();
                //   cmd.SetRenderTarget(m_MainLightShadowmap.id);
                //  cmd.ClearRenderTarget(true,true, Color.white, 1);
                cmd.DrawMesh(mesh, _frustumMatrix, mat);
                context.ExecuteCommandBuffer(cmd);
            }
            
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    public Mesh meshFilter;
    public Material material;
    public override void Create()
    {
        if (CustomShadowMgr.Instance == null)
            return;
        
        m_ScriptablePass = new CustomRenderPass();
        //
        // var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // meshFilter = obj.GetComponent<MeshFilter>().sharedMesh;
        // GameObject.Destroy(obj);
        //
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (CustomShadowMgr.Instance == null || !CustomShadowMgr.Instance.IsValid)
            return;
        
        m_ScriptablePass.Setup(meshFilter, material, CustomShadowMgr.Instance.FrustumMatrix);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


