﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class GuiHelper
    {

        public static void ConfigTextureProperty(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, bool hasFoldoutProperties, bool skip_drag_and_drop_handling = false)
        {
            switch (Config.Singleton.default_texture_type)
            {
                case TextureDisplayType.small:
                    SmallTextureProperty(position, prop, label, editor, hasFoldoutProperties);
                    break;
                case TextureDisplayType.big:
                    if (DrawingData.CurrentTextureProperty.reference_properties_exist || DrawingData.CurrentTextureProperty.reference_property_exists)
                        StylizedBigTextureProperty(position, prop, label, editor, hasFoldoutProperties);
                    else
                        BigTextureProperty(position, prop, label, editor, DrawingData.CurrentTextureProperty.hasScaleOffset);
                    break;

                case TextureDisplayType.stylized_big:
                    StylizedBigTextureProperty(position, prop, label, editor, hasFoldoutProperties, skip_drag_and_drop_handling);
                    break;
            }
        }

        public static void SmallTextureProperty(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, bool hasFoldoutProperties, Action extraFoldoutGUI = null)
        {
            Rect thumbnailPos = position;
            Rect foloutClickCheck = position;
            Rect tooltipRect = position;
            thumbnailPos.x += hasFoldoutProperties ? 20 : 0;
            editor.TexturePropertyMiniThumbnail(thumbnailPos, prop, label.text, label.tooltip);
            if (DrawingData.CurrentTextureProperty.reference_property_exists)
            {
                ShaderProperty property = ShaderEditor.Active.PropertyDictionary[DrawingData.CurrentTextureProperty.options.reference_property];
                Rect r = position;
                r.x += EditorGUIUtility.labelWidth - CurrentIndentWidth();
                r.width -= EditorGUIUtility.labelWidth - CurrentIndentWidth();
                foloutClickCheck.width -= r.width;
                property.Draw(new CRect(r), new GUIContent());
                property.tooltip.ConditionalDraw(r);
            }
            if (hasFoldoutProperties && DrawingData.CurrentTextureProperty != null)
            {
                //draw dropdown triangle
                thumbnailPos.x += DrawingData.CurrentTextureProperty.xOffset * 15;
                //This is an invisible button with zero functionality. But it needs to be here so that the triangle click reacts fast
                if (GUI.Button(thumbnailPos, "", GUIStyle.none)) { }
                if (Event.current.type == EventType.Repaint)
                    EditorStyles.foldout.Draw(thumbnailPos, false, false, DrawingData.CurrentTextureProperty.showFoldoutProperties, false);

                if (DrawingData.IsEnabled)
                {
                    //sub properties
                    if (DrawingData.CurrentTextureProperty.showFoldoutProperties)
                    {
                        EditorGUI.indentLevel += 2;
                        extraFoldoutGUI?.Invoke();
                        if (DrawingData.CurrentTextureProperty.hasScaleOffset)
                        {
                            ShaderEditor.Active.Editor.TextureScaleOffsetProperty(prop);
                            tooltipRect.height += GUILayoutUtility.GetLastRect().height;
                        }
                        //In case of locked material end disabled group here to allow editing of sub properties
                        if (ShaderEditor.Active.IsLockedMaterial) EditorGUI.EndDisabledGroup();

                        PropertyOptions options = DrawingData.CurrentTextureProperty.options;
                        if (options.reference_properties != null)
                            foreach (string r_property in options.reference_properties)
                            {
                                ShaderProperty property = ShaderEditor.Active.PropertyDictionary[r_property];
                                property.Draw(useEditorIndent: true);
                            }

                        //readd disabled group
                        if (ShaderEditor.Active.IsLockedMaterial) EditorGUI.BeginDisabledGroup(false);

                        EditorGUI.indentLevel -= 2;
                    }
                    if (ShaderEditor.Input.LeftClick_IgnoreLockedAndUnityUses && foloutClickCheck.Contains(Event.current.mousePosition))
                    {
                        ShaderEditor.Input.Use();
                        DrawingData.CurrentTextureProperty.showFoldoutProperties = !DrawingData.CurrentTextureProperty.showFoldoutProperties;
                    }
                }
            }
            
            Rect object_rect = new Rect(position);
            object_rect.height = GUILayoutUtility.GetLastRect().y - object_rect.y + GUILayoutUtility.GetLastRect().height;
            DrawingData.LastGuiObjectRect = object_rect;
            DrawingData.TooltipCheckRect = tooltipRect;
        }

        public static void BigTextureProperty(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, bool scaleOffset)
        {
            Rect rect = GUILayoutUtility.GetRect(label, Styles.bigTextureStyle);
            float defaultLabelWidth = EditorGUIUtility.labelWidth;
            float defaultFieldWidth = EditorGUIUtility.fieldWidth;
            editor.SetDefaultGUIWidths();
            editor.TextureProperty(position, prop, label.text, label.tooltip, scaleOffset);
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            EditorGUIUtility.fieldWidth = defaultFieldWidth;
            Rect object_rect = new Rect(position);
            object_rect.height += rect.height;
            DrawingData.LastGuiObjectRect = object_rect;
            DrawingData.TooltipCheckRect = object_rect;
        }

        static int s_texturePickerWindow = -1;
        static MaterialProperty s_texturePickerWindowProperty = null;
        public static void StylizedBigTextureProperty(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, bool hasFoldoutProperties, bool skip_drag_and_drop_handling = false)
        {
            position.x += (EditorGUI.indentLevel) * 15;
            position.width -= (EditorGUI.indentLevel) * 15;
            Rect rect = GUILayoutUtility.GetRect(label, Styles.bigTextureStyle);
            rect.x += (EditorGUI.indentLevel) * 15;
            rect.width -= (EditorGUI.indentLevel) * 15;
            Rect border = new Rect(rect);
            border.position = new Vector2(border.x, border.y - position.height);
            border.height += position.height;

            if (DrawingData.CurrentTextureProperty.reference_properties_exist)
            {
                border.height += 8;
                foreach (string r_property in DrawingData.CurrentTextureProperty.options.reference_properties)
                {
                    border.height += editor.GetPropertyHeight(ShaderEditor.Active.PropertyDictionary[r_property].materialProperty);
                }
            }
            if (DrawingData.CurrentTextureProperty.reference_property_exists)
            {
                border.height += 8;
                border.height += editor.GetPropertyHeight(ShaderEditor.Active.PropertyDictionary[DrawingData.CurrentTextureProperty.options.reference_property].materialProperty);
            }


            //background
            //GUI.DrawTexture(border, Styles.rounded_texture, ScaleMode.StretchToFill, true, 0, Styles.COLOR_BACKGROUND_1, 0, 0);

            Color prevC = GUI.color;
            GUI.color = Styles.COLOR_BACKGROUND_1;

            GUI.DrawTexture(border, Styles.rounded_texture, ScaleMode.StretchToFill, true);
            Rect quad = new Rect(border);
            quad.width = quad.height / 2;
            GUI.DrawTextureWithTexCoords(quad, Styles.rounded_texture, new Rect(0, 0, 0.5f, 1), true);
            quad.x += border.width - quad.width;
            GUI.DrawTextureWithTexCoords(quad, Styles.rounded_texture, new Rect(0.5f, 0, 0.5f, 1), true);

            GUI.color = prevC;

            quad.width = border.height - 4;
            quad.height = quad.width;
            quad.x = border.x + border.width - quad.width - 1;
            quad.y += 2;

            Rect preview_rect_border = new Rect(position);
            preview_rect_border.height = rect.height + position.height - 6;
            preview_rect_border.width = preview_rect_border.height;
            preview_rect_border.y += 3;
            preview_rect_border.x += position.width - preview_rect_border.width - 3;
            Rect preview_rect = new Rect(preview_rect_border);
            preview_rect.height -= 6;
            preview_rect.width -= 6;
            preview_rect.x += 3;
            preview_rect.y += 3;
            if (prop.hasMixedValue)
            {
                Rect mixedRect = new Rect(preview_rect);
                mixedRect.y -= 5;
                mixedRect.x += mixedRect.width / 2 - 4;
                GUI.Label(mixedRect, "_");
            }
            else if (prop.textureValue != null)
            {
                GUI.DrawTexture(preview_rect, prop.textureValue);
            }
            GUI.DrawTexture(preview_rect_border, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.grey, 3, 5);

            //selection button and pinging
            Rect select_rect = new Rect(preview_rect);
            select_rect.height = 12;
            select_rect.y += preview_rect.height - 12;
            if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == s_texturePickerWindow && s_texturePickerWindowProperty.name == prop.name)
            {
                prop.textureValue = (Texture)EditorGUIUtility.GetObjectPickerObject();
                ShaderEditor.RepaintActive();
            }
            if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == s_texturePickerWindow)
            {
                s_texturePickerWindow = -1;
                s_texturePickerWindowProperty = null;
            }
            if (GUI.Button(select_rect, "Select", EditorStyles.miniButton))
            {
                EditorGUIUtility.ShowObjectPicker<Texture>(prop.textureValue, false, "", 0);
                s_texturePickerWindow = EditorGUIUtility.GetObjectPickerControlID();
                s_texturePickerWindowProperty = prop;
            }
            else if (Event.current.type == EventType.MouseDown && preview_rect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.PingObject(prop.textureValue);
            }

            if (!skip_drag_and_drop_handling)
                if ((ShaderEditor.Input.is_drag_drop_event) && preview_rect.Contains(ShaderEditor.Input.mouse_position) && DragAndDrop.objectReferences[0] is Texture)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (ShaderEditor.Input.is_drop_event)
                    {
                        DragAndDrop.AcceptDrag();
                        prop.textureValue = (Texture)DragAndDrop.objectReferences[0];
                    }
                }

            //scale offset rect

            if (hasFoldoutProperties || DrawingData.CurrentTextureProperty.options.reference_property != null)
            {
                EditorGUI.indentLevel += 2;

                if (DrawingData.CurrentTextureProperty.hasScaleOffset)
                {
                    Rect scale_offset_rect = new Rect(position);
                    scale_offset_rect.y += 37;
                    scale_offset_rect.width -= 2 + preview_rect.width + 10 + 30;
                    scale_offset_rect.x += 30;
                    editor.TextureScaleOffsetProperty(scale_offset_rect, prop);
                }

                //In case of locked material end disabled group here to allow editing of sub properties
                if (ShaderEditor.Active.IsLockedMaterial) EditorGUI.EndDisabledGroup();

                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 128;

                PropertyOptions options = DrawingData.CurrentTextureProperty.options;
                if (options.reference_property != null)
                {
                    ShaderProperty property = ShaderEditor.Active.PropertyDictionary[options.reference_property];
                    property.Draw(useEditorIndent: true);
                }
                if (options.reference_properties != null)
                    foreach (string r_property in options.reference_properties)
                    {
                        ShaderProperty property = ShaderEditor.Active.PropertyDictionary[r_property];
                        property.Draw(useEditorIndent: true);
                    }
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUI.indentLevel -= 2;

                //readd disabled group
                if (ShaderEditor.Active.IsLockedMaterial) EditorGUI.BeginDisabledGroup(false);
            }

            Rect label_rect = new Rect(position);
            label_rect.x += 2;
            label_rect.y += 2;
            GUI.Label(label_rect, label);

            GUILayoutUtility.GetRect(0, 5);
            
            DrawingData.LastGuiObjectRect = border;
            DrawingData.TooltipCheckRect = border;
        }

        static Stack<int> s_previousIndentLevels = new Stack<int>();
        public static void BeginCustomIndentLevel(int indent)
        {
            s_previousIndentLevels.Push(EditorGUI.indentLevel);
            EditorGUI.indentLevel = indent;
        }

        public static void EndCustomIndentLevel()
        {
            EditorGUI.indentLevel = s_previousIndentLevels.Pop();
        }

        static string[] s_fallbackShaderTypes = { "Standard", "Toon", "Unlit", "VertexLit", "Particle", "Sprite", "Matcap", "MobileToon" };
        static string[] s_fallbackRenderTypes = { "Opaque", "Cutout", "Transparent", "Fade" };
        static string[] s_fallbackRenderTypesValues = { "", "Cutout", "Transparent", "Fade" };
        static string[] s_fallbackCullTypes = { "OneSided", "DoubleSided" };
        static string[] s_fallbackCullTypesValues = { "", "DoubleSided" };
        static string[] s_fallbackNoTypes = { "None", "Hidden" };
        static string[] s_fallbackNoTypesValues = { "", "Hidden" };
        static string[] s_vRCFallbackOptionsPopup = s_fallbackNoTypes.Union(s_fallbackShaderTypes.SelectMany(s => s_fallbackRenderTypes.SelectMany(r => s_fallbackCullTypes.Select(c => r + "/" + c).Select(rc => s + "/" + rc)))).ToArray();
        static string[] s_vRCFallbackOptionsValues = s_fallbackNoTypes.Union(s_fallbackShaderTypes.SelectMany(s => s_fallbackRenderTypesValues.SelectMany(r => s_fallbackCullTypesValues.Select(c => r + c).Select(rc => s + rc)))).ToArray();
        public static void VRCFallbackSelector(ShaderEditor shaderEditor)
        {
            string current = shaderEditor.Materials[0].GetTag("VRCFallback", false, "None");
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("VRChat Fallback Shader", s_vRCFallbackOptionsValues.Select((f, i) => (f, i)).FirstOrDefault(f => f.f == current).i , s_vRCFallbackOptionsPopup);
            if (EditorGUI.EndChangeCheck())
                shaderEditor.Materials[0].SetOverrideTag("VRCFallback", s_vRCFallbackOptionsValues[selected]);
        }

        public static void MinMaxSlider(Rect settingsRect, GUIContent content, MaterialProperty prop)
        {
            bool changed = false;
            Vector4 vec = prop.vectorValue;
            Rect sliderRect = settingsRect;

            EditorGUI.LabelField(settingsRect, content);

            if (settingsRect.width > 160)
            {
                Rect numberRect = settingsRect;
                numberRect.width = 65 + (EditorGUI.indentLevel - 1) * 15;

                numberRect.x = EditorGUIUtility.labelWidth - (EditorGUI.indentLevel - 1) * 15;

                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = prop.hasMixedValue;
                vec.x = EditorGUI.FloatField(numberRect, vec.x, EditorStyles.textField);
                changed |= EditorGUI.EndChangeCheck();

                numberRect.x = settingsRect.xMax - numberRect.width;

                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = prop.hasMixedValue;
                vec.y = EditorGUI.FloatField(numberRect, vec.y);
                changed |= EditorGUI.EndChangeCheck();

                sliderRect.xMin = EditorGUIUtility.labelWidth - (EditorGUI.indentLevel - 1) * 15;
                sliderRect.xMin += (65 + -8);
                sliderRect.xMax -= (65 + -8);
            }

            vec.x = Mathf.Clamp(vec.x, vec.z, vec.y);
            vec.y = Mathf.Clamp(vec.y, vec.x, vec.w);

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(sliderRect, ref vec.x, ref vec.y, vec.z, vec.w);
            changed |= EditorGUI.EndChangeCheck();

            if (changed)
            {
                prop.vectorValue = vec;
            }
        }

        public static bool DrawListField<type>(List<type> list) where type : UnityEngine.Object
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add", EditorStyles.miniButton))
                list.Add(null);
            if (GUILayout.Button("Remove", EditorStyles.miniButton))
                if (list.Count > 0)
                    list.RemoveAt(list.Count - 1);
            GUILayout.EndHorizontal();

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = (type)EditorGUILayout.ObjectField(list[i], typeof(type), false);
            }
            return false;
        }
        public static bool DrawListField<type>(List<type> list, float maxHeight, ref Vector2 scrollPosition) where type : UnityEngine.Object
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add", EditorStyles.miniButton))
                list.Add(null);
            if (GUILayout.Button("Remove", EditorStyles.miniButton))
                if (list.Count > 0)
                    list.RemoveAt(list.Count - 1);
            GUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(maxHeight));
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = (type)EditorGUILayout.ObjectField(list[i], typeof(type), false);
            }
            GUILayout.EndScrollView();
            return false;
        }

        public static bool GUIDataStruct<t>(t data)
        {
            return GUIDataStruct<t>(data, new string[] { });
        }

        public static bool GUIDataStruct<t>(t data, string[] exclude)
        {
            Type type = data.GetType();
            bool changed = false;
            foreach (FieldInfo f in type.GetFields())
            {
                bool skip = false;
                foreach (string s in exclude)
                    if (s == f.Name)
                        skip = true;
                if (skip)
                    continue;

                if (f.FieldType.IsEnum)
                    changed |= GUIEnum(f, data);
                else if (f.FieldType == typeof(string))
                    changed |= GUIString(f, data);
                else if (f.FieldType == typeof(int))
                    changed |= GUIInt(f, data);
                else if (f.FieldType == typeof(float))
                    changed |= GUIFloat(f, data);
            }
            return changed;
        }

        private static bool GUIEnum(FieldInfo f, object o)
        {
            EditorGUI.BeginChangeCheck();
            Enum e = EditorGUILayout.EnumPopup(f.Name, (Enum)f.GetValue(o), GUILayout.ExpandWidth(false));
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
                f.SetValue(o, e);
            return changed;
        }

        private static bool GUIString(FieldInfo f, object o)
        {
            EditorGUI.BeginChangeCheck();
            string s = EditorGUILayout.TextField(f.Name, (string)f.GetValue(o), GUILayout.ExpandWidth(false));
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
                f.SetValue(o, s);
            return changed;
        }

        private static bool GUIInt(FieldInfo f, object o)
        {
            EditorGUI.BeginChangeCheck();
            int i = EditorGUILayout.IntField(f.Name, (int)f.GetValue(o), GUILayout.ExpandWidth(false));
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
                f.SetValue(o, i);
            return changed;
        }

        private static bool GUIFloat(FieldInfo f, object o)
        {
            EditorGUI.BeginChangeCheck();
            float i = EditorGUILayout.FloatField(f.Name, (float)f.GetValue(o), GUILayout.ExpandWidth(false));
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
                f.SetValue(o, i);
            return changed;
        }

        public static void DrawLocaleSelection(GUIContent label, string[] locales, int selected)
        {
            EditorGUI.BeginChangeCheck();
            selected = EditorGUILayout.Popup(label.text, selected, locales);
            if (EditorGUI.EndChangeCheck())
            {
                ShaderEditor.Active.PropertyDictionary[ShaderEditor.PROPERTY_NAME_LOCALE].materialProperty.floatValue = selected;
                ShaderEditor.ReloadActive();
            }
        }

        public static void DrawHeader(ref bool enabled, ref bool options, GUIContent name)
        {
            var r = EditorGUILayout.BeginHorizontal("box");
            enabled = EditorGUILayout.Toggle(enabled, EditorStyles.radioButton, GUILayout.MaxWidth(15.0f));
            options = GUI.Toggle(r, options, GUIContent.none, new GUIStyle());
            EditorGUILayout.LabelField(name, Styles.dropDownHeaderLabel);
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawMasterLabel(string shaderName, Rect parent)
        {
            Rect rect = new Rect(0, parent.y, parent.width, 18);
            EditorGUI.LabelField(rect, "<size=16>" + shaderName + "</size>", Styles.masterLabel);
        }

        public static float CurrentIndentWidth()
        {
            return EditorGUI.indentLevel * 15;
        }
        // Mimics the normal map import warning - written by Orels1
        static bool TextureImportWarningBox(string message){
            GUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
            EditorGUILayout.LabelField(message, new GUIStyle(EditorStyles.label) {
                fontSize = 9, wordWrap = true
            });
            EditorGUILayout.BeginHorizontal(new GUIStyle() {
                alignment = TextAnchor.MiddleRight
            }, GUILayout.Height(24));
            EditorGUILayout.Space();
            bool buttonPress = GUILayout.Button("Fix Now", new GUIStyle("button") {
                stretchWidth = false,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(9, 9, 0, 0)
            }, GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return buttonPress;
        }
        public static void sRGBWarning(MaterialProperty tex){
            if (tex.textureValue){
                string sRGBWarning = "This texture is marked as sRGB, but should not contain color information.";
                string texPath = AssetDatabase.GetAssetPath(tex.textureValue);
                TextureImporter texImporter;
                var importer = TextureImporter.GetAtPath(texPath) as TextureImporter;
                if (importer != null){
                    texImporter = (TextureImporter)importer;
                    if (texImporter.sRGBTexture){
                        if (TextureImportWarningBox(sRGBWarning)){
                            texImporter.sRGBTexture = false;
                            texImporter.SaveAndReimport();
                        }
                    }
                }
            }
        }

        public static bool Button(Rect r, GUIStyle style)
        {
            return GUI.Button(r, GUIContent.none, style);
        }

        public static bool Button(GUIStyle style, int width, int height)
        {
            Rect r = GUILayoutUtility.GetRect(width, height);
            return Button(r, style);
        }
        
        public static bool ButtonWithCursor(GUIStyle style, int width, int height)
        {
            Rect r = GUILayoutUtility.GetRect(width, height);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
            return Button(r, style);
        }

        public static bool Button(GUIStyle style, int width, int height, Color c)
        {
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = c;
            bool b = GuiHelper.Button(style, width, height);
            GUI.backgroundColor = prevColor;
            return b;
        }

        public static bool Button(Rect r, GUIStyle style, Color c, bool doColor)
        {
            Color prevColor = GUI.backgroundColor;
            if(doColor) GUI.backgroundColor = c;
            bool b = GuiHelper.Button(r, style);
            GUI.backgroundColor = prevColor;
            return b;
        }
    }

    public class BetterTooltips
    {
        private static Tooltip activeTooltip;

        public class Tooltip
        {
            private GUIContent content;
            private bool empty;

            public bool isSelected { get; private set; } = false;

            private Rect containerRect;
            private Rect contentRect;

            readonly static Vector2 PADDING = new Vector2(10, 10);

            public Tooltip(string text)
            {
                content = new GUIContent(text);
                empty = string.IsNullOrWhiteSpace(text);
            }

            public Tooltip(string text, Texture texture)
            {
                content = new GUIContent(text, texture);
                empty = string.IsNullOrWhiteSpace(text) && texture == null;
            }

            public void SetText(string text)
            {
                content.text = text;
                empty &= string.IsNullOrWhiteSpace(text);
            }

            public void ConditionalDraw(Rect hoverOverRect)
            {
                if (empty) return;
                bool isSelected = hoverOverRect.Contains(Event.current.mousePosition);
                if (isSelected )
                {
                    CalculatePositions(hoverOverRect);
                    activeTooltip = this;
                    this.isSelected = true;
                }
            }

            private void CalculatePositions(Rect hoverOverRect)
            {
                Vector2 contentSize = EditorStyles.label.CalcSize(content);
                Vector2 containerPosition = new Vector2(Event.current.mousePosition.x - contentSize.x / 2 - PADDING.x / 2, hoverOverRect.y - contentSize.y - PADDING.y - 3);

                containerPosition.x = Mathf.Max(0, containerPosition.x);
                containerPosition.x = Mathf.Min(EditorGUIUtility.currentViewWidth - contentSize.x - PADDING.x, containerPosition.x);

                contentRect = new Rect(containerPosition + new Vector2(PADDING.x/2, PADDING.y/2), contentSize);
                containerRect = new Rect(containerPosition, contentSize + new Vector2(PADDING.x, PADDING.y));
            }

            public void Draw()
            {
                EditorGUI.DrawRect(containerRect, Styles.COLOR_BG);
                EditorGUI.LabelField(contentRect, content);
                isSelected = false;
            }
        }

        public static void DrawActive()
        {
            if(activeTooltip != null)
            {
                if (activeTooltip.isSelected)
                {
                    activeTooltip.Draw();
                }
                else
                {
                    activeTooltip = null;
                }
            }
        }
    }

    public class FooterButton
    {
        private GUIContent content;
        private bool isTextureContent;
        const int texture_height = 40;
        int texture_width;
        private ButtonData data;

        public FooterButton(ButtonData data)
        {
            this.data = data;
            if (data != null)
            {
                if (data.texture == null)
                {
                    content = new GUIContent(data.text, data.hover);
                    isTextureContent = false;
                }
                else
                {
                    texture_width = (int)((float)data.texture.loaded_texture.width / data.texture.loaded_texture.height * texture_height);
                    content = new GUIContent(data.texture.loaded_texture, data.hover);
                    isTextureContent = true;
                }
            }
            else
            {
                content = new GUIContent();
            }
        }

        public void Draw()
        {
            Rect cursorRect;
            if (isTextureContent)
            {
                if(GUILayout.Button(content, new GUIStyle(), GUILayout.MaxWidth(texture_width), GUILayout.Height(texture_height))){
                    data.action.Perform();
                }
                cursorRect = GUILayoutUtility.GetLastRect();
                GUILayout.Space(8);
            }
            else
            {
                if (GUILayout.Button(content, GUILayout.ExpandWidth(false), GUILayout.Height(texture_height)))
                    data.action.Perform();
                cursorRect = GUILayoutUtility.GetLastRect();
                GUILayout.Space(2);
            }
            EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.Link);
        }

        public static void DrawList(List<FooterButton> list)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(2);
            foreach (FooterButton b in list)
            {
                b.Draw();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    public class ThryHeaderDrawer : MaterialPropertyDrawer
    {
        private MaterialProperty property;

        private bool expanded;

        private string keyword;
        private string end;

        public bool isHideable;

        int p_xOffset;
        int p_xOffset_total;
        public int xOffset
        {
            set
            {
                p_xOffset = value;
                p_xOffset_total = value * 15 + 15;
            }
        }

        private ButtonData button;

        public ThryHeaderDrawer(string end, string keyword, string buttonText, string buttonHover, string buttonAction, float isHideable)
        {
            this.end = end;
            this.keyword = keyword;

            button = new ButtonData();
            button.text = buttonText;
            button.hover = buttonHover;
            button.action = DefineableAction.ParseDrawerParameter(buttonAction);

            this.isHideable = isHideable == 1;
        }
        
        public ThryHeaderDrawer(string end, string keyword, string buttonText, string buttonHover, string buttonAction) : this(end, keyword, buttonText, buttonHover, buttonAction, 0          ) { }
        public ThryHeaderDrawer(string end, string keyword, float isHideable) :                                           this(end, keyword, null,       null,        null,         isHideable ) { }
        public ThryHeaderDrawer(string end, string keyword, string buttonAction) :                                        this(end, keyword, null,       null,        buttonAction, 0          ) { }
        public ThryHeaderDrawer(string end, string keyword) :                                                             this(end, keyword, null,       null,        null,         0          ) { }
        public ThryHeaderDrawer(string end) :                                                                             this(end, null   , null,       null,        null,         0          ) { }

        public ThryHeaderDrawer(float isHideable) :                                                                       this(null,null,    null,       null,        null,         isHideable ) { }
        public ThryHeaderDrawer(float isHideable, string end) :                                                           this(end, null,    null,       null,        null,         isHideable ) { }
        public ThryHeaderDrawer(float isHideable, string buttonText, string buttonHover, string buttonAction) :           this(null,null,    buttonText, buttonHover, buttonAction, 0          ) { }
        public ThryHeaderDrawer(float isHideable, string end, string buttonText, string buttonHover, string buttonAction):this(end, null,    buttonText, buttonHover, buttonAction, isHideable ) { }

        public ThryHeaderDrawer(){}

        public string GetEndProperty()
        {
            return end;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            DrawingData.LastPropertyDrawerType = DrawerType.Header;
            DrawingData.LastPropertyDrawer = this;
            return base.GetPropertyHeight(prop, label, editor);
        }

        public bool is_expanded
        {
            get
            {
                return expanded;
            }
        }

        public void Toggle()
        {
            expanded = !expanded;
            foreach (Material m in ShaderEditor.Active.Materials) m.SetFloat(property.name, expanded ? 1 : 0);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (this.property == null)
            {
                this.property = prop;
                this.expanded = prop.floatValue == 1;
            }

            PropertyOptions options = ShaderEditor.Active.CurrentProperty.options;
            Event e = Event.current;

            position.width -= p_xOffset_total - position.x;
            position.x = p_xOffset_total;

            DrawingData.LastGuiObjectHeaderRect = position;

            DrawBoxAndContent(position, e, label, options);

            DrawSmallArrow(position, e);
            HandleToggleInput(position);
        }

        private void DrawBoxAndContent(Rect rect, Event e, GUIContent content, PropertyOptions options)
        {
            if (options.reference_property != null && ShaderEditor.Active.PropertyDictionary.ContainsKey(options.reference_property))
            {
                GUI.Box(rect, new GUIContent("     " + content.text, content.tooltip), Styles.dropDownHeader);
                DrawIcons(rect, options, e);

                Rect togglePropertyRect = new Rect(rect);
                togglePropertyRect.x += 5;
                togglePropertyRect.y += 1;
                togglePropertyRect.height -= 4;
                togglePropertyRect.width = GUI.skin.font.fontSize * 3;
                float fieldWidth = EditorGUIUtility.fieldWidth;
                EditorGUIUtility.fieldWidth = 20;
                ShaderProperty prop = ShaderEditor.Active.PropertyDictionary[options.reference_property];

                int xOffset = prop.xOffset;
                prop.xOffset = 0;
                prop.Draw(new CRect(togglePropertyRect), new GUIContent(), isInHeader: true);
                prop.xOffset = xOffset;
                EditorGUIUtility.fieldWidth = fieldWidth;
            }else if(keyword != null)
            {
                GUI.Box(rect, "     " + content.text, Styles.dropDownHeader);
                DrawIcons(rect, options, e);

                Rect togglePropertyRect = new Rect(rect);
                togglePropertyRect.x += 20;
                togglePropertyRect.width = 20;

                EditorGUI.BeginChangeCheck();
                bool keywordOn = EditorGUI.Toggle(togglePropertyRect, "", ShaderEditor.Active.Materials[0].IsKeywordEnabled(keyword));
                if (EditorGUI.EndChangeCheck())
                {
                    MaterialHelper.ToggleKeyword(ShaderEditor.Active.Materials, keyword, keywordOn);
                }
            }
            else
            {
                GUI.Box(rect, content, Styles.dropDownHeader);
                DrawIcons(rect, options, e);
            }

        }

        /// <summary>
        /// Draws the icons for ShaderEditor features like linking and copying
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="e"></param>
        private void DrawIcons(Rect rect, PropertyOptions options, Event e)
        {
            Rect buttonRect = new Rect(rect);
            buttonRect.y += 1;
            buttonRect.height -= 4;
            buttonRect.width = buttonRect.height;

            float right = rect.x + rect.width;
            buttonRect.x = right - 56;
            DrawHelpButton(buttonRect, options, e);
            buttonRect.x = right - 38;
            DrawLinkSettings(buttonRect, e);
            buttonRect.x = right - 20;
            DrawDowdownSettings(buttonRect, e);
        }

        private void DrawHelpButton(Rect rect, PropertyOptions options, Event e)
        {
            ButtonData button = this.button != null ? this.button : options.button_help;
            if (button != null && button.condition_show.Test())
            {
                if (GuiHelper.Button(rect, Styles.icon_style_help))
                {
                    ShaderEditor.Input.Use();
                    if (button.action != null) button.action.Perform();
                }
            }
        }

        private void DrawDowdownSettings(Rect rect, Event e)
        {
            if (GuiHelper.Button(rect, Styles.icon_style_menu))
            {
                ShaderEditor.Input.Use();
                Rect buttonRect = new Rect(rect);
                buttonRect.width = 150;
                buttonRect.x = Mathf.Min(Screen.width - buttonRect.width, buttonRect.x);
                buttonRect.height = 60;
                float maxY = GUIUtility.ScreenToGUIPoint(new Vector2(0, EditorWindow.focusedWindow.position.y + Screen.height)).y - 2.5f * buttonRect.height;
                buttonRect.y = Mathf.Min(buttonRect.y - buttonRect.height / 2, maxY);

                ShowHeaderContextMenu(buttonRect, ShaderEditor.Active.CurrentProperty, ShaderEditor.Active.Materials[0]);
            }
        }

        private void DrawLinkSettings(Rect rect, Event e)
        {
            if (GuiHelper.Button(rect, Styles.icon_style_linked, Styles.COLOR_ICON_ACTIVE_CYAN, MaterialLinker.IsLinked(ShaderEditor.Active.CurrentProperty.materialProperty)))
            {
                ShaderEditor.Input.Use();
                List<Material> linked_materials = MaterialLinker.GetLinked(ShaderEditor.Active.CurrentProperty.materialProperty);
                MaterialLinker.Popup(rect, linked_materials, ShaderEditor.Active.CurrentProperty.materialProperty);
            }
        }

        void ShowHeaderContextMenu(Rect position, ShaderPart property, Material material)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset"), false, delegate ()
            {
                property.CopyFromMaterial(new Material(material.shader), true);
                List<Material> linked_materials = MaterialLinker.GetLinked(property.materialProperty);
                if (linked_materials != null)
                    foreach (Material m in linked_materials)
                        property.CopyToMaterial(m, true);
            });
            menu.AddItem(new GUIContent("Copy"), false, delegate ()
            {
                Mediator.copy_material = new Material(material);
                Mediator.transfer_group = property;
            });
            menu.AddItem(new GUIContent("Paste"), false, delegate ()
            {
                if (Mediator.copy_material != null || Mediator.transfer_group != null)
                {
                    property.TransferFromMaterialAndGroup(Mediator.copy_material, Mediator.transfer_group, true);
                    List<Material> linked_materials = MaterialLinker.GetLinked(property.materialProperty);
                    if (linked_materials != null)
                        foreach (Material m in linked_materials)
                            property.CopyToMaterial(m, true);
                }
            });
            menu.DropDown(position);
        }

        private void DrawSmallArrow(Rect rect, Event e)
        {
            if (e.type == EventType.Repaint)
            {
                var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
                EditorStyles.foldout.Draw(toggleRect, false, false, expanded, false);
            }
        }

        private void HandleToggleInput(Rect rect)
        {
            //Ignore unity uses is cause disabled will use the event to prevent toggling
            if (ShaderEditor.Input.LeftClick_IgnoreLocked && rect.Contains(ShaderEditor.Input.mouse_position) && !ShaderEditor.Input.is_alt_down)
            {
                this.Toggle();
                ShaderEditor.Input.Use();
            }
        }
    }
}