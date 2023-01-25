using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class Pine : ShapeInfo
{
    public List<BranchInformation> _branches = new();
    private Leaves _leaves;

    public int BranchCount = 3;

    private int _leafCount = 5;

    public float BranchLength = 5f;
    public float BranchLengthMul = 0.8f;
    public float BranchWidth = 0.5f;
    public float BranchWidthMul = 0.5f;
    public int BranchGroupCount = 3;


    public float TrunkWidthBottom = 1f;
    public float TrunkWidthTop = 0.5f;

    public float TrunkLength = 4;

    public int LeaveCount = 5;

    public Pine()
    {
        Name = "Pine Tree";
        ShapeType = 8;
        // Data
        // 6 - standard + custom
        DataLength = 6 + 1;
        Data = new float3[DataLength];

        Data[6] = _branches.Count;

        _leaves = new();
        _leaves._planeMesh = (Mesh)AssetDatabase.LoadAssetAtPath(Paths.Path_PineMesh, typeof(Mesh));

        InitGeneralData();

        Generate();
    }

    public Pine(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {
        Name = "Pine Tree";
        ShapeType = 8;
        // Data
        // 6 - standard + custom
        DataLength = 6 + 1;
        Data = new float3[DataLength];

        Data[6] = _branches.Count;

        _leaves = new();
        _leaves._planeMesh = (Mesh)AssetDatabase.LoadAssetAtPath(Paths.Path_PineMesh, typeof(Mesh));

        InitGeneralData();

        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;

        Generate();
    }

    public void Generate()
    {
        _leaves.Transforms?.Clear();
        _branches?.Clear();

        BranchInformation trunk = SetBranchInfo(0.2f, 0, TrunkWidthBottom, TrunkWidthTop, new float3(0, 0, 0), new float3(0, TrunkLength, 0));
        _branches.Add(trunk);

        float branchStep = TrunkLength / BranchCount;

        float bL = BranchLength;

        for (int i = 0; i < BranchCount; i++)
        {
            float3 bBranch = new Vector3(0, branchStep * i, 0);
            float3 eBranch = bBranch + new float3(1, 0, 0) * (bL);

            bL *= BranchLengthMul;

        //    float bWidth = BranchWidth * (BranchWidthMul * i);

            Quaternion rRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 275f), Vector3.up);

            for (int j = 0; j < BranchGroupCount; j++)
            {
                float angle = 360f / BranchGroupCount;
                float3 neBranch = Quaternion.AngleAxis(angle * j, Vector3.up) * eBranch;

                neBranch = rRotation * neBranch;

                BranchInformation newB = SetBranchInfo(0.2f, 0, BranchWidth, BranchWidth * BranchWidthMul, bBranch, neBranch);
                _branches.Add(newB);

                float3 dir = Vector3.Normalize(neBranch - bBranch);

                for (int k = 1; k < _leafCount; k++)
                {
                    float far = bL / _leafCount * k;
                    float3 pos = bBranch + dir * far;
                    Matrix4x4 t = (Matrix4x4)Transform;
                    Matrix4x4 newLeaf = t.inverse * Matrix4x4.TRS(pos, rRotation * Quaternion.AngleAxis(angle * j, Vector3.up) * Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), dir), Vector3.one * 0.5f);
              
                    _leaves.Transforms.Add(newLeaf);
                }
            }

            // Leaves

        }

        Data[6] = _branches.Count;
    }

    public override void Draw()
    {
        _leaves.Draw();
    }

    public override void InspectorGUI()
    {
        base.InspectorGUI();

        GUILayout.BeginVertical("box");

        GUILayout.Label(Name + " Settings", GUIInfo.TitleSmall);

        if (GUILayout.Button("Update"))
        {
            Generate();
        }

       GUILayout.Label("Leaf Count");
        int leafCount = (int)EditorGUILayout.Slider(_leafCount, 0, 6);

        //EditorGUI.BeginChangeCheck();
        //int leafCount = (int)EditorGUILayout.Slider(_LeafCount, 0, 8);

        // Shape Name
        GUILayout.Label("Branch Settings", GUIInfo.TitleSmall);

        EditorGUILayout.Space();

        GUILayout.Label("Branch Width");
        float bWidth = EditorGUILayout.Slider(BranchWidth, 0f, 6f);

        GUILayout.Label("Branch Width Multi");
        float bWidthMul = EditorGUILayout.Slider(BranchWidthMul, 0f, 1f);

        GUILayout.Label("Branch Count");
        int bCount = (int)EditorGUILayout.Slider(BranchCount, 0, 10);

        GUILayout.Label("Branches Per Row");
        int bpR = (int)EditorGUILayout.Slider(BranchGroupCount, 0, 10);

        if (EditorGUI.EndChangeCheck())
        {
            BranchCount = bCount;
            BranchGroupCount = bpR;
            BranchWidth = bWidth;
            BranchWidthMul = bWidthMul;
            //_WidthDecrease = widthDecrease;
            //BranchLength = startLength;
            _leafCount = leafCount;
            //  _LeafCount = leafCount;


        }

        EditorGUILayout.Space();

        GUILayout.Label("Trunk Settings", GUIInfo.TitleSmall);
        GUILayout.Label("Trunk Length");

        EditorGUI.BeginChangeCheck();

        float trunkLength = EditorGUILayout.Slider(TrunkLength, 0f, 6f);

        GUILayout.Label("Trunk Bottom Width");

        float trunkWidthB = EditorGUILayout.Slider(TrunkWidthBottom, 0.1f, 3f);

        GUILayout.Label("Trunk Top Width");

        float trunkWidthMulT = EditorGUILayout.Slider(TrunkWidthTop, 0.1f, 1f);

        // TRUNK
        if (EditorGUI.EndChangeCheck())
        {
            TrunkWidthBottom = trunkWidthB;
            TrunkWidthTop = trunkWidthMulT;

            TrunkLength = trunkLength;

           // Generate();
            // UpdateBranchData();

        }


        GUILayout.EndVertical();

        _leaves.InspectorGUI();
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
                Quaternion newRotation = Handles.RotationHandle(_rotation, _position).normalized;
                EditTransformGUI(ref _rotation, newRotation);
                break;

            case Tool.Scale:
                Vector3 newScale = Handles.ScaleHandle(_scale, _position, _rotation);
                EditTransformGUI(ref _scale, newScale);
                break;
        }

    }

    private void EditTransformGUI<T>(ref T oldValue, T newValue)
    {
        if (EditorGUI.EndChangeCheck())
        {
            oldValue = newValue;
            Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
            Generate();
        }
    }

    public static BranchInformation SetBranchInfo(float l, float angle, float w0, float w2, float3 pos, float3 ePos)
    {
        BranchInformation b = new();
        b.l = l;
        b.angle = angle;
        b.w0 = w0;
        b.w2 = w2;
        b.pos = pos;
        b.ePos = ePos;

        return b;
    }
}
