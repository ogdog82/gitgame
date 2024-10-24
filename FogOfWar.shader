Shader "Custom/FogOfWar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LightIntensity ("Light Intensity", Float) = 1.0
        _TorchIntensity ("Torch Intensity", Float) = 1.0
        _TorchColor ("Torch Color", Color) = (1,0.8,0.6,1)
        _TileColor ("Tile Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha

        sampler2D _MainTex;
        float _LightIntensity;
        float _TorchIntensity;
        fixed4 _TorchColor;
        fixed4 _TileColor;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _TileColor;
            fixed3 torchLight = lerp(fixed3(1,1,1), _TorchColor.rgb, _TorchIntensity);
            o.Albedo = c.rgb * _LightIntensity * torchLight;
            o.Alpha = _LightIntensity;
        }
        ENDCG
    }
    FallBack "Diffuse"
}