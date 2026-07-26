Shader "Custom/TutorialShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 color : COLOR;
            };

            fixed4 _Color;
            float4 _Rects[16]; // Supports up to 16 cutouts
            int _RectCount;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex; // We use screen/local space for logic
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 screenPos = i.vertex.xy; 
                
                // Check if the current pixel is inside any of the rects
                for (int n = 0; n < _RectCount; n++) 
                {
                    float4 r = _Rects[n]; // x=minX, y=minY, z=maxX, w=maxY
                    if (screenPos.x >= r.x && screenPos.x <= r.z &&
                        screenPos.y >= r.y && screenPos.y <= r.w) {
                        discard; // Punch the hole
                    }
                }

                //return fixed4(i.vertex.x / _ScreenParams.x, i.vertex.y / _ScreenParams.y, 0, 1);
                return i.color;
            }
            ENDCG
        }
    }
}
