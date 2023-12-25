Shader "Unlit/DrawShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,0,0,1)
    }
    SubShader
    {
        Tags {
				"RenderPipeline"="UniversalPipeline"}
                
        LOD 100

        Pass
        {

            Stencil {
                     Ref 200          //0-255
                     Comp NotEqual     //default:always

            }
            
            Tags{"LightMode" = "UniversalForward" }
            Blend DstColor Zero
            cull off
            ZTest off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
        //    #pragma multi_compile_fog

            //#include "UnityCG.cginc"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
               // The DeclareDepthTexture.hlsl file contains utilities for sampling the
            // Camera depth texture.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
           // #include "LitInput.hlsl"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv : TEXCOORD0;
              //  float3 world:TEXCOORD1;
              //  float4 screenPos:TEXCOORD2;
                
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;

            uniform  float texTexel;
            uniform float4x4 shadowVPMat;

           // uniform float4x4 _I_VP;
           // uniform sampler2D _CameraDepthTexture;

            TEXTURE2D_SHADOW(CustomShadowMap);
			SAMPLER_CMP(sampler_CustomShadowMap);
            
            v2f vert (appdata v)
            {
                v2f o;
                o.positionHCS = TransformObjectToHClip(v.vertex);
                float3 world = mul(unity_ObjectToWorld, v.vertex);
             //   o.world = world;
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                //o.screenPos = ComputeScreenPos(o.positionHCS);
                
             //   UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // real GetDepthFromSM(float2 uv)
            // {
            //     #if UNITY_REVERSED_Z
            //         real shadow_depth = tex2D(CustomShadowMap, uv).x;
            //         
            //     #else
            //         // Adjust Z to match NDC for OpenGL ([-1, 1])
            //         real shadow_depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, tex2D(CustomShadowMap, uv).x);
            //     #endif
            //     return shadow_depth;
            // }

            float3 GetShadowCoord(float3 wolrdPos)
            {
                return mul(shadowVPMat, float4(wolrdPos,1));
            }
            float4 frag (v2f i) : SV_Target
            {
               // return float4(1,0,0,1);
                
                // To calculate the UV coordinates for sampling the depth buffer,
                // divide the pixel location by the render target resolution
                // _ScaledScreenParams.
                float2 UV = i.positionHCS.xy / _ScaledScreenParams.xy;

                // Sample the depth from the Camera depth texture.
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    // Adjust Z to match NDC for OpenGL ([-1, 1])
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                // Reconstruct the world space positions.
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
                
          //       float4 shadowClip = mul(shadowVPMat, float4(worldPos,1));
          //       shadowClip = shadowClip/shadowClip.w;
          //       
          // //      return half4(shadowClip.x,0,0,1);
          //        if (shadowClip.x < - 1|| shadowClip.x > 1 ||shadowClip.y < - 1|| shadowClip.y > 1)
			       //   discard;
          //   //
          //  //  return float4(shadowClip.z,0,0,1);
          //   //
          //       
          //        float3 shadowInfo = float3(0.5* (shadowClip.xy + 1), shadowClip.z);
          //           
          //        float2  shadow_uv = float2(shadowInfo.x, shadowInfo.y);


          //  float visible =  SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap, float3(shadow_uv,  max(shadowInfo.z, 0.00001)));

                
            //     float calc_depth = 0.5 * (shadowInfo.z + 1);
                
           //     return float4(calc_depth,0,0,1);
           //   real shadow_depth = GetDepthFromSM(shadow_uv);
           //     return float4(shadow_depth,0,0,1);
           // //  float shadow_depth = tex2D(CustomShadowMap, shadow_uv).x;
           //   
           //   float visible = calc_depth>= shadow_depth?1:0;

                float3 shadowCoord = GetShadowCoord(worldPos);
          //      if (shadowCoord.x < 0|| shadowCoord.x > 1 ||shadowCoord.y < 0|| shadowCoord.y > 1 )
			       // discard;

                 #if UNITY_REVERSED_Z
                    float visible =  SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap,float3(shadowCoord.xy, max(0.00001, shadowCoord.z)));
                #else
                    float visible =  SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap, shadowCoord);
                #endif
                
                // float visible =0;
                //
                // float offset = texTexel * 0.5;
                // float3 shadow_coord = float3(shadow_uv,  max(shadowInfo.z, 0.00001));
                //     
                // visible = visible + SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap, shadow_coord + float3(-offset, offset, 0)) * 0.25;
                // visible = visible + SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap, shadow_coord + float3(-offset, -offset, 0)) * 0.25;
                // visible = visible + SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap, shadow_coord + float3(offset, offset, 0)) * 0.25;
                // visible = visible + SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap, shadow_coord + float3(offset, -offset, 0)) * 0.25;
                
                // visible = visible + (calc_depth>=GetDepthFromSM(shadow_uv + float2(-offset * 0.5, -offset))?1:0) * 0.2;
                // visible = visible + (calc_depth>=GetDepthFromSM(shadow_uv + float2(-offset, offset * 0.5))?1:0) * 0.2;
                // visible = visible + (calc_depth>=GetDepthFromSM(shadow_uv + float2(offset * 0.5, offset))?1:0) * 0.2;
                // visible = visible + (calc_depth>=GetDepthFromSM(shadow_uv + float2(offset, -offset * 0.5))?1:0) * 0.2;
             return visible * _BaseColor;
            }
            ENDHLSL
        }
    }
}
