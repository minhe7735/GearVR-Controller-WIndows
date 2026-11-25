using System;

namespace GearVR_Controller
{
    // Coordinates are in the same virtual-desktop space used by Get/SetCursorPos.
    internal readonly struct DesktopBounds
    {
        public readonly int Left, Top, Width, Height;
        public DesktopBounds(int left, int top, int width, int height)
        {
            Left = left; Top = top; Width = width; Height = height;
        }
        public bool IsValid => Width > 1 && Height > 1;
        public double ToMotionX(double x) => ((x - Left) / (Width - 1) - 0.5) * 1.5;
        public double ToMotionY(double y) => (y - Top) / (Height - 1) - 0.5;
        public double ToScreenX(double x) => Left + (x / 1.5 + 0.5) * (Width - 1);
        public double ToScreenY(double y) => Top + (y + 0.5) * (Height - 1);
    }
}
