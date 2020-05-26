using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode;
using static XNode.Node;

namespace XNodeEditor
{
    [DrawerPriority( 100, 0, 0 )]
    public class InNodeEdtiorFieldDrawer<T> : OdinValueDrawer<T>
    {
        protected override bool CanDrawValueProperty( InspectorProperty property )
        {
            if ( !NodeEditor.inNodeEditor )
                return false;

            return property.GetAttribute<InputAttribute>() == null && property.GetAttribute<OutputAttribute>() == null && property.GetAttribute<DontFoldAttribute>() == null;
        }

        protected bool drawData = true;

        protected override void DrawPropertyLayout( GUIContent label )
        {
            var node = Property.Tree.WeakTargets[0] as Node;
            if ( node == null )
            {
                SirenixEditorGUI.ErrorMessageBox( "Not a property of a Node" );
                CallNextDrawer( label );

                return;
            }

            if ( Event.current.type == EventType.Layout )
                drawData = !node.folded;

            if ( drawData )
                CallNextDrawer( label );
        }
    }
}