using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

enum Effects
{ 
    Voxel = 1,
    Outline = 2,
    cel = 0,
    Mosaic = 3
}


public class Effect
{
    Effects currentEffect;


    const string _active = "Deactivate";
    const string _non_active = "Activate";

    private string[] _EffectsActive = new[]{ _non_active, _non_active, _non_active, _non_active };

    public void InspectorGUI()
    {
        EditorGUILayout.BeginVertical("box");


        EditorGUI.BeginChangeCheck();

        Effects newShape = (Effects)EditorGUILayout.EnumPopup("Effects", currentEffect);

        if (EditorGUI.EndChangeCheck())
        {
            currentEffect = newShape;
            //SDF.Effect = (int)newShape;
        }

        if (GUILayout.Button(_EffectsActive[(int)newShape]))
        {
            switch(currentEffect)
            {
                case Effects.cel:
                    SDF.CelShadingEffect = SDF.CelShadingEffect == 0 ? 1 : 0;
                    break;

                case Effects.Mosaic:
                    SDF.MosaicEffect = SDF.MosaicEffect == 0 ? 1 : 0;
                    break;

                case Effects.Outline:
                    SDF.OutlineEffect = SDF.OutlineEffect == 0 ? 1 : 0;
                    break;

                case Effects.Voxel:
                    SDF.VoxelEffect = SDF.VoxelEffect == 0 ? 1 : 0;
                    break;

            }

            _EffectsActive[(int)newShape] = _EffectsActive[(int)newShape] == _active ? _non_active : _active; 
        }

        EditorGUILayout.EndVertical();


    }
}
