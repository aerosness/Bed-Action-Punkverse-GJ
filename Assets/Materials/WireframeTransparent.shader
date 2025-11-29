Shader "VR/SpatialMapping/WireframeFancy"
{
    Properties
    {
        _TintColor      ("Tint Color", Color) = (0.2, 1.0, 0.3, 1.0)

        _LineColor      ("Line Color", Color) = (1, 1, 1, 1)
        _WireThickness  ("Wire Thickness", Range(0, 800)) = 100
        _Alpha          ("Line Alpha", Range(0, 1)) = 0.4

        _GlowColor      ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowAlpha      ("Glow Alpha", Range(0, 2)) = 0.8
        _GlowWidth      ("Glow Width", Range(0.1, 3.0)) = 1.5

        _PulseSpeed     ("Pulse Speed", Range(0, 10)) = 2.0
        _PulseAmplitude ("Pulse Amplitude", Range(0, 1)) = 0.4

        _DistanceFadeStart ("Fade Start Distance", Range(0, 200)) = 0.0
        _DistanceFadeEnd   ("Fade End Distance",   Range(0, 200)) = 100.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _TintColor;

            float4 _LineColor;
            float  _Alpha;

            float4 _GlowColor;
            float  _GlowAlpha;
            float  _GlowWidth;

            float  _WireThickness;

            float  _PulseSpeed;
            float  _PulseAmplitude;

            float  _DistanceFadeStart;
            float  _DistanceFadeEnd;

            struct appdata 
            { 
                float4 vertex : POSITION; 
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2g 
            {
                float4 projectionSpaceVertex : SV_POSITION;
                float4 worldSpacePosition    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO_EYE_INDEX
            };

            struct g2f 
            {
                float4 projectionSpaceVertex : SV_POSITION;
                float4 worldSpacePosition    : TEXCOORD0;
                float4 dist                  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2g vert (appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT_STEREO_EYE_INDEX(o);

                o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
                o.worldSpacePosition    = mul(unity_ObjectToWorld, v.vertex);

                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g i[3], inout TriangleStream<g2f> stream)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i[0]);

                float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
                float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
                float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;

                float2 e0 = p2 - p1;
                float2 e1 = p2 - p0;
                float2 e2 = p1 - p0;

                float area = abs(e1.x * e2.y - e1.y * e2.x);
                float wt   = 800 - _WireThickness;

                g2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // v0
                o.worldSpacePosition    = i[0].worldSpacePosition;
                o.projectionSpaceVertex = i[0].projectionSpaceVertex;
                o.dist.xyz = float3((area / length(e0)), 0, 0) * o.projectionSpaceVertex.w * wt;
                o.dist.w   = 1.0 / o.projectionSpaceVertex.w;
                stream.Append(o);

                // v1
                o.worldSpacePosition    = i[1].worldSpacePosition;
                o.projectionSpaceVertex = i[1].projectionSpaceVertex;
                o.dist.xyz = float3(0, (area / length(e1)), 0) * o.projectionSpaceVertex.w * wt;
                o.dist.w   = 1.0 / o.projectionSpaceVertex.w;
                stream.Append(o);

                // v2
                o.worldSpacePosition    = i[2].worldSpacePosition;
                o.projectionSpaceVertex = i[2].projectionSpaceVertex;
                o.dist.xyz = float3(0, 0, (area / length(e2))) * o.projectionSpaceVertex.w * wt;
                o.dist.w   = 1.0 / o.projectionSpaceVertex.w;
                stream.Append(o);
            }

            float4 frag(g2f i) : SV_Target
            {
                float d = min(i.dist.x, min(i.dist.y, i.dist.z)) * i.dist.w;
                if (d > 0.9) 
                    return float4(0,0,0,0);

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmplitude;

                float core = exp2(-2 * d * d) * pulse;
                float glow = exp2(-2 * (d * _GlowWidth) * (d * _GlowWidth)) * pulse;

                // безопасный fade по расстоянию
                float dist  = distance(_WorldSpaceCameraPos, i.worldSpacePosition.xyz);
                float fade  = 1.0;
                float denom = (_DistanceFadeEnd - _DistanceFadeStart);
                if (denom > 0.001)
                {
                    fade = saturate((_DistanceFadeEnd - dist) / denom);
                }

                float3 lineColor = _TintColor.rgb * _LineColor.rgb * core;
                float3 glowColor = _TintColor.rgb * _GlowColor.rgb * glow * _GlowAlpha;

                float alpha = (core * _Alpha + glow * _GlowAlpha) * fade;
                float3 rgb  = (lineColor + glowColor) * fade;

                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
}
