using System.Drawing;
using System.Windows.Forms;

namespace RE4_PS2_LIT_Editor.Utils
{
    public class TabCreator
    {
        public TabPage CreateTab(int lightNumber)
        {
            TabPage tab = new TabPage();
            tab.BackColor = Color.FromArgb(30, 30, 30);
            tab.Text = "Light " + lightNumber.ToString();

            GroupBox groupBox = new GroupBox();
            groupBox.Text = "Main Settings";
            groupBox.Size = new Size(354, 286);
            groupBox.Location = new Point(19, 19);
            groupBox.BackColor = Color.FromArgb(40, 40, 50);

            tab.Controls.Add(TypeSpotlight());
            tab.Controls.Add(groupBox);
            return tab;
        }

        public GroupBox TypeQuadratic()
        {
            GroupBox groupBox = new GroupBox();
            groupBox.Text = "Parameters: Quadratic";
            groupBox.Size = new Size(354, 139);
            groupBox.Location = new Point(19, 330);
            groupBox.BackColor = Color.FromArgb(40, 40, 50);
            groupBox.ForeColor = Color.White;

            Label label = new Label();
            label.Text = "Smooth Edge";
            label.Location = new Point(123, 52);
            groupBox.ForeColor = Color.White;

            NumericUpDown numericUpDown = new NumericUpDown();
            numericUpDown.Location = new Point(112, 69);
            numericUpDown.Size = new Size(92, 20);
            numericUpDown.DecimalPlaces = 2;
            numericUpDown.Maximum = 100000;
            numericUpDown.Minimum = -100000;
            numericUpDown.TextAlign = HorizontalAlignment.Center;

            groupBox.Controls.Add(numericUpDown);
            groupBox.Controls.Add(label);
            return groupBox;
        }

        public GroupBox TypeSpotlight()
        {
            GroupBox groupBox = new GroupBox();
            groupBox.Text = "Parameters: Spotlight";
            groupBox.Size = new Size(354, 139);
            groupBox.Location = new Point(19, 330);
            groupBox.BackColor = Color.FromArgb(40, 40, 50);
            groupBox.ForeColor = Color.White;

            Label posX = new Label();
            posX.Text = "Relative Position X";
            posX.Location = new Point(18, 30);
            groupBox.ForeColor = Color.White;

            Label posY = new Label();
            posY.Text = "Relative Position Y";
            posY.Location = new Point(128, 30);
            groupBox.ForeColor = Color.White;

            Label posZ = new Label();
            posZ.Text = "Relative Position Z";
            posZ.Location = new Point(240, 30);
            groupBox.ForeColor = Color.White;

            Label range = new Label();
            range.Text = "Range";
            range.Location = new Point(97, 83);
            groupBox.ForeColor = Color.White;

            Label smooth = new Label();
            smooth.Text = "Smooth Edge";
            smooth.Location = new Point(191, 83);
            groupBox.ForeColor = Color.White;

            NumericUpDown spinPosX = new NumericUpDown();
            spinPosX.Location = new Point(20, 47);
            spinPosX.Size = new Size(92, 20);
            spinPosX.DecimalPlaces = 2;
            spinPosX.Maximum = 300000;
            spinPosX.Minimum = -300000;
            spinPosX.TextAlign = HorizontalAlignment.Center;
            spinPosX.Name = "spinPosX";

            NumericUpDown spinPosY = new NumericUpDown();
            spinPosY.Location = new Point(130, 46);
            spinPosY.Size = new Size(92, 20);
            spinPosY.DecimalPlaces = 2;
            spinPosY.Maximum = 300000;
            spinPosY.Minimum = -300000;
            spinPosY.TextAlign = HorizontalAlignment.Center;
            spinPosY.Name = "spinPosY";

            NumericUpDown spinPosZ = new NumericUpDown();
            spinPosZ.Location = new Point(242, 46);
            spinPosZ.Size = new Size(92, 20);
            spinPosZ.DecimalPlaces = 2;
            spinPosZ.Maximum = 300000;
            spinPosZ.Minimum = -300000;
            spinPosZ.TextAlign = HorizontalAlignment.Center;
            spinPosZ.Name = "spinPosZ";

            NumericUpDown spinRange = new NumericUpDown();
            spinRange.Location = new Point(70, 100);
            spinRange.Size = new Size(92, 20);
            spinRange.DecimalPlaces = 2;
            spinRange.Maximum = 300000;
            spinRange.Minimum = -300000;
            spinRange.TextAlign = HorizontalAlignment.Center;

            NumericUpDown spinSmooth = new NumericUpDown();
            spinSmooth.Location = new Point(180, 99);
            spinSmooth.Size = new Size(92, 20);
            spinSmooth.DecimalPlaces = 2;
            spinSmooth.Maximum = 300000;
            spinSmooth.Minimum = -300000;
            spinSmooth.TextAlign = HorizontalAlignment.Center;

            groupBox.Controls.Add(spinPosX);
            groupBox.Controls.Add(spinPosY);
            groupBox.Controls.Add(spinPosZ);
            groupBox.Controls.Add(spinRange);
            groupBox.Controls.Add(spinSmooth);
            groupBox.Controls.Add(posX);
            groupBox.Controls.Add(posY);
            groupBox.Controls.Add(posZ);
            groupBox.Controls.Add(range);
            groupBox.Controls.Add(smooth);
            return groupBox;
        }
    }
}
