namespace Asaki.Core.UI
{
    public enum AsakiUIWidgetType
    {
        Container, // 空容器 (RectTransform)
        Text, // 文本 (Legacy)
        TextMeshPro, // TextMeshPro
        Button, // 按钮
        Image, // 图片
        InputField, // 输入框
        Dropdown, // 下拉菜单
        ScrollView, // 滚动视图
        Slider, // 滑动条
        Toggle, // 开关
        Custom, // 自定义 (需指定 Prefab 路径)
    }
}
