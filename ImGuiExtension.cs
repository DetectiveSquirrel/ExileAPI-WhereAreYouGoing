using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;

namespace WhereAreYouGoing
{
    public class ImGuiExtension
    {
        // Int Drags
        public static int IntDrag(string labelString, int value, int minValue, int maxValue, float dragSpeed)
        {
            var refValue = value;
            ImGui.DragInt(labelString, ref refValue, dragSpeed, minValue, maxValue);
            return refValue;
        }

        // Color Pickers
        public static Color ColorPicker(string labelName, Color inputColor)
        {
            var color = inputColor.ToVector4();
            var colorToVect4 = new ImGuiVector4(color.X, color.Y, color.Z, color.W);
            if (ImGui.ColorEdit4(labelName, ref colorToVect4, ImGuiColorEditFlags.AlphaBar)) return new Color(colorToVect4.X, colorToVect4.Y, colorToVect4.Z, colorToVect4.W);
            return inputColor;
        }

        // Checkboxes
        public static bool Checkbox(string labelString, bool boolValue)
        {
            ImGui.Checkbox(labelString, ref boolValue);
            return boolValue;
        }
    }
}
