using UnityEngine.UIElements;

namespace Asaki.Editor.Utilities.Extensions
{
    public static class AsakiUIStyleExtensions
    {
        // =========================================================
        // Padding 拓展
        // =========================================================

        /// <summary>
        /// 一键设置所有方向的 Padding
        /// </summary>
        public static void SetPadding(this IStyle style, float value)
        {
            style.paddingLeft = value;
            style.paddingRight = value;
            style.paddingTop = value;
            style.paddingBottom = value;
        }

        /// <summary>
        /// 分别设置水平和垂直 Padding
        /// </summary>
        public static void SetPadding(this IStyle style, float horizontal, float vertical)
        {
            style.paddingLeft = horizontal;
            style.paddingRight = horizontal;
            style.paddingTop = vertical;
            style.paddingBottom = vertical;
        }

        // =========================================================
        // Margin 拓展
        // =========================================================

        /// <summary>
        /// 一键设置所有方向的 Margin
        /// </summary>
        public static void SetMargin(this IStyle style, float value)
        {
            style.marginLeft = value;
            style.marginRight = value;
            style.marginTop = value;
            style.marginBottom = value;
        }

        /// <summary>
        /// 分别设置水平和垂直 Margin
        /// </summary>
        public static void SetMargin(this IStyle style, float horizontal, float vertical)
        {
            style.marginLeft = horizontal;
            style.marginRight = horizontal;
            style.marginTop = vertical;
            style.marginBottom = vertical;
        }

        // =========================================================
        // Border Radius 拓展
        // =========================================================

        /// <summary>
        /// 一键设置圆角
        /// </summary>
        public static void SetBorderRadius(this IStyle style, float value)
        {
            style.borderTopLeftRadius = value;
            style.borderTopRightRadius = value;
            style.borderBottomLeftRadius = value;
            style.borderBottomRightRadius = value;
        }

        // =========================================================
        // Border Width 拓展
        // =========================================================

        public static void SetBorderWidth(this IStyle style, float value)
        {
            style.borderLeftWidth = value;
            style.borderRightWidth = value;
            style.borderTopWidth = value;
            style.borderBottomWidth = value;
        }

        // =========================================================
        // Border Color 拓展
        // =========================================================

        /// <summary>
        /// 一键设置所有方向的 Border Color
        /// </summary>
        /// <param name="style">要设置的样式</param>
        /// <param name="color">要设置的颜色</param>
        public static void SetBorderColor(this IStyle style, UnityEngine.Color color)
        {
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
        }
    }
}
