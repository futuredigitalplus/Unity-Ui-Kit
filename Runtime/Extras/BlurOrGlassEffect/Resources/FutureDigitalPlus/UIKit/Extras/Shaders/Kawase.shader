// One pass of a Kawase blur: the average of four bilinear taps placed diagonally around the pixel, at a distance that
// grows with the pass number (_Offset 0, 1, 2, ...). Run by BlurSource through Graphics.Blit, so it needs no render
// pipeline feature: the same plain CG shader works in the Built-in pipeline, URP and HDRP.
Shader "Hidden/FutureDigitalPlus/UIKit/Blur/Kawase"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Offset in texels", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * (_Offset + 0.5);
                half4 c = tex2D(_MainTex, i.uv + float2(-o.x, -o.y));
                c += tex2D(_MainTex, i.uv + float2( o.x, -o.y));
                c += tex2D(_MainTex, i.uv + float2(-o.x,  o.y));
                c += tex2D(_MainTex, i.uv + float2( o.x,  o.y));
                return c * 0.25h;
            }
            ENDCG
        }
    }
    Fallback Off
}
