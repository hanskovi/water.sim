// "Berekende bol" (impostor) voor de atomen van Water.sim.
// Elk atoom is één vierkantje dat naar de camera kijkt; per pixel wordt de echte bol uitgerekend
// (straal + diepte). Daardoor zijn bollen op elke afstand perfect rond en zijn de snijlijnen tussen
// O en H echte cirkels. Zelfde belichting en donkere rand als WaterSim/Atoom.
// Verwacht: vierkant van (-1,-1) tot (1,1) in xy; de instancematrix bepaalt middelpunt en grootte
// (schaal 2r, net als de Unity-bol met straal 0,5).
Shader "WaterSim/AtoomGlad"
{
    Properties
    {
        [MainColor] _BaseColor ("Kleur", Color) = (1, 1, 1, 1)
        _RandKleur ("Randkleur", Color) = (0.08, 0.13, 0.17, 1)
        _RandBreedte ("Randbreedte", Range(0, 1)) = 0.24
        _Glans ("Glans", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "AtoomGlad"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RandKleur;
                half _RandBreedte;
                half _Glans;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionVS : TEXCOORD0;   // punt op het vierkantje, in camerakoordinaten
                float4 bol : TEXCOORD1;          // middelpunt (xyz, camerakoordinaten) en straal (w)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float3 midW = TransformObjectToWorld(float3(0, 0, 0));
                float r = length(TransformObjectToWorld(float3(0.5, 0, 0)) - midW);
                float3 midV = TransformWorldToView(midW);
                // vierkantje iets groter dan de bol en een straal naar de camera toe (nooit achter het nabije vlak)
                float z = min(midV.z + r, -_ProjectionParams.y * 1.01);
                float3 p = float3(midV.xy + i.positionOS.xy * r * 1.5, z);
                o.positionVS = p;
                o.bol = float4(midV, r);
                o.positionCS = TransformWViewToHClip(p);
                return o;
            }

            half4 frag(Varyings i, out float diepte : SV_Depth) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // straal vanuit de camera (oorsprong) door deze pixel; snijpunt met de bol
                float3 d = normalize(i.positionVS);
                float3 c = i.bol.xyz;
                float r = i.bol.w;
                float b = dot(d, c);
                float disc = b * b - (dot(c, c) - r * r);
                if (disc < 0) discard;
                float t = b - sqrt(disc);
                if (t <= 0) discard;                       // camera zit in de bol
                float3 pV = d * t;
                float4 pC = TransformWViewToHClip(pV);
                diepte = pC.z / pC.w;

                float3 nV = (pV - c) / r;
                float3 n = normalize(mul((float3x3)UNITY_MATRIX_I_V, nV));
                float3 v = normalize(mul((float3x3)UNITY_MATRIX_I_V, -d));
                Light licht = GetMainLight();

                half nl = saturate(dot(n, licht.direction));
                half3 kleur = _BaseColor.rgb * (SampleSH(n) + licht.color * nl * 0.85);
                half3 h = normalize(licht.direction + v);
                kleur += pow(saturate(dot(n, h)), 48.0) * _Glans * licht.color;

                half rand = 1.0 - saturate(dot(n, v));
                half deel = smoothstep(1.0 - _RandBreedte, 1.0 - _RandBreedte * 0.5, rand);
                kleur = lerp(kleur, _RandKleur.rgb, deel);

                kleur = MixFog(kleur, ComputeFogFactor(pC.z));
                return half4(kleur, 1.0);
            }
            ENDHLSL
        }
    }
}
