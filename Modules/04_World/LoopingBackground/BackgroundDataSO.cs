using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BackgroundDataSO", menuName = "Background/Background Data", order = 1)]
public class BackgroundDataSO : ScriptableObject
{
    public BackgroundVariant backgroundVariant;

    [Header("Layer Colors")]
    public Color _color_00_Sky;
    public Color _color_01_Siluette;
    public Color _color_02_Buildings;
    public Color _color_03_Windows;
    public Color _color_04_Road;
    public Color _color_05_Ground;
    public Color _color_06_GroundLight;
}
