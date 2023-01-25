using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

public class LeafObject : ShapeInfo
{
    // Distibute over sphere and Cube
    private Leaves _leaves = new();

    private int _Count = 80;
    private float _MinSize = 0.1f;
    private float _MaxSize = 1f;
    public float _Radius = 5f;


    public LeafObject(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {
        Name = "Leaf Object";

        // 0: Boolean
        DataLength = 6;
        Data = new float3[DataLength];
        ShapeType = 5;

        InitGeneralData();


        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;

        RandomizeSphere(_Radius);
    }

    public LeafObject() 
    {
        Name = "Leaf Object";
        
        // 0: Boolean
        DataLength = 6;
        Data = new float3[DataLength];
        ShapeType = 5;
 
        InitGeneralData();

        RandomizeSphere(_Radius);
    }

    public override void GUI()
    {
        EditorGUI.BeginChangeCheck();

        //_m03_m13_m23

        switch (Tools.current)
        {
            case Tool.Move:
                Vector3 newPosition = Handles.PositionHandle(_position, Quaternion.identity);
                EditTransformGUI(ref _position, newPosition);
                break;

            case Tool.Rotate:
                Quaternion newRotation = Handles.RotationHandle(quaternion.identity, _position);
                
                break;

            case Tool.Scale:
                Vector3 newScale = Handles.ScaleHandle(Vector3.zero, _position, quaternion.identity);
                break;
        }
        
    }


    private void EditTransformGUI<T>(ref T oldValue, T newValue)
    {
        if (EditorGUI.EndChangeCheck())
        {
            oldValue = newValue;
            Transform = Matrix4x4.TRS(_position, Quaternion.identity, Vector3.one).inverse;
            RandomizeSphere(_Radius);
        }
    }
    public override void InspectorGUI()
    {
        _leaves.InspectorGUI();

        GUILayout.BeginVertical("box");

    //     private int _Count = 80;
    //private float _MinSize = 0.1f;
    //private float _MaxSize = 1f;
    //private float _Radius = 5f;

    // Shape Name
     GUILayout.Label("General Shape Settings", GUIInfo.TitleSmall);
        Name = EditorGUILayout.TextField("Shape Name", Name);
        EditorGUI.BeginChangeCheck();

        int count = (int)EditorGUILayout.Slider("Count", _Count, 1, 200);
        float minSize = EditorGUILayout.Slider("Minimum Size", _MinSize, 0.1f, 2f);
        float maxSize = EditorGUILayout.Slider("Maximum Size", _MaxSize, minSize, minSize + 2f);
        float radius = EditorGUILayout.Slider("Radius", _Radius, 0.1f, 5f);

        if (EditorGUI.EndChangeCheck())
        {
            _Count = count;
            // _planeMaterial.SetTexture("_Color", color);
            //SDF.Effect = (int)newShape;

            _MinSize = minSize;
            _MaxSize = maxSize;
           _Radius = radius;

            _leaves.UpdateColor();

          //  Debug.Log(_Radius);

            RandomizeSphere(_Radius);
        }


        GUILayout.EndVertical();
    }

    public override void Draw()
    {
        if (SDF.VoxelEffect == 1) return;
        _leaves.Draw();
    }

    public void RandomizeSphere(float r)
    {
        _leaves.Transforms.Clear();
        for (int i = 0; i < _Count; i++)
        {
            float x = UnityEngine.Random.Range(-r, r);
            float y = UnityEngine.Random.Range(-r, r);
            float z = UnityEngine.Random.Range(-r, r);

            float size = UnityEngine.Random.Range(_MinSize, _MaxSize);

            _leaves.Transforms.Add(Matrix4x4.TRS(_position + new Vector3(x, y, z), Quaternion.Euler(x * 90, y * 90, z * 90), new Vector3(size, size, size)));
        }
       
    }

}


public class Leaves
{
    private Material _leafMaterial;
    public Mesh _planeMesh;
    private Texture2D _mask;

    private Color _Color;
    private Vector4[] _colorList = new Vector4[1023];

    public List<Matrix4x4> Transforms = new();

    private MaterialPropertyBlock block;


    public int Count = 4;

    public float MinSize = .2f;
    public float MaxSize = .5f;

    private Gradient _gadient = new Gradient();
    

    public Leaves()
    {
        _leafMaterial = (Material)AssetDatabase.LoadAssetAtPath(Paths.Path_LeafMaterial, typeof(Material));
        _planeMesh = (Mesh)AssetDatabase.LoadAssetAtPath(Paths.Path_BaseLeaf, typeof(Mesh));

        _leafMaterial.enableInstancing = true;

        block = new MaterialPropertyBlock();

        _leafMaterial.SetVector("_LightDir", new Vector3(0.2f, -0.7f, 0));
      
       // _colorList?.Clear();
        for (int i = 0; i < Transforms.Count; i++)
        {
            _colorList[i] = (_Color/* * _gadient.colorKeys[Random.Range(0, _gadient.colorKeys.Length - 1)].color*/);
        }

        block.SetVectorArray("_Colors", _colorList);
    }

    public void InspectorGUI()
    {

        EditorGUILayout.BeginVertical("box");

        GUILayout.Label(" Leaf Settings", GUIInfo.TitleSmall);

        _planeMesh = (Mesh)EditorGUILayout.ObjectField("Leaf Mesh", _planeMesh, typeof(Mesh), true);
        Color newC =  EditorGUILayout.ColorField("Plane Color", _Color);
        Gradient gradient = EditorGUILayout.GradientField("Leaf Gradient", _gadient);

        if (EditorGUI.EndChangeCheck())
        {
            _Color = newC;
            _gadient = gradient;

            UpdateColor();
            _leafMaterial.SetVector("_LightDir", new Vector3(0.2f, -0.7f, 0));
        }

        EditorGUILayout.EndVertical();
    }

    

    public void UpdateColor()
    {
        //_colorList.Clear();
        for (int i = 0; i < Transforms.Count; i++)
        {

            _colorList[i] = (_Color * _gadient.Evaluate((float)i / Transforms.Count));
        }
        block.SetVectorArray("_Colors", _colorList);

    }

    public void Draw()
    {
        Graphics.DrawMeshInstanced(_planeMesh, 0, _leafMaterial, Transforms, block);
    }
}
