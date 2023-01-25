// Shapes
// functions based on https://iquilezles.org/articles/distfunctions/

#include "Effects.cginc"

#define PI 3.141592653589793

struct BranchInformation
{
    float l;
    float angle;
    float w0;
    float w2;
    float3 pos;
    float3 ePos;
};  

uniform RWStructuredBuffer<BranchInformation> _BranchInformation : register(u3);

// Booleans
// Normal add
float bool_union(float d1, float d2)
{
    return (d1 < d2) ? d1 : d2;
}

float2 uv_bool(float d1, float d2, float2 uv1, float2 uv2)
{
    return (d1 < d2) ? uv1 : uv2;
}

float3 col_smooth(float d1, float d2, float3 col1, float3 col2, float k)
{
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(col2, col1, h) - k * h * (1.0 - h);
}

float bool_sub(float d1, float d2) 
{
    return max(-d1, d2); 
}

float bool_smooth(float d1, float d2, float k) 
{
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
}

float3 twist(in float3 p, float k)
{
    if (abs(k) < 0.001) return p;
    float c = cos(k * p.y);
    float s = sin(k * p.y);
    float2x2 m = float2x2(c, -s, s, c);
    return float3(mul(p.xz, m).x, p.y, mul(p.xz, m).y);
}

float3 bend(in float3 p, in float l, in float a)
{
    if (abs(a) < 0.001) return p;

    float ra = 0.5 * l / a;
    p.x -= ra;

    float2 sc = float2(sin(a), cos(a));
    float2 q = p.xy - 2.0 * sc * max(0.0, dot(sc, p.xy));

    float s = sign(a);
    return float3((p.y > 0.0) ? ra - s * length(q) : sign(-s * p.x) * (q.x + ra),
        (p.y > 0.0) ? ra * atan2(s * p.y, -s * p.x) : (s * p.x < 0.0) ? p.y : l - p.y, p.z);
}

// SPHERE
float sdSphere(float3 p, float3 s)
{
    float k0 = length(p / s);
    float k1 = length(p / (s * s));
    return k0 * (k0 - 1.0) / k1;
}

// BOX
float sdBox(float3 p, float3 b)
{
	float3 q = abs(p) - b;
	return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

// ROUNDED BOX
float sdRoundBox(float3 p, float3 b, float r)
{
    float3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - r;
}

// NON INFINITE PLANE
float sdExactPlane(float3 p, float3 s)
{
    float3 q = abs(p) - s;
    return length(max(q, 0.0)) + min(max(q.x, max(0.1, q.z)), 0.0);
}

// PLANE
float sdPlane(float3 p, float3 n, float h)
{
    // n must be normalized
    return dot(p, n) + h;
}

// CYLINDER
float sdCappedCylinder(float3 p, float h, float r)
{
    float2 d = abs(float2(length(p.xz), p.y)) - float2(r, h);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}
// CAPPED CONE
float sdCappedCone(float3 p, float h, float r1, float r2)
{
    float2 q = float2(length(p.xz), p.y);
    float2 k1 = float2(r2, h);
    float2 k2 = float2(r2 - r1, 2.0 * h);
    float2 ca = float2(q.x - min(q.x, (q.y < 0.0) ? r1 : r2), abs(q.y) - h);
    float2 cb = q - k1 + k2 * clamp(dot(k1 - q, k2) / dot(k2, k2), 0.0, 1.0);
    float s = (cb.x < 0.0 && ca.y < 0.0) ? -1.0 : 1.0;
    return s * sqrt(min(dot(ca, ca), dot(cb, cb)));
}

//float sdCappedCylinder(float3 p, float h, float r)
//{
//    float2 d = abs(float2(length(p.xz), p.y)) - float2(r, h);
//    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
//}

float sdCappedCone(float3 p, float3 a, float3 b, float ra, float rb)
{
    float rba = rb - ra;
    float baba = dot(b - a, b - a);
    float papa = dot(p - a, p - a);
    float paba = dot(p - a, b - a) / baba;
    float x = sqrt(papa - paba * paba * baba);
    float cax = max(0.0, x - ((paba < 0.5) ? ra : rb));
    float cay = abs(paba - 0.5) - 0.5;
    float k = rba * rba + baba;
    float f = clamp((rba * (x - ra) + paba * baba) / k, 0.0, 1.0);
    float cbx = x - ra - f * rba;
    float cby = paba - f;
    float s = (cbx < 0.0 && cay < 0.0) ? -1.0 : 1.0;
    return s * sqrt(min(cax * cax + cay * cay * baba,
        cbx * cbx + cby * cby * baba));
}

// ROUND CONE
float sdRoundCone(float3 p, float r1, float r2, float h)
{
    // sampling independent computations (only depend on shape)
    float b = (r1 - r2) / h;
    float a = sqrt(1.0 - b * b);

    // sampling dependant computations
    float2 q = float2(length(p.xz), p.y);
    float k = dot(q, float2(-b, a));
    if (k < 0.0) return length(q) - r1;
    if (k > a * h) return length(q - float2(0.0, h)) - r2;
    return dot(q, float2(a, b)) - r1;
}

float sdRoundCone(float3 p, float3 a, float3 b, float r1, float r2)
{
    // sampling independent computations (only depend on shape)
    float3  ba = b - a;
    float l2 = dot(ba, ba);
    float rr = r1 - r2;
    float a2 = l2 - rr * rr;
    float il2 = 1.0 / l2;

    // sampling dependant computations
    float3 pa = p - a;
    float y = dot(pa, ba);
    float z = y - l2;
    float x2 = dot(pa * l2 - ba * y, pa * l2 - ba * y);
    float y2 = y * y * l2;
    float z2 = z * z * l2;

    // single square root!
    float k = sign(rr) * rr * rr * x2;
    if (sign(z) * a2 * z2 > k) return  sqrt(x2 + z2) * il2 - r2;
    if (sign(y) * a2 * y2 < k) return  sqrt(x2 + y2) * il2 - r1;
    return (sqrt(x2 * a2 * il2) + y * rr) * il2 - r1;
}


// RANDOM FUNCTIONS
float hash(float p)
{
    return frac(sin(dot(p.rr, float2(89.44, 19.36))) * 22189.22);
}

float3 hash_33(float3 p3)
{
    p3 = frac(p3 * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yxz + 33.33);
    return frac((p3.xxy + p3.yxx) * p3.zyx);
}

float random(in float x) {
    return frac(sin(x) * 1e4);
}

// HELPER FUNCTIONS
float3 slerp(float3 p0, float3 p1, float t)
{
    float dotp = dot(normalize(p0), normalize(p1));
    if ((dotp > 0.9999) || (dotp < -0.9999))
    {
        if (t <= 0.5)
            return p0;
        return p1;
    }
    float theta = acos(dotp);
    float3 P = ((p0 * sin((1 - t) * theta) + p1 * sin(t * theta)) / sin(theta));

    return P;
}

float3x3 rotateZ(float theta) {
    float c = cos(theta);
    float s = sin(theta);
    return float3x3(
        float3(c, -s, 0),
        float3(s, c, 0),
        float3(0, 0, 1)
    );
}

float3x3 rotateY(float theta) {
    float c = cos(theta);
    float s = sin(theta);
    return float3x3(
        float3(c, 0, s),
        float3(0, 1, 0),
        float3(-s, 0, c)
    );
}

// REPETITION
float3 opRepLim(in float3 p, in float c, in float3 l)
{
    float3 q = p - c * clamp(round(p / c), -l, l);
    return q;
}

// LIGHTING
float3 cel_shading(float3 n, float3 lightDir, float3 lightColor)
{
    float3 color = float3(0.1, 0.1, 0.1);
    float intensity = dot(n, normalize(lightDir));
    intensity = ceil(intensity * 4) / 4;
    intensity = max(intensity, 0.1);
    color = lightColor * intensity;
    return color;
}
