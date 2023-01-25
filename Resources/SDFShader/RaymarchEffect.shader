// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Raymarching/RaymarchEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _ShapeTexture("Texture", 2D) = "white" {}
        _BrushTexture("Texture", 2D) = "black" {}
    //    _MouseWorld("MouseWorld", Color) = (1, 0, 0, 0)
    }

        CGINCLUDE

        // Function Includes
#include "UnityCG.cginc"
//#include "SDFFunctions.cginc"
#include "ModelMap.cginc"
#include "TreeFunctions.cginc"

#define PreComp

uniform sampler2D _CameraDepthTexture;
    sampler2D _MainTex;
    uniform float4 _MainTex_TexelSize;
    uniform float3 _CamWorldPos;
    uniform float4x4 _CamFrustum, _CameraInvViewMatrix;
    uniform float3 _LightDir;

    uniform RWStructuredBuffer<int> _ShapeList : register(u1);
    uniform RWStructuredBuffer<float3> _ShapeData : register(u5);
    uniform RWStructuredBuffer<float4x4> _TransformList : register(u2);
    uniform RWStructuredBuffer<float4x4> _SGTransformList : register(u6);
    

    // uv list
    uniform float2 _MouseUV;
    uniform RWStructuredBuffer<float4> _MouseWorldBuffer : register(u4);

    uniform int CurrentEffect;
    uniform int OutlineEffect;
    uniform int CelShadingEffect;
    uniform int MosaicEffect;
    uniform int VoxelEffect;

    sampler2D _ShapeTexture;
    sampler2D _BrushTexture;

    int dataIndex;
    int branchIndex;
    float3 ObjectColor = float3(1, 0, 1);

    void CheckColor(float d1, float d2, float3 col1, float3 col2)
    {
        ObjectColor = (d1 < d2) ? col1 : col2;
    }

    // x: meshbool y: color bool
    float Boolean(int index, float3 output, float3 newShape)
    {

        switch ((int)_ShapeData[index].x)
        {
            // Case Add
        case 0:
            CheckColor(newShape, output, _ShapeData[dataIndex + 2], ObjectColor);
            return bool_union(newShape, output);

            // Case Smooth
        case 1:

            ObjectColor = col_smooth(newShape, output, _ShapeData[dataIndex + 2], ObjectColor, .5);
            return bool_smooth(newShape, output, 0.5);

            // Case Subtract
        case 2:

            ObjectColor = ObjectColor;
            return bool_sub(newShape, output);
        }

        return bool_union(output, newShape);
    }

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : POSITION;
        float3 ray : TEXCOORD1;
        float4 worldPos : TEXCOORD2;
        float4 projPos : TEXCOORD3;
    };

    // x = output y = uv
    float3 MapModel(float3 p)
    {

#ifdef PreComp
        float3 outputModel = float3(sdSphere(p + float3(200, 0, 0), 0.1), 0, 0);

        float d2 = 1e10;
        float2 newUV = float2(0, 0);
       
        float3 tempP = p;

        dataIndex = 0;
        branchIndex = 0;
       
        for (uint x = 0; x < _ShapeList.Length; x++)
        {
            int function = _ShapeList[x];

            float3 scale = _ShapeData[dataIndex + 5];          

            float3 postion = _TransformList[x]._m03_m13_m23;

            float3 transformedP = mul(_TransformList[x], p) + postion;

            
            transformedP = twist(transformedP, _ShapeData[dataIndex + 4].x);
            transformedP = bend(transformedP, 2.0, _ShapeData[dataIndex + 3].x);
            

//            _SGTransformList[x];

            switch (function)
            {
            // YTREE
            case 1:
            case 8:
                for (int b = 0; b < (int)_ShapeData[dataIndex + 6].x; b++)
                {
                    float3 bPos = _BranchInformation[branchIndex].pos;
                    float3 ePos = _BranchInformation[branchIndex].ePos;

                    float3 w0 = _BranchInformation[branchIndex].w0;
                    float3 w2 = _BranchInformation[branchIndex].w2;

                    if (b == 0)
                    {
                        d2 = sdCappedCone(transformedP, bPos, ePos, w0, w2);
                        branchIndex++;
                        continue;
                    }

                    d2 = bool_union(d2, sdRoundCone(transformedP, bPos, ePos, w0, w2));

                    branchIndex++;
                }

                outputModel.x = Boolean(dataIndex + 1, outputModel.x, d2);

                break;

            case 9:
             //   float3 objColor = ObjectColor;
                float leaves = 1e10;

                for (int b = 0; b < (int)_ShapeData[dataIndex + 6].x; b++)
                {
                    float3 bPos = _BranchInformation[branchIndex].pos;
                    float3 ePos = _BranchInformation[branchIndex].ePos;

                    float3 w0 = _BranchInformation[branchIndex].w0;
                    float3 w2 = _BranchInformation[branchIndex].w2;

                    
                    if (b == 0)
                    {
                        
                       // ObjectColor = _ShapeData[dataIndex + 7];
                        leaves = sdSphere(transformedP - ePos - float3(0, 1.5, 0), float3(2, 2, 2));
                        leaves = bool_union(leaves, sdSphere(transformedP - ePos - float3(2, 0, 0), float3(1, 1, 1)));
                    }

                    
                    d2 = bool_union(d2, sdCappedCone(transformedP, bPos, ePos, w0, w2));

                 

                    branchIndex++;
                }
                    
                outputModel.x = Boolean(dataIndex + 1, outputModel.x, d2);
                CheckColor(d2, leaves, ObjectColor, _ShapeData[dataIndex + 7]);
                outputModel.x = bool_union(d2, leaves);

                break;

            // BEZIER
            case 2: 
                break;

            // SPHERE
            case 3:
                d2 = sdSphere(transformedP, scale);

                outputModel.yz = uv_bool(outputModel.x, d2, outputModel.yz, newUV);
                outputModel.x = Boolean(dataIndex + 1, outputModel.x, d2);
                break;

            // BOX
            case 4:

                d2 = sdBox(transformedP, scale);

                outputModel.yz = uv_bool(outputModel.x, d2, outputModel.yz, newUV);
                outputModel.x = Boolean(dataIndex + 1, outputModel.x, d2);
                break;

            // ROUNDED BOX
            case 6:             
                d2 = sdRoundBox(transformedP, scale, _ShapeData[dataIndex + 6]);

                outputModel.x = Boolean(dataIndex + 1, outputModel.x, d2);
                break;

            // CAPPED CONE
             case 7:

                d2 = sdCappedCylinder(transformedP, scale.y, _ShapeData[dataIndex + 6]);
                outputModel.x = Boolean(dataIndex + 1, outputModel.x, d2);
                break;
            }

            
           

            dataIndex += _ShapeData[dataIndex].x;
        }
          
#endif
        

        return outputModel;
    }

    ///// OUTLINE
    float4 shadeOutline(float3 pos, float t) {
        float alpha = smoothstep(0., 5., t);
        alpha -= smoothstep(5., 1., t);
        alpha *= .5;
        float3 color = float3(0, 0, 0);
        //applyFog(color, pos);
        return float4(color, alpha);
    }
    /// END OUTLINE

    float3 calcNormal(in float3 p)
    {
        // epsilon - used to approximate dx when taking the derivative
        const float2 eps = float2(0.001, 0.0);

        float3 nor = float3(
            MapModel(p + eps.xyy).x - MapModel(p - eps.xyy).x,
            MapModel(p + eps.yxy).x - MapModel(p - eps.yxy).x,
            MapModel(p + eps.yyx).x - MapModel(p - eps.yyx).x);

        return normalize(nor);
    }

    float unmix(float a, float b, float x) {
        return (x - a) / (b - a);
    }

    float4 voxel(float3 ro, float3 rd, float2 uv)
    {
        rd = normalize(rd);
        float s = 2.;
        ro *= s;
        float3 grid = floor(ro);
        float3 grid_step = sign(rd);
        float3 delta = (-frac(ro) + 0.5 * (grid_step + 1.0)) / rd;
        float3 delta_step = 1.0 / abs(rd);
        float3 mask = float3(0.0, 0.0, 0.0);
        float3 pos;
        bool hit = false;
        float d = 0.0;
        float t = 0.0;

        for (int i = 0; i < 128; i++)
        {
            pos = (grid + 0.5) / s;

            float3 c = step(delta, delta.yzx);
            if (t > 2000) continue;
            
            d = MapModel(pos).x;
            if (d < 0.01f)
            {
                hit = true;
                break;
            }
            
            mask = c * (1.0 - c.zxy);
            grid += grid_step * mask;
            delta += delta_step * mask;

            t += d;
        }

        float3 col;
        if (hit)
        {
            col = ObjectColor;
            float br = dot(float3(0.5, 0.9, 0.7), mask);
            float depth = dot(delta - delta_step, mask);
            col *= br;

        }
        else
        {
            return tex2D(_MainTex, uv);
        }

        return float4(col, 1.0);
    }

    float4 raymarch(float3 ro, float3 rd, float2 uv, float depth)
    {

        float4 ret = fixed4(0, 0, 0, 0);

        const int maxstep = 128;
        float t = 0; 

        [loop]
        for (int i = 0; i < maxstep; ++i) 
        {
            
            float3 p = ro + rd * t; 

            // last floats are uvs
            float3 d = MapModel(p);//  yBranch(p);//

            if (t > 200.0 || t >= depth)
            {
                return tex2D(_MainTex, uv);
                break;
            }            

            // HIT
            if (d.x < 0.001) {
                float3 norm = calcNormal(p) * 0.5;
                ret = tex2Dlod(_ShapeTexture, float4(d.yz, 0, 0));             

                float dScale = length(p - ro);
                float dVal = clamp(unmix(0.0, 100.0, dScale), 0.0, 1.0);
                float dA = 100.0; // higher gives more detail on nearer values
                float finalDepth = log(dA * dVal + 1.0) / log(dA + 1.0);
                
               // float4 shade = fixed4(.rrr, saturate(finalDepth + 0.01));
               // float3 lightDir = normalize(_WorldSpaceLightPos0 - p);
                float nS = dot(-_LightDir.xyz, normalize(norm));
                //float nS = dot(-_LightDir.xyz, norm);
                float3 shading = float3(nS, nS, nS);

                if(CelShadingEffect == 1)
                {
                    shading = cel_shading(normalize(norm), -_LightDir, float3(1, 1, 1));
                }

                float4 shade = float4(shading.xyz, saturate(finalDepth + 0.01));
                ret = float4(ObjectColor.xyz, 1)*shade;

                break;
            }

            t += d.x;

        }
        
        return ret;
    }
    ENDCG

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass // 0 Mouse Pass
        {
            CGPROGRAM

            #pragma target 5.0  

            #pragma vertex mouseVert
            #pragma fragment mouseFrag    

            v2f mouseVert(appdata v)
            {
                v2f o;

                half index = v.vertex.z;
                v.vertex.z = 0;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.projPos = ComputeScreenPos(o.vertex);
                o.uv = v.uv.xy;

#if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.uv.y = 1 - o.uv.y;
#endif

                // Get the eyespace view ray (normalized)
                o.ray = _CamFrustum[(int)index].xyz;

                o.ray /= abs(o.ray.z);

                // Transform the ray from eyespace to worldspace
                // Note: _CameraInvViewMatrix was provided by the script
                o.ray = mul(_CameraInvViewMatrix, o.ray);
                return o;
            }

            fixed4 mouseFrag(v2f i) : SV_Target
            {

                fixed4 col = tex2D(_MainTex, i.uv);
#ifdef PreComp

                float2 screenUV = i.projPos.xy / i.projPos.w;

                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenUV).r);
                float leng = length(_MouseUV - i.uv);

                if (leng < 0.001)
                {
                    col = fixed4(1, 0, 0, 0);
                    col = raymarch(_CamWorldPos, normalize(i.ray.xyz), i.uv, depth);
                    //RayMarch
                }
