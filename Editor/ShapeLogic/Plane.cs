using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class Plane : ShapeInfo
{
    private Mesh _planeMesh;
    private Material _planeMaterial;

    private Texture2D _mask = Texture2D.whiteTexture;
    private Color _Color = Color.white;

    public Vector3 LightDir = Vector3.one;


    public Plane(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {

        Name = "Plane";

        _planeMesh = (Mesh)AssetDatabase.LoadAssetAtPath(Paths.Path_BasePlaneMesh, typeof(Mesh));
        _planeMaterial = new Material((Material)AssetDatabase.LoadAssetAtPath(Paths.Path_PlainPlaneMaterial, typeof(Material)));

        // .CopyPropertiesFromMaterial(mat)


        _planeMaterial.SetVector("_LightDir", new Vector3(0.2f, -0.7f, 0));

        //_planeMaterial.SetInt("CurrentEffect", SDF.Effect);
        // 0: Boolean
        DataLength = 6;
        Data = new float3[DataLength];
        ShapeType = 2;


        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;

        InitGeneralData();
    }

    public Plane()
    {
        Name = "Plane";

        _planeMesh = (Mesh)AssetDatabase.LoadAssetAtPath(Paths.Path_BasePlaneMesh, typeof(Mesh));
        _planeMaterial = new Material((Material)AssetDatabase.LoadAssetAtPath(Paths.Path_PlainPlaneMaterial, typeof(Material)));

        _planeMaterial.SetVector("_LightDir", new Vector3(0.2f, -0.7f, 0));
       
        DataLength = 6;
        Data = new float3[DataLength];
        ShapeType = 2;

        InitGeneralData();
    }

    public override void Draw()
    {

        if (SDF.VoxelEffect == 1) return;
        base.Draw();
        Graphics.DrawMesh(_planeMesh, Matrix4x4.Inverse(Transform ) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, _scale), _planeMaterial, 0);
    }

    public override void GUI()
    {
        base.GUI();       
    }

    public override void InspectorGUI()
    {
        GUILayout.BeginVertical("box");

        // Shape Name
        GUILayout.Label("General Shape Settings", GUIInfo.TitleSmall);
        Name = EditorGUILayout.TextField("Shape Name", Name);
        EditorGUI.BeginChangeCheck();

        Color color = EditorGUILayout.ColorField("Plane Color", _Color);

        if (EditorGUI.EndChangeCheck())
        {
            _planeMaterial.SetColor("_Color", color);
            _Color = color;
        }


        GUILayout.EndVertical();

        
    }
}
