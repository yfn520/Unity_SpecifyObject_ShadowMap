using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomShadowMgr : MonoBehaviour
{
    private List<Renderer> shadowCasters = new List<Renderer>();
    public static CustomShadowMgr Instance;

    private Light _mainLight;

    public Light MainLight => _mainLight;
    public Matrix4x4 ViewMatrix => viewMatrix;
    public Matrix4x4 ProjMatrix => projMatrix;
    public Matrix4x4 VPMatrix => vpMatrix;
    public Matrix4x4 LightMatrix => lightMatrix;

    public Matrix4x4 FrustumMatrix => frutumMatrix;

    public List<Renderer> ShadowCasters => shadowCasters;
    
    private Matrix4x4 viewMatrix;
    private Matrix4x4 projMatrix;
    private Matrix4x4 vpMatrix;
    private Matrix4x4 lightMatrix;
    
    private Matrix4x4 frutumMatrix;
    
    private int frame = 0;
    
    public int interval = 1; //刷新间隔
    public float frustumExtendSize = 2; //大一些才能覆盖住需要投影的像素区域
    public bool IsValid => shadowCasters.Count > 0;
    private void Awake()
    {
        Instance = this;
        Instance.Init();
    }

    public void Init(Light mainLight = null)
    {
        shadowCasters.Clear();
        _mainLight = mainLight != null ? mainLight : Light.GetLights(LightType.Directional, 0)[0];
    }
    
    public void AddShadowCaster(GameObject obj)
    {
        var meshRenders = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var meshRenderer in meshRenders)
        {
            shadowCasters.Add(meshRenderer);
        }

        var skinRenders = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skinRender in skinRenders)
        {
            shadowCasters.Add(skinRender);
        }
    }

    void RemoveShadowCaster(GameObject obj)
    {
        var meshRenders = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var meshRenderer in meshRenders)
        {
            shadowCasters.Remove(meshRenderer);
        }
        
        var skinRenders = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skinRender in skinRenders)
        {
            shadowCasters.Remove(skinRender);
        }
    }


   void CaculateVP()
   {
       var worldBounds = new Bounds();

       var center = Vector3.zero;
    
       //skinMeshrender need bake？
       foreach (var meshRenderer in shadowCasters)
       {
           var mBounds = meshRenderer.bounds;
           center += mBounds.center;
       }

       center /= shadowCasters.Count;

       worldBounds.center = center;
       foreach (var meshRenderer in shadowCasters)
       {
           var mBounds = meshRenderer.bounds;
           worldBounds.Encapsulate(mBounds);
       }
    
       var radius = Vector3.Distance(worldBounds.center, worldBounds.max);

       
       var lightDirection = _mainLight.transform.rotation * Vector3.forward;
       var sidePt = worldBounds.center + (-lightDirection.normalized) * (radius);
    
       //Quaternion quat = Quaternion.LookRotation(light.direction);
       viewMatrix = Matrix4x4.TRS(sidePt, _mainLight.transform.rotation, new Vector3(1,1,-1)).inverse;
       projMatrix = Matrix4x4.Ortho(-radius, radius, -radius, radius, 0f, 2 * radius);
       //projMatrix = GL.GetGPUProjectionMatrix(projMatrix, false);

       vpMatrix = GetShadowTransform(projMatrix, viewMatrix);
           //GL.GetGPUProjectionMatrix(projMatrix, true) * viewMatrix;
       lightMatrix = _mainLight.transform.localToWorldMatrix;

       frutumMatrix = Matrix4x4.TRS(worldBounds.center, _mainLight.transform.rotation,
           Vector3.one * (2 * radius + frustumExtendSize));
       
   }
   
   static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
   {
       // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
       // apply z reversal to projection matrix. We need to do it manually here.
       if (SystemInfo.usesReversedZBuffer)
       {
           proj.m20 = -proj.m20;
           proj.m21 = -proj.m21;
           proj.m22 = -proj.m22;
           proj.m23 = -proj.m23;
       }

       Matrix4x4 worldToShadow = proj * view;

       var textureScaleAndBias = Matrix4x4.identity;
       textureScaleAndBias.m00 = 0.5f;
       textureScaleAndBias.m11 = 0.5f;
       textureScaleAndBias.m22 = 0.5f;
       textureScaleAndBias.m03 = 0.5f;
       textureScaleAndBias.m23 = 0.5f;
       textureScaleAndBias.m13 = 0.5f;

       // Apply texture scale and offset to save a MAD in shader.
       return textureScaleAndBias * worldToShadow;
   }
   
    
    public void RefreshFrustum()
    {
        CaculateVP();
    }
    // Start is called before the first frame update
    void Start()
    {
        AddShadowCaster(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        frame++;
        if (frame % interval == 0)
        {
            RefreshFrustum();
        }
    }

    private void OnDestroy()
    {
        RemoveShadowCaster(gameObject);
        Instance = null;
    }
}