#endif

                return col;
            }

        ENDCG
        }

        Pass // 1 Main Raymarch Pass
        {
            CGPROGRAM

            #pragma target 5.0  

            #pragma vertex vert
            #pragma fragment frag        

            v2f vert(appdata v)
            {
                v2f o;

                half index = v.vertex.z;
                v.vertex.z = 0;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos(o.vertex);
                o.uv = v.uv.xy;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

#if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.uv.y = 1 - o.uv.y;
#endif

                float4 clip = float4((v.uv.xy * 2.0f - 1.0f) * float2(1, -1), 0.0f, 1.0f);

                o.ray = _CamFrustum[(int)index].xyz;

                o.ray /= abs(o.ray.z);
                o.ray = mul(_CameraInvViewMatrix, o.ray);
                return o;
            }       

            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenUV = i.projPos.xy / i.projPos.w;

                //float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy));
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));

#ifdef PreComp
            float4 output;

            if (VoxelEffect == 1)
            {
                return voxel(_CamWorldPos, normalize(i.ray.xyz), i.uv);
            }

            output = raymarch(_CamWorldPos, normalize(i.ray.xyz), i.uv, depth);


#endif

            return output;
            }           

        ENDCG
        }


        Pass // 2 POST-PROCESSING Pass
        {
                CGPROGRAM

                #pragma target 5.0  

                #pragma vertex ppVert
                #pragma fragment ppFrag              

                float isInInterval(float a, float b, float x) {
                    return step(a, x) * (1.0 - step(b, x));
                }

                void outlineCheck(in float2 uv, in float weight, in float aBase, inout float n) 
                {
                    float4 data = tex2D(_MainTex, uv);// textureLod(iChannel0, uv, 0.0);
                    float depth = data.a;

                    n += weight * (1.0 - isInInterval(aBase - 0.004, aBase + 0.004, depth));
                }

                float outline(in float2 uv, in float aBase) {
                    float2 uvPixel = 1.0 / _ScreenParams.xy;
                    float n = 0.0;

                    outlineCheck(uv + float2(1.0, 0.0) * uvPixel, (1.0 / 8.0), aBase, n);
                    outlineCheck(uv + float2(0.0, 1.0) * uvPixel, (1.0 / 8.0), aBase, n);
                    outlineCheck(uv + float2(0.0, -1.0) * uvPixel, (1.0 / 8.0), aBase, n);
                    outlineCheck(uv + float2(-1.0, 0.0) * uvPixel, (1.0 / 8.0), aBase, n);

                    outlineCheck(uv + float2(1.0, 1.0) * uvPixel, (1.0 / 8.0), aBase, n);
                    outlineCheck(uv + float2(1.0, -1.0) * uvPixel, (1.0 / 8.0), aBase, n);
                    outlineCheck(uv + float2(-1.0, 1.0) * uvPixel, (1.0 / 8.0), aBase, n);
                    outlineCheck(uv + float2(-1.0, -1.0) * uvPixel, (1.0 / 8.0), aBase, n);

                    return n;
                }

                v2f ppVert(appdata v)
                {
                    v2f o;

                    half index = v.vertex.z;
                    v.vertex.z = 0;

                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv.xy;

    #if UNITY_UV_STARTS_AT_TOP
                    if (_MainTex_TexelSize.y < 0)
                        o.uv.y = 1 - o.uv.y;
    #endif

                    return o;
                }

                fixed4 ppFrag(v2f i) : SV_Target
                {

                    fixed4 col = tex2D(_MainTex, i.uv);


#ifndef PRECOMP 

                if (OutlineEffect == 1)
                {
                    float outlineAmount = outline(i.uv, col.w);
                    float3 outlineColor = float3(0.0, 0.0, 0.0);
                    col =  float4(lerp(float3(col.x, col.y, col.z), outlineColor, outlineAmount * 0.8), 1.0);
                }
                if (MosaicEffect == 1)
                {
                    col = lerp(Mozaic(100, 100, i.uv, _MainTex), col, 0.5f);
                }
#endif

                return fixed4(col.xyz, 1.0);
                }

            ENDCG
            }

    }
}