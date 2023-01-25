using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class GUIInfo
{
    public static GUIStyle TitleSmall = new GUIStyle();

    public static void Init()
    {
        TitleSmall.fontSize = 15;
        TitleSmall.fontStyle = FontStyle.Bold;
        TitleSmall.normal.textColor = Color.white;
    }
}

public class GUITools
{
    public static Texture2D CreateBackground(Color color)
    {
        Texture2D newBackground = new Texture2D(1, 1);

        newBackground.SetPixel(0, 0, color);
        newBackground.Apply();

        return newBackground;

    }
}

public class ShapeGroup
{
    public ShapeGroup(ShapeInfo sInfo)
    {
        Shapes.Add(sInfo);
    }

    public List<ShapeInfo> Shapes = new();

    public float3x3[] LeafData = new float3x3[0];

    public string Name = " New Shape Group ";

    private int _selectedShape = 0;


    public Vector3 _position = Vector3.zero;
    private Quaternion _rotation = Quaternion.identity;
    private float _scale = 0;
    

    public void UpdateLeafDataSize()
    {
        int leafCount = 0;

        foreach (ShapeInfo tree in Shapes)
        {
            if (tree as Tree == null) continue;
            leafCount += (int)tree.Data[7].x;
        }

        LeafData = leafCount > 0 ? new float3x3[leafCount] : new float3x3[1];
    }


    public void GUI()
    {
        if (_selectedShape < 0) return;



        EditorGUI.BeginChangeCheck();

        if (Shapes.Count > 1)
        {
            switch (Tools.current)
            {
                case Tool.Move:
                    Vector3 newPosition = Handles.PositionHandle(_position, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach(ShapeInfo shape in Shapes)
                        {
                            shape._position += newPosition - _position;
                            shape.Transform = Matrix4x4.TRS(shape._position, shape._rotation, Vector3.one).inverse;

                            if (shape.ShapeType == 5)
                            {

                                LeafObject obj = (LeafObject)shape as LeafObject;
                                obj.RandomizeSphere(obj._Radius);
                            }
                        }

                        _position = newPosition;
                    }
                    break;

                case Tool.Rotate:
                    Quaternion newRotation = Handles.RotationHandle(_rotation, _position);

                    if (EditorGUI.EndChangeCheck())
                    {
                        //   Quaternion.differ
                        Quaternion diff = Diff(newRotation, _rotation);
                        foreach (ShapeInfo shape in Shapes)
                        {
                            shape._position = diff.normalized * (shape._position - _position) + _position;
                            shape._rotation = (diff * shape._rotation).normalized;

                            shape.Transform = Matrix4x4.TRS(shape._position, shape._rotation, Vector3.one).inverse;

                            if (shape.ShapeType == 5)
                            {

                                LeafObject obj = (LeafObject)shape as LeafObject;
                                obj.RandomizeSphere(obj._Radius);
                            }
                        }

                        _rotation = newRotation;
                    }
                    break;

                case Tool.Scale:
                    Vector3 newScale = Handles.ScaleHandle(new Vector3(_scale, _scale, _scale), _position, Quaternion.identity);
                 
                    break;
            }

            return;
        }


        //  if (Shapes.Count > 1) return;
        Shapes[_selectedShape].GUI();
    }


    public Quaternion Diff(Quaternion to, Quaternion from)
    {
        return to * Quaternion.Inverse(from);
    }

    Vector2 _scrollPosition;

    public void InspectorGUI()
    {
        Texture2D _selectedBackround = GUITools.CreateBackground(new Color(68f / 255f, 100f / 255f, 124f / 255f));

        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Shapes", GUIInfo.TitleSmall);

        _scrollPosition = GUILayout.BeginScrollView(
        _scrollPosition, GUILayout.Height(50));
        
        if (SDF.ShapeGroups.Count > 0)
        {
            GUIStyle btn = new GUIStyle(UnityEngine.GUI.skin.button);

            for (int i = 0; i < Shapes.Count; i++)
            {
                btn.normal.background = (_selectedShape == i) ? _selectedBackround : null;


                if (GUILayout.Button(Shapes[i].Name, btn))
                {
                    _selectedShape = (_selectedShape == i) ? -1 : i;
                }
            }
        }



        EditorGUILayout.EndScrollView();

        if (Shapes.Count > 1)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Up"))
            {
                if (_selectedShape > 0)
                {
                    ShapeInfo tempShape = Shapes[_selectedShape];
                    Shapes[_selectedShape] = Shapes[_selectedShape - 1];
                    Shapes[_selectedShape - 1] = tempShape;
                    _selectedShape = _selectedShape - 1;
                }
            }

            if (GUILayout.Button("Move Down"))
            {
                if (_selectedShape >= Shapes.Count - 1) return;

                ShapeInfo tempShape = Shapes[_selectedShape];
                Shapes[_selectedShape] = Shapes[_selectedShape + 1];
                Shapes[_selectedShape + 1] = tempShape;
                _selectedShape = _selectedShape + 1;

            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Un Merge"))
            {
                Shapes[_selectedShape].SGTransform = float4x4.identity;
                SDF.ShapeGroups.Add(new ShapeGroup(Shapes[_selectedShape]));
                Shapes.RemoveAt(_selectedShape);
                _selectedShape = _selectedShape - 1 >= 0 ? _selectedShape - 1 : _selectedShape;
            }
        }

        EditorGUILayout.EndVertical();

        // Option to Change Shape

        EditorGUILayout.BeginVertical();
        if (Shapes.Count > 0 && _selectedShape >= 0)
        {
            EditorGUI.BeginChangeCheck();

            ShapeType newShape = (ShapeType)EditorGUILayout.EnumPopup("Shape Type", (ShapeType)Shapes[_selectedShape].ShapeType);

            if (EditorGUI.EndChangeCheck())
            {

                Shapes[_selectedShape] = ChangeShape(newShape, Shapes[_selectedShape]);
            }
            
        }
        EditorGUILayout.EndVertical();


        if (_selectedShape < 0) return;
        Shapes[_selectedShape].InspectorGUI();
    }



    private ShapeInfo ChangeShape(ShapeType newType, ShapeInfo shape)
    {
        switch (newType)
        {
            case ShapeType.Tree:
                return new Tree(shape._position, shape._rotation, shape._scale);

            case ShapeType.Box:
                return new Box(shape._position, shape._rotation, shape._scale);

            case ShapeType.Sphere:
                return new Sphere(shape._position, shape._rotation, shape._scale);

            case ShapeType.Plane:
                return new Plane(shape._position, shape._rotation, shape._scale);

            case ShapeType.Leaves:
                return new LeafObject(shape._position, shape._rotation, shape._scale);

            case ShapeType.RoundedBox:
                return new RoundedBox(shape._position, shape._rotation, shape._scale);

            case ShapeType.CappedCone:
                return new CappedCone(shape._position, shape._rotation, shape._scale);

            case ShapeType.PineTree:
                return new Pine(shape._position, shape._rotation, shape._scale);

            case ShapeType.SimpleTree:
                return new LPTree(shape._position, shape._rotation, shape._scale);
        }

        return new Sphere();
        // SDF.ShapeData[ListIndex] = _shapeInfo.SDFIndex;
    }

}

public class SDF
{
   // public static List<ShapeInfo> Shapes = new();
    public static List<ShapeGroup> ShapeGroups = new();

    // Holds information about what kind of shape it is
    public static List<int> ShapeData = new();

    //// Holds all information of the shapes (aka booleans, size, etc, etc)
    public static List<float3> ShapeDataList = new();

    //// Holds information about the transforms of the shapes (Transform, scale, rotate)
    public static List<float4x4> TransformData = new();

    public static List<float4x4> MergedTransformData = new();

    public static List<BranchInformation> BranchInfo = new();

    public static List<float3> BezierPoints = new() { float3.zero };
//  public static int Effect = 0;
    public static int OutlineEffect = 0;
    public static int MosaicEffect = 0;
    public static int CelShadingEffect = 0;
    public static int VoxelEffect = 0;


    public static SortedDictionary<int, ShapeInfo> CombinedShapes = new();

    public static List<BranchInformation> GetBranchInfo()
    {
        List<BranchInformation> branches = new();

        for (int i = 0; i < ShapeGroups.Count; i++)
        {
            for (int j = 0; j < ShapeGroups[i].Shapes.Count; j++)
            {
                if (ShapeGroups[i].Shapes[j].GetType() == typeof(Tree))
                {
                    Tree info = (Tree)ShapeGroups[i].Shapes[j];
                    branches.AddRange(info._branches);
                }
                else if (ShapeGroups[i].Shapes[j].GetType() == typeof(Pine))
                {
                    Pine info = (Pine)ShapeGroups[i].Shapes[j];
                    branches.AddRange(info._branches);
                  
                }
                else if (ShapeGroups[i].Shapes[j].GetType() == typeof(LPTree))
                {
                    LPTree info = (LPTree)ShapeGroups[i].Shapes[j];
                    branches.AddRange(info._branches);

                }
            }
        }

        return branches;

    }

    public static void SetLightDataPlanes(Vector3 lightInfo)
    {
        for (int i = 0; i < ShapeGroups.Count; i++)
        {
            for (int j = 0; j < ShapeGroups[i].Shapes.Count; j++)
            {
                if (ShapeGroups[i].Shapes[j].GetType() == typeof(Plane))
                {
                    Plane currPlane = (Plane)ShapeGroups[i].Shapes[j];
                    currPlane.LightDir = lightInfo;
                }
            }
        }
    }
    // Compute Buffer data set
    public static int[] GetShapeList()
    {
        // return Shapes.Select(o => o.Type).ToArray();
        List<int> Shapes = new();
        for (int i = 0; i < ShapeGroups.Count; i++)
        {
           Shapes.AddRange(ShapeGroups[i].Shapes.Select(o => o.ShapeType).ToArray());
        }

        return Shapes.ToArray();
    }

    public static float4x4[] GetTransformList()
    {
        List<float4x4> Transforms = new();
        for (int i = 0; i < ShapeGroups.Count; i++)
        {
            Transforms.AddRange(ShapeGroups[i].Shapes.Select(o => o.Transform).ToArray());
        }

        return Transforms.ToArray();
    }

    public static float4x4[] GetShaderGroupTransforms()
    {

        List<float4x4> MergedTransformData = new();
        for (int i = 0; i < ShapeGroups.Count; i++)
        {
            MergedTransformData.AddRange(ShapeGroups[i].Shapes.Select(o => o.SGTransform).ToArray());
        }

        return MergedTransformData.ToArray();
        
    }

    public static float3[] GetShapeDataList()
    {
        List<float3> outputList = new();

        foreach(ShapeGroup group in ShapeGroups)
        {
            for (int k = 0; k < group.Shapes.Count; k++)
            {
                for (int j = 0; j < group.Shapes[k].Data.Length; j++)
                {
                    outputList.Add(group.Shapes[k].Data[j]);
                }
            }
        }

         

        return outputList.ToArray();
    }


    // Manipulate data list
    public static void Add(int index, ShapeGroup newShapeGroup)
    {
       // Shapes.Insert(index, newShape);
        ShapeGroups.Insert(index, newShapeGroup);
    }

    public static void Copy(int index)
    {

    }

    public static int Delete(int index)
    {
        //if (Shapes.Count < 2) return index;

        ////Get shapetype and length
        //Shapes.RemoveAt(index);

        //return index > Shapes.Count - 1 ? Shapes.Count - 1 : index;

        if(ShapeGroups.Count < 2) return index;

        //Get shapetype and length
        ShapeGroups.RemoveAt(index);

        return index > ShapeGroups.Count - 1 ? ShapeGroups.Count - 1 : index;
    }

    public static void MoveUp(int index)
    {
        ShapeGroup tempShape = ShapeGroups[index];
        ShapeGroups[index] = ShapeGroups[index - 1];
        ShapeGroups[index - 1] = tempShape;
    }

    public static void MoveDown(int index)
    {
        // Return if index is last object
        if (index >= ShapeGroups.Count - 1) return;

        ShapeGroup tempShape = ShapeGroups[index];
        ShapeGroups[index] = ShapeGroups[index + 1];
        ShapeGroups[index + 1] = tempShape;

        //Add Undo
    }

    public static int MergeUp(int index)
    {
        if (index <= 0) return index;

        ShapeGroups[index - 1].Shapes.AddRange(ShapeGroups[index].Shapes);
        ShapeGroups.RemoveAt(index);

        return index - 1;

        //Add Undo
    }

    public static int MergeDown(int index)
    {
        if (index >= ShapeGroups.Count - 1) return index;

        ShapeGroups[index].Shapes.AddRange(ShapeGroups[index + 1].Shapes);
        ShapeGroups.RemoveAt(index + 1);

        return index;
        //for (int i = 0; i < 2; i++)
        //{
        //  //  CombinedShapes.Add(0, index + i);
        //    Shapes[index + i].CombinedGroup = 0;
        //}

        //CombinedShapes.Add(0, index + 1);
        //Shapes[index + 1].CombinedGroup = 0;
    }

    public static void Duplicate(int index)
    {
     //   ShapeInfo duplicate = Shapes[index].Clone();

     //   duplicate.Name = "Yes";
     ////   Debug.Log(duplicate.Data[);
     //   Shapes.Insert(index, duplicate);
    }

    public static void GUICombined()
    {
        //switch (Tools.current)
        //{
        //    case Tool.Move:
        //        Vector3 newPosition = Handles.PositionHandle(_position, Quaternion.identity);
        //        EditTransformGUI(ref _position, newPosition);
        //        break;

        //    case Tool.Rotate:
        //        Quaternion newRotation = Handles.RotationHandle(_rotation, _position);
        //        EditTransformGUI(ref _rotation, newRotation);
        //        break;

        //    case Tool.Scale:
        //        Vector3 newScale = Handles.ScaleHandle(_scale, _position, _rotation);
        //        EditTransformGUI(ref _scale, newScale);
        //        break;
        //}
    }

}