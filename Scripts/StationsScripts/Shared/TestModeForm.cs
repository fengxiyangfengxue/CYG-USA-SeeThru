using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Test.Definition;
using UserHelpers.Helpers;

namespace Test.StationsScripts.Shared
{
    public partial class TestModeForm : Form
    {

        public Test_Mode SelectedMode = Test_Mode.PRIME;
        ITestProject _Project = null;

        List<(string text, Color btnColor, Color txtColor, Test_Mode mode)> _Options = new List<(string, Color, Color, Test_Mode)>();

        public TestModeForm(ITestProject project)
        {
            _Project = project;
            InitializeComponent();
        }

        private void Form_Shown(object sender, EventArgs e)
        {
            //var rect = _Project.GetAppRect(); //获取当前Project的屏幕坐标 
            //                                  //将Form定位到Project上方
            //double x = rect.Left + (rect.Width - this.Width) / 2;
            //double y = rect.Top + (rect.Height - this.Height) / 2;
            //this.Left = (int)x;
            //this.Top = (int)y;

            this.Width = 400;
            this.Height = 580;

            this.Focus();
        }


        private void TestModeForm_Load(object sender, EventArgs e)
        {
            _Options.Add(("PRIME", Color.Green, Color.White, Test_Mode.PRIME));
            _Options.Add(("FA", Color.Red, Color.White, Test_Mode.FA));
            _Options.Add(("REWORK", Color.Brown, Color.White, Test_Mode.REWORK));
            _Options.Add(("GRR", Color.Purple, Color.White, Test_Mode.GRR));
            _Options.Add(("REL", Color.DarkGray, Color.White, Test_Mode.REL));


            int x = 40;
            int y = 50; 
            _Options.ForEach(o =>
            {
                Button btn = new Button();
                btn.Text = o.text;
                btn.BackColor = o.btnColor;
                btn.ForeColor = o.txtColor;
                btn.Height = 60; 
                btn.Font = new Font("", 14);
                btn.Left = x;
                btn.Top = y;
                btn.Tag = o.mode;
                btn.Click += TestMode_Click;
                btn.Width = 300;
                this.Controls.Add(btn);
                y = y + btn.Height + 20;

            });
             

        }

        void TestMode_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            SelectedMode = (Test_Mode)btn.Tag; 
            this.Close();
        }
    }
}
