using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;


public static class QuaternionExt
{
    public static Quaternion GetNormalized(this Quaternion q)
    {
        float f = 1f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return new Quaternion(q.x * f, q.y * f, q.z * f, q.w * f);
    }
}

[ExecuteInEditMode]
public class SDFRenderer : MonoBehaviour
{
#if UNITY_EDITOR

    ImageEffectRM RayMarchEffect;
    SceneView SceneViewVar = null;
    bool SceneLoaded = false;

    public void OnEnable() => Enabled();

    public void Enabled()
    {  
        // Disable transform tools
        transform.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;
        SceneLoaded = false;
    }

  

    public void Update()
    {
        if (SDF.ShapeGroups.Count <= 0) SDF.ShapeGroups.Add(new ShapeGroup(new Box())); 

        if (SceneView.lastActiveSceneView != null && !SceneLoaded)
        {
            SceneViewVar = SceneView.lastActiveSceneView;

           
            SceneLoaded = true;

            GameObject currentCamObj = SceneViewVar.camera.gameObject;
            RayMarchEffect = currentCamObj.GetComponent<ImageEffectRM>() ? null : currentCamObj.AddComponent<ImageEffectRM>();
        }

        if (GUI.changed) EditorUtility.SetDirty(SceneViewVar);

        // Reactivate Tools when Object is not selected
        if (Tools.hidden) Tools.hidden = false;
        if (Selection.activeGameObject == gameObject) Tools.hidden = true;
    }

    public void OnDisable()
    {
        SceneLoaded = false;
        var sceneView = SceneView.lastActiveSceneView;
        if (!sceneView) return;

        GameObject currentCamObj = sceneView.camera.gameObject;

        var obj = currentCamObj.GetComponent<ImageEffectRM>();     

        if (obj) DestroyImmediate(obj);
    }

#endif

}

[ExecuteInEditMode]
public class ImageEffectRM : MonoBehaviour
{
    public Texture2D texture;

    Material ImageEffect;

    Vector4[] data = new Vector4[2];

    public void OnEnable()
    {
        ImageEffect = new Material(Shader.Find("Raymarching/RaymarchEffect"));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (SDF.GetShapeList().Length <= 0) return;

        // General Shape Buffer
        ComputeBuffer ShapeInformation = new ComputeBuffer(SDF.GetShapeList().Length, sizeof(int));
        Graphics.SetRandomWriteTarget(1, ShapeInformation, false);

        // Shape Data Buffer (All Information Per Shape)
        ComputeBuffer ShapeData = new ComputeBuffer(SDF.GetShapeDataList().Length, sizeof(float) * 3);
        Graphics.SetRandomWriteTarget(5, ShapeData, false);

        // Transforms Of Shapes Buffer
        ComputeBuffer TransformInformation = new ComputeBuffer(SDF.GetTransformList().Length, sizeof(float) * 4 * 4);
        Graphics.SetRandomWriteTarget(2, TransformInformation, false);

        // Buffer that holds Mouse Position Compared To The RayMarch
        ComputeBuffer MouseWorld = new ComputeBuffer(data.Length, sizeof(float) * 4);
        Graphics.SetRandomWriteTarget(4, MouseWorld, false);

        MouseWorld.SetData(new Vector4[2] { new Vector4( 300000f, 300000f, 300000f, 3), Vector4.one });
        ShapeInformation.SetData(SDF.GetShapeList());
        ShapeData.SetData(SDF.GetShapeDataList());
        TransformInformation.SetData(SDF.GetTransformList());

        SDF.BranchInfo = SDF.GetBranchInfo();
        if (SDF.BranchInfo.Count < 1) SDF.BranchInfo.Add(new BranchInformation());
        ComputeBuffer BranchInformation = new ComputeBuffer(SDF.BranchInfo.Count, sizeof(float) * 10, ComputeBufferType.Structured);
        Graphics.SetRandomWriteTarget(3, BranchInformation, true);
        BranchInformation.SetData(SDF.BranchInfo);


        ComputeBuffer ShapeGroupTransforms = new ComputeBuffer(SDF.GetShaderGroupTransforms().Length, sizeof(float) * 16, ComputeBufferType.Structured);
        Graphics.SetRandomWriteTarget(6, ShapeGroupTransforms, true);
        ShapeGroupTransforms.SetData(SDF.GetShaderGroupTransforms());

        ImageEffect.SetVector("_MouseUV", (new Vector2(Event.current.mousePosition.x / Screen.width, 1 - (Event.current.mousePosition.y * 1.1f) / Screen.height)));
        ImageEffect.SetBuffer("_ShapeList", ShapeInformation);
        ImageEffect.SetBuffer("_ShapeData", ShapeData);
        ImageEffect.SetBuffer("_TransformList", TransformInformation);
        ImageEffect.SetBuffer("_SGTransformList", ShapeGroupTransforms);
        ImageEffect.SetBuffer("_MouseWorldBuffer", MouseWorld);
        
        ImageEffect.SetMatrix("_CamFrustum", GetFrustumCorners(SceneView.lastActiveSceneView.camera));
        ImageEffect.SetMatrix("_CameraInvViewMatrix", SceneView.lastActiveSceneView.camera.cameraToWorldMatrix);
        ImageEffect.SetVector("_CamWorldPos", SceneView.lastActiveSceneView.camera.transform.position);

        Light[] lights = FindObjectsOfType(typeof(Light)) as Light[];

        SDF.SetLightDataPlanes(lights[0].transform.forward);

        ImageEffect.SetVector("_LightDir", lights[0].transform.forward);
        ImageEffect.SetInt("OutlineEffect", SDF.OutlineEffect);
        ImageEffect.SetInt("MosaicEffect", SDF.MosaicEffect);
        ImageEffect.SetInt("CelShadingEffect", SDF.CelShadingEffect);
        ImageEffect.SetInt("VoxelEffect", SDF.VoxelEffect);

        ImageEffect.SetTexture("_ShapeTexture", texture);       

        RenderTexture.active = destination;

        CustomGraphicsBlit(source, destination, ImageEffect, 0);

        CustomGraphicsBlit(destination, source, ImageEffect, 1);

        CustomGraphicsBlit(source, destination, ImageEffect, 2);

        MouseWorld.GetData(data);

        MouseWorld.Dispose();
        ShapeInformation.Dispose();
        ShapeData.Dispose();
        TransformInformation.Dispose();
        BranchInformation.Dispose();
        ShapeGroupTransforms.Dispose();
    }

    static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
    {
        RenderTexture.active = dest;

        fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();

        fxMaterial.SetPass(passNr);

        GL.Begin(GL.QUADS);

        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }

    private Matrix4x4 GetFrustumCorners(Camera cam)
    {
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 toRight = Vector3.right * tan_fov * camAspect;
        Vector3 toTop = Vector3.up * tan_fov;

        Vector3 topLeft = (-Vector3.forward - toRight + toTop);
        Vector3 topRight = (-Vector3.forward + toRight + toTop);
        Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
        Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        return frustumCorners;
    }
}