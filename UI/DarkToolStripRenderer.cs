namespace RE4_PS2_MOD_WORKSPACE;

/// <summary>Shared dark renderer for every context menu created by the application.</summary>
internal sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer() : base(new DarkColorTable()) { RoundedEdges=false; }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if(e.ToolStrip is ToolStripDropDown)
            e.TextColor=e.Item.Enabled?Color.FromArgb(238,240,244):Color.FromArgb(105,111,122);
        base.OnRenderItemText(e);
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        static readonly Color Surface=Color.FromArgb(31,35,43),Hover=Color.FromArgb(92,42,45),Pressed=Color.FromArgb(116,45,49),Border=Color.FromArgb(61,67,78);
        public DarkColorTable(){UseSystemColors=false;}
        public override Color ToolStripDropDownBackground=>Surface;
        public override Color ImageMarginGradientBegin=>Surface;
        public override Color ImageMarginGradientMiddle=>Surface;
        public override Color ImageMarginGradientEnd=>Surface;
        public override Color MenuBorder=>Border;
        public override Color MenuItemBorder=>Color.FromArgb(151,55,59);
        public override Color MenuItemSelected=>Hover;
        public override Color MenuItemSelectedGradientBegin=>Hover;
        public override Color MenuItemSelectedGradientEnd=>Hover;
        public override Color MenuItemPressedGradientBegin=>Pressed;
        public override Color MenuItemPressedGradientMiddle=>Pressed;
        public override Color MenuItemPressedGradientEnd=>Pressed;
        public override Color SeparatorDark=>Color.FromArgb(50,55,65);
        public override Color SeparatorLight=>Color.FromArgb(50,55,65);
        public override Color CheckBackground=>Hover;
        public override Color CheckSelectedBackground=>Pressed;
        public override Color CheckPressedBackground=>Pressed;
    }
}
