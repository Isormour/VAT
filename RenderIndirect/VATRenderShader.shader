Shader "VAT/VATIndirectBasic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PosTex ("VATAnimationTexture", 2D) = "white" {}
        _VATNormalTexture ("VATNormalTexture", 2D) = "white" {}
        _VATTangentTexture ("VATTangentTexture", 2D) = "white" {}
        _VATAnimationTime("VATAnimationTime",Float)  = 1
    }
    SubShader
    {
        Tags { "LightMode" = "UniversalForward" "RenderType"="Opaque"}
        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma glsl
            
            
            #pragma vertex vert
            #pragma fragment frag
            // make fog work

       
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define ts _PosTex_TexelSize
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

         

            struct shaderParams
            {
                float4x4 tranformMatrix;
                float animationTime;
            };
            struct appdata
            {
              
                float2 uv : TEXCOORD0;
                uint vertexId : SV_VertexID;
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal     : NORMAL;
            };

             StructuredBuffer<shaderParams> _ParamsBuffer;

            sampler2D _MainTex;

            Texture2D  _VATPositionTexture;
            Texture2D  _VATNormalTexture;
            Texture2D  _VATTangentTexture;
            
            SamplerState sampler_VATPositionTexture;
           
            float4 _WorldPos;
            float4 _PosTex_TexelSize;
            float4 _MainTex_ST;

            v2f vert (appdata v,uint vid : SV_VertexID, const uint instance_id: SV_InstanceID)
            {
               
                InitIndirectDrawArgs(0);
                uint cmdID = GetCommandID(0);
                uint ID = GetIndirectInstanceID(instance_id);
                float x = (vid + 0.5) * ts.x;
				float y =  _ParamsBuffer[ID].animationTime+_Time.x*(ID*0.01f);

				float4 position = _VATPositionTexture.SampleLevel(sampler_VATPositionTexture, float2(x, y),0);
                float4 normal = _VATNormalTexture.SampleLevel(sampler_VATPositionTexture, float2(x, y),0);
                float4 tangent = _VATTangentTexture.SampleLevel(sampler_VATPositionTexture, float2(x, y),0);

                position = mul(_ParamsBuffer[ID].tranformMatrix,position);


                v2f o;
                o.vertex = GetVertexPositionInputs(position).positionCS;
                o.normal = GetVertexNormalInputs(normal,tangent).normalWS.xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
              
                Light mainLight = GetMainLight();
                
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                half nl = max(0.3, dot(i.normal, mainLight.direction));
                
                // factor in the light color
                half4 diff = nl * half4(mainLight.color, 1);
                
            
                return col*diff;
            }
            ENDHLSL
        }
    }
}
