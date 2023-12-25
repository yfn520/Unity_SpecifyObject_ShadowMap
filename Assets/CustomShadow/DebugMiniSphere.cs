#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DebugMiniSphere : MonoBehaviour
{
    public Camera camera;

    public float cascedePercent = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnDrawGizmos()
    {
       // DrawRendererBounds();
       // CaculateVP();
        if (camera == null)
            return;

    //     var mxCamProj = camera.projectionMatrix;
    //
    //     var zCascadeNear = camera.nearClipPlane;
    //     var zCascadeFar = camera.farClipPlane;
    //
    //     var zCascadeNearNDC = -1;//= (zCascadeNear * mxCamProj.m22 + mxCamProj.m32) / zCascadeNear;
    //     var zCascadeFarNDC = 1;//(zCascadeFar * mxCamProj.m22 + mxCamProj.m32) / zCascadeFar;
    //
    //     var cameraPosition = camera.transform.position;
    //     var cameraDirection = camera.transform.forward;
    //     
    //     var mxCamViewProjInv = (camera.projectionMatrix * camera.worldToCameraMatrix).inverse;
    //     
    //     // 计算各层 cascade 的 Frustum (NDC space -> world space)
    //     Vector4[] vector4s = new Vector4[8];
    //     vector4s[0] = mxCamViewProjInv * new Vector4(-1.0f, -1.0f, zCascadeNearNDC, 1);
    //     vector4s[1] =  mxCamViewProjInv * new Vector4(-1.0f, 1.0f, zCascadeNearNDC, 1);
    //     vector4s[2] =  mxCamViewProjInv * new Vector4(1.0f, -1.0f, zCascadeNearNDC, 1);
    //     vector4s[3] =  mxCamViewProjInv * new Vector4(1.0f, 1.0f, zCascadeNearNDC, 1);
    //     
    //     vector4s[4] =  mxCamViewProjInv * new Vector4(-1.0f, -1.0f, zCascadeFarNDC, 1);
    //     vector4s[5] =  mxCamViewProjInv * new Vector4(-1.0f, 1.0f, zCascadeFarNDC, 1);
    //     vector4s[6] =  mxCamViewProjInv * new Vector4(1.0f, -1.0f, zCascadeFarNDC, 1);
    //     vector4s[7] =  mxCamViewProjInv * new Vector4(1.0f, 1.0f, zCascadeFarNDC, 1);
    //
    //
    //     Vector3[] worldFrustum = new Vector3[8];
    //    // Vector4[] worldFrustum = new Vector4[8];
    //     for (int i = 0; i < 8; i++)
    //     {
    //         worldFrustum[i] = vector4s[i] / vector4s[i].w;
    //         Gizmos.DrawCube(worldFrustum[i], Vector3.one * 1f);     
    //     }
    //     
    // // Step 2 Begin
    // // 为 SubFrustum 计算外接球
    //         float a2 = (worldFrustum[3] - worldFrustum[0]).sqrMagnitude;
    //         float b2 = (worldFrustum[7] - worldFrustum[4]).sqrMagnitude;
    //         float len = zCascadeFar - zCascadeNear;
    //         float x = len * 0.5f + (a2 - b2) / (8.0f * len); 
    //
    // // zCascadeDistance: 当前 cascade 中 Near平面中心点 到 frustum 外接球心 的距离
    //         float zCascadeDistance = len - x;
    //
    // // 计算 外接球 的 圆心，VS = ViewSpace，WS = World Space
    //         Vector3 sphereCenterVS = new Vector3(0.0f, 0.0f, zCascadeNear + zCascadeDistance); 
    //         Vector3 sphereCenterWS = cameraPosition + cameraDirection * sphereCenterVS.z;
    //  
    // // 计算 外接球 的 半径，这里用勾股定理算的
    //         float sphereRadius = Mathf.Sqrt(zCascadeDistance * zCascadeDistance + (a2 * 0.25f));
    //         
    //         Gizmos.DrawWireSphere(sphereCenterWS, sphereRadius);

           // PaperMethods((worldFrustum[5] - worldFrustum[4]).magnitude, (worldFrustum[6] - worldFrustum[4]).magnitude);

           var cascadeClipPlane = new Vector2[2];

           var percents = new float[] { 0.5f, 0.5f };
           var nearclip = camera.nearClipPlane;
           var percent = 0f;
           for (int i = 0; i < 2; i++)
           {
               percent += percents[i];
               var far = (camera.farClipPlane - camera.nearClipPlane) * percent;
               cascadeClipPlane[i] = new Vector2(nearclip, far);
               nearclip = far;
           }
           
           for (int i = 0; i < cascadeClipPlane.Length; i++)
           {
               Matrix4x4 matrix4X4;
               //GetCascadeVPMatrix(camera, cascadeClipPlane[i].x, cascadeClipPlane[i].y, out matrix4X4);
               
               GetCascadeVPMatrix2(camera, cascadeClipPlane[i].x, cascadeClipPlane[i].y, 1024, out matrix4X4);


               // if (i == 0)
               // {
               //     var planes = GeometryUtility.CalculateFrustumPlanes(matrix4X4);
               //     foreach (var plane in planes)
               //     {
               //         DrawPlane(plane);
               //     }   
               // }
           }
           
    }

    void DrawPlane(Plane plane)
    {
        Quaternion rotation = Quaternion.LookRotation(plane.normal);
        Matrix4x4 trs = Matrix4x4.TRS(plane.normal.normalized * plane.distance, rotation, Vector3.one);
        Gizmos.matrix = trs;
        Color32 color = Color.blue;
        color.a = 125;
        Gizmos.color = color;
        Gizmos.DrawCube(Vector3.zero, new Vector3(1.0f, 1.0f, 0.0001f) * 100);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }

    bool GetCascadeVPMatrix(Camera camera, float zCascadeNear, float zCascadeFar, out Matrix4x4 VPMatrix)
    {
        VPMatrix = Matrix4x4.zero;
        if (camera == null)
            return false;
        
        var light = GameObject.FindObjectOfType<Light>();
        if (light == null)
            return false;
        
        var mxCamProj = camera.projectionMatrix;
        // var zCascadeNear = camera.nearClipPlane;
        // var zCascadeFar = camera.farClipPlane;

        var zCascadeNearNDC = (-zCascadeNear * mxCamProj.m22 + mxCamProj.m23) / zCascadeNear;
        var zCascadeFarNDC = (-zCascadeFar * mxCamProj.m22 + mxCamProj.m23) / zCascadeFar;

       // zCascadeNearNDC = -1;//zCascadeNear * 2f / (camera.farClipPlane - camera.nearClipPlane) - 1;
       // zCascadeFarNDC = 1f;//zCascadeFar * 2f/ (camera.farClipPlane - camera.nearClipPlane) - 1;
        
        
        var cameraPosition = camera.transform.position;
        var cameraDirection = camera.transform.forward;
        
        var mxCamViewProjInv = (camera.projectionMatrix * camera.worldToCameraMatrix).inverse;
        
        // 计算各层 cascade 的 Frustum (NDC space -> world space)
        Vector4[] vector4s = new Vector4[8];
        vector4s[0] = mxCamViewProjInv * new Vector4(-1.0f, -1.0f, zCascadeNearNDC, 1);
        vector4s[1] =  mxCamViewProjInv * new Vector4(-1.0f, 1.0f, zCascadeNearNDC, 1);
        vector4s[2] =  mxCamViewProjInv * new Vector4(1.0f, -1.0f, zCascadeNearNDC, 1);
        vector4s[3] =  mxCamViewProjInv * new Vector4(1.0f, 1.0f, zCascadeNearNDC, 1);
        
        vector4s[4] =  mxCamViewProjInv * new Vector4(-1.0f, -1.0f, zCascadeFarNDC, 1);
        vector4s[5] =  mxCamViewProjInv * new Vector4(-1.0f, 1.0f, zCascadeFarNDC, 1);
        vector4s[6] =  mxCamViewProjInv * new Vector4(1.0f, -1.0f, zCascadeFarNDC, 1);
        vector4s[7] =  mxCamViewProjInv * new Vector4(1.0f, 1.0f, zCascadeFarNDC, 1);


        Vector3[] worldFrustum = new Vector3[8];
       // Vector4[] worldFrustum = new Vector4[8];
        for (int i = 0; i < 8; i++)
        {
            worldFrustum[i] = vector4s[i] / vector4s[i].w;
            Gizmos.DrawCube(worldFrustum[i], Vector3.one * 1f);     
        }
        
    // Step 2 Begin
    // 为 SubFrustum 计算外接球
            float a2 = (worldFrustum[3] - worldFrustum[0]).sqrMagnitude;
            float b2 = (worldFrustum[7] - worldFrustum[4]).sqrMagnitude;
            float len = zCascadeFar - zCascadeNear;
            float x = len * 0.5f + (a2 - b2) / (8.0f * len); 

    // zCascadeDistance: 当前 cascade 中 Near平面中心点 到 frustum 外接球心 的距离
            float zCascadeDistance = len - x;

    // 计算 外接球 的 圆心，VS = ViewSpace，WS = World Space
            Vector3 sphereCenterVS = new Vector3(0.0f, 0.0f, zCascadeNear + zCascadeDistance); 
            Vector3 sphereCenterWS = cameraPosition + cameraDirection * sphereCenterVS.z;
     
    // 计算 外接球 的 半径，这里用勾股定理算的
            float sphereRadius = Mathf.Sqrt(zCascadeDistance * zCascadeDistance + (a2 * 0.25f));
            
            var worldUnitPerTexl = sphereRadius * 2f / (0.5f * 2048);
            var divid =sphereCenterWS / worldUnitPerTexl;
            divid = new Vector3(Mathf.Floor(divid.x),  Mathf.Floor(divid.y), Mathf.Floor(divid.z));
            sphereCenterWS = worldUnitPerTexl * divid;
            
            
            float extraDis = 0;
            var lightDirection = light.transform.rotation * Vector3.forward;
            var sidePt = sphereCenterWS + (-lightDirection.normalized) * (sphereRadius + extraDis);
        
            //Quaternion quat = Quaternion.LookRotation(light.direction);
            var viewMatrix = Matrix4x4.TRS(sidePt, light.transform.rotation, new Vector3(1,1,-1)).inverse;
            var projMatrix = Matrix4x4.Ortho(-sphereRadius, sphereRadius, -sphereRadius, sphereRadius, 0.5f, 2 * sphereRadius + extraDis);
            VPMatrix = projMatrix * viewMatrix;

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(sphereCenterWS, sphereRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(sidePt, Vector3.one * sphereRadius * 2);
            return true;
    }

    bool GetCascadeVPMatrix2(Camera camera, float zCascadeNear, float zCascadeFar, float resolution, out Matrix4x4 VPMatrix)
    {
        VPMatrix = Matrix4x4.zero;
        if (camera == null)
            return false;
        
        var light = GameObject.FindObjectOfType<Light>();
        if (light == null)
            return false;
        
        var mxCamProj = camera.projectionMatrix;
        // var zCascadeNear = camera.nearClipPlane;
        // var zCascadeFar = camera.farClipPlane;

        var zCascadeNearNDC = (-zCascadeNear * mxCamProj.m22 + mxCamProj.m23) / zCascadeNear;
        var zCascadeFarNDC = (-zCascadeFar * mxCamProj.m22 + mxCamProj.m23) / zCascadeFar;

       // zCascadeNearNDC = -1;//zCascadeNear * 2f / (camera.farClipPlane - camera.nearClipPlane) - 1;
       // zCascadeFarNDC = 1f;//zCascadeFar * 2f/ (camera.farClipPlane - camera.nearClipPlane) - 1;
        
        
        var cameraPosition = camera.transform.position;
        var cameraDirection = camera.transform.forward;
        
        var mxCamViewProjInv = (camera.projectionMatrix * camera.worldToCameraMatrix).inverse;
        
        // 计算各层 cascade 的 Frustum (NDC space -> world space)
        Vector4[] vector4s = new Vector4[8];
        vector4s[0] = mxCamViewProjInv * new Vector4(-1.0f, -1.0f, zCascadeNearNDC, 1);
        vector4s[1] =  mxCamViewProjInv * new Vector4(-1.0f, 1.0f, zCascadeNearNDC, 1);
        vector4s[2] =  mxCamViewProjInv * new Vector4(1.0f, -1.0f, zCascadeNearNDC, 1);
        vector4s[3] =  mxCamViewProjInv * new Vector4(1.0f, 1.0f, zCascadeNearNDC, 1);
        
        vector4s[4] =  mxCamViewProjInv * new Vector4(-1.0f, -1.0f, zCascadeFarNDC, 1);
        vector4s[5] =  mxCamViewProjInv * new Vector4(-1.0f, 1.0f, zCascadeFarNDC, 1);
        vector4s[6] =  mxCamViewProjInv * new Vector4(1.0f, -1.0f, zCascadeFarNDC, 1);
        vector4s[7] =  mxCamViewProjInv * new Vector4(1.0f, 1.0f, zCascadeFarNDC, 1);

        Vector3[] locations = new Vector3[8];
       // Vector4[] worldFrustum = new Vector4[8];
        for (int i = 0; i < 8; i++)
        {
            locations[i] = vector4s[i] / vector4s[i].w;
        //    Gizmos.DrawCube(locations[i], Vector3.one * 1f);     
        }

        var maxDist = Mathf.Max(Vector3.Distance(locations[0], locations[3]), 
            Vector3.Distance(locations[4], locations[7]),
            Vector3.Distance(locations[0], locations[7]));

        //Vector3 centerInLight = Vector3.zero;
        Matrix4x4 light2WorldMatrix = Matrix4x4.TRS(Vector3.zero, light.transform.rotation, Vector3.one);
        Matrix4x4 lightMatrix = light2WorldMatrix.inverse;

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        
        for (int i = 0; i < 8; i++)
        {
            locations[i] = lightMatrix.MultiplyPoint(locations[i]);

            min.x = Mathf.Min(locations[i].x, min.x);
            min.y = Mathf.Min(locations[i].y, min.y);
            min.z = Mathf.Min(locations[i].z, min.z);

            max.x = Mathf.Max(locations[i].x, max.x);
            max.y = Mathf.Max(locations[i].y, max.y);
            max.z = Mathf.Max(locations[i].z, max.z);
        }

        var boundsInLight = new Bounds();
        boundsInLight.SetMinMax(min, max);
        
        Gizmos.matrix = light2WorldMatrix;
        
        Gizmos.color = Color.white;

        var worldUnit = maxDist / resolution;

        boundsInLight.center = boundsInLight.center / worldUnit;
        boundsInLight.center = new Vector3(Mathf.Floor(boundsInLight.center.x), Mathf.Floor(boundsInLight.center.y),
            Mathf.Floor(boundsInLight.center.z));
        boundsInLight.center *= worldUnit;
        
        //Gizmos.DrawWireCube(boundsInLight.center, boundsInLight.size);
        
        boundsInLight.size = Vector3.one * maxDist;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boundsInLight.center, boundsInLight.size);
        
        var lightDirection = light.transform.rotation * Vector3.forward;

        var halfDist = 0.5f * maxDist;
        Vector3 viewOrigin = light2WorldMatrix.MultiplyPoint(boundsInLight.center) - lightDirection * halfDist;

        float extraDis = 0;
        //light2WorldMatrix =  Matrix4x4.TRS(viewOrigin, light.transform.rotation, Vector3.one);
        var viewMatrix = Matrix4x4.TRS(viewOrigin, light.transform.rotation, new Vector3(1,1,-1)).inverse;
        var projMatrix = Matrix4x4.Ortho(-halfDist, halfDist, -halfDist, halfDist, 0f, 2 * halfDist + extraDis);
        VPMatrix = projMatrix * viewMatrix;

        //Gizmos.DrawWireCube(sidePt, Vector3.one * sphereRadius * 2);
        
        return true;
    }
    void PaperMethods(float h, float w)
    {
        // var h = camera.scaledPixelHeight;
        // var w = camera.scaledPixelWidth;
        var fov = camera.fieldOfView/2.0f * Mathf.Deg2Rad;
        var f = camera.farClipPlane;
        var n = camera.nearClipPlane;
        var k = Mathf.Sqrt(1 + 1.0f * (h * h) / (w * w)) * Mathf.Tan(fov);

        var C = Vector3.zero;
        var R = 0f;
        if(k* k >= 1.0f*(f - n)/(f+n))
        {
            C = new Vector3(0f, 0, f);
            R = f * k;
        }
        else
        {
            C = new Vector3(0, 0, 0.5f * (f + n) * (1 + k * k));
            R = 0.5f * Mathf.Sqrt((f - n) * (f - n) + 2 * (f * f + n * n) * k * k + (f + n) * (f + n) * k * k * k * k);
        }
        
        Gizmos.DrawWireSphere(C, R);        
    }

    void DrawRendererBounds()
    {
        var selObj = Selection.activeGameObject;
        if (selObj == null)
            return;
        
        var render = selObj.GetComponent<MeshRenderer>();
        if(render == null)
            return;
        
        var bound = render.bounds;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bound.center, bound.size);

    }
    
    void CaculateVP()
    {
        //m_light = GameObject.FindObjectOfType<Light>();
        // if (_renderers.Count == 0)
        // {
        var _renderers = new List<MeshRenderer>(GameObject.FindObjectsOfType<MeshRenderer>());

        var layer = LayerMask.NameToLayer("CustomShadow");
           
        for (int i = _renderers.Count - 1; i >= 0; i--)
        {
            if (_renderers[i].gameObject.layer != layer)
            {
                _renderers.RemoveAt(i);
            }
        }
        // DirectionalLight
        if ( _renderers.Count == 0)
        {
            return;
        }
        
        var center = Vector3.zero;
        
        foreach (var meshRenderer in _renderers)
        {
            var mBounds = meshRenderer.bounds;
            center += mBounds.center;
        }

        center /= _renderers.Count;
        var worldBounds = new Bounds(center, Vector3.one);
        
        //worldBounds.center = center;
        foreach (var meshRenderer in _renderers)
        {
            var mBounds = meshRenderer.bounds;
            worldBounds.Encapsulate(mBounds);
         //   Debug.Log($"{meshRenderer.gameObject.name}");
        }
        
        var radius = Vector3.Distance(worldBounds.center, worldBounds.max);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        
        Gizmos.DrawWireCube(worldBounds.center, radius * 2 * Vector3.one);
        // float extraDis = 0;
        // var lightDirection = m_light.transform.rotation * Vector3.forward;
        // sidePt = center + (-lightDirection.normalized) * (radius + extraDis);
        //
        // //Quaternion quat = Quaternion.LookRotation(light.direction);
        // viewMatrix = Matrix4x4.TRS(sidePt, m_light.transform.rotation, new Vector3(1,1,-1)).inverse;
        // projMatrix = Matrix4x4.Ortho(-radius, radius, -radius, radius, 0f, 2 * radius + extraDis);
        //projMatrix = GL.GetGPUProjectionMatrix(projMatrix, false);
    }
}
#endif