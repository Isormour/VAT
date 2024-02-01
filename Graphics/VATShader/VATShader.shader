Shader "VAT/VATShaderBasic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PosTex ("VATAnimationTexture", 2D) = "white" {}
        _VATNormalTexture ("VATNormalTexture", 2D) = "white" {}
        _VATTangentTexture ("VATTangentTexture", 2D) = "white" {}
        _VATAnimationTime("VATAnimationTime",Float)  = 1
        _VertexCount("VertexCount",int) = 10
        _TextureWidth("TextureWidth",int) = 4096
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

            sampler2D _MainTex;
            sampler2D _PosTex;
            sampler2D _VATNormalTexture;
            sampler2D _VATTangentTexture;

            float4 _PosTex_TexelSize;
            float4 _MainTex_ST;
            uint _VertexCount;
            uint _TextureWidth;
            float _VATAnimationTime;

            v2f vert (appdata v,uint vid : SV_VertexID)
            {
                v2f o;
              
                float x = (vid + 0.5) * ts.x;
				float y = _VATAnimationTime;
				float4 pos = tex2Dlod(_PosTex, float4(x, y, 0.0, 0.0));
                float4 normal = tex2Dlod(_VATNormalTexture,float4(x,y,0,0));
                float4 tangent = tex2Dlod(_VATTangentTexture,float4(x,y,0,0));

                o.vertex = GetVertexPositionInputs(pos).positionCS;
                o.normal =GetVertexNormalInputs(normal,tangent).normalWS.xyz;;
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
