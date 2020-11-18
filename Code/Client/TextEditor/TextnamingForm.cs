using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class TextnamingForm : Form
    {
        public string file_name = "";
        public int respawn;
        MainForm m = null;

        public TextnamingForm(MainForm mf)
        {
            InitializeComponent();
            CenterToParent(); // 부모 중앙 위치
            respawn = 0;
            m = mf;
        }

        private void btn_ok_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbox_textnaming.Text.Trim()))
            {
                respawn = 1;
                this.Close();
            }
            else
            {
                MessageBox.Show("파일명을 입력해주세요.");
            }
        }

        private void tbox_textnaming_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(tbox_textnaming.Text.Trim()))
                {
                    respawn = 1;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("파일명을 입력해주세요.");
                }
            }
        }
    }
}
