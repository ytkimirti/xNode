using System;
using UnityEngine;

/// <summary> Draw enums correctly within nodes. Without it, enums show up at the wrong positions. </summary>
/// <remarks> Enums with this attribute are not detected by EditorGui.ChangeCheck due to waiting before executing </remarks>
[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum )]
public class DefaultNoodleColorAttribute : Attribute
{
    public Color Color { get; private set; }

    public DefaultNoodleColorAttribute( float colorR, float colorG, float colorB )
    {
        Color = new Color( colorR, colorG, colorB );
    }

    public DefaultNoodleColorAttribute( byte colorR, byte colorG, byte colorB )
    {
        Color = new Color32( colorR, colorG, colorB, 255 );
    }
}
