#if ODIN_INSPECTOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace XNodeEditor
{
    public static class OdinGenericMenuExtensions
    {
        public static void DisplayAsSelector( this GenericMenu gm, Rect position )
        {
            var menuItemsFI = typeof( GenericMenu ).GetField( "menuItems", BindingFlags.Instance | BindingFlags.NonPublic );

            var selector = new GenericSelector<MenuItemProxy>( string.Empty, false,
                item => item.content.text,
                ( (ArrayList)menuItemsFI.GetValue( gm ) ).ToArray().Select( x => new MenuItemProxy( x ) ).Where( x => x.func != null || x.func2 != null || string.IsNullOrEmpty( x.content.text ) )
            );
            var first = selector.SelectionTree.MenuItems.OrderByDescending( x => x.Name.Length ).FirstOrDefault();
            if ( first != null )
                position.size = new Vector2( Mathf.Max( position.size.x,16+ first.Name.Length * 8 ), position.size.y );

            Action<IEnumerable<MenuItemProxy>> onSelect = ( IEnumerable<MenuItemProxy> objs ) =>
            {
                var i = objs.FirstOrDefault();
                if ( i != null )
                {
                    if ( i.func != null || i.func2 != null )
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if ( i.func2 != null )
                                i.func2( i.userData );
                            else if ( i.func != null )
                                i.func();
                        };

                        selector.SelectionTree.Selection.ConfirmSelection();
                    }
                }
            };
            //selector.SelectionConfirmed += onSelect;
            selector.SelectionChanged += onSelect;
            //selector.EnableSingleClickToSelect();

            selector.SelectionTree.Config = new OdinMenuTreeDrawingConfig()
            {
                DrawSearchToolbar= selector.SelectionTree.MenuItems.Count > 8
            };
            selector.ShowInPopup( position );
        }
    }

    public class MenuItemProxy
    {
        public GUIContent content;
        public bool separator;
        public bool on;
        public GenericMenu.MenuFunction func;
        public GenericMenu.MenuFunction2 func2;
        public object userData;

        public MenuItemProxy( object o )
        {
            var type = o.GetType();
            content = (GUIContent)type.GetField( "content" ).GetValue( o );
            separator = (bool)type.GetField( "separator" ).GetValue( o );
            on = (bool)type.GetField( "on" ).GetValue( o );
            func = (GenericMenu.MenuFunction)type.GetField( "func" ).GetValue( o );
            func2 = (GenericMenu.MenuFunction2)type.GetField( "func2" ).GetValue( o );
            userData = type.GetField( "userData" ).GetValue( o );
        }

        public override string ToString()
        {
            return content.text;
        }
    }
}
#endif