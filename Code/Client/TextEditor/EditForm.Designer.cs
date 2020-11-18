namespace TextEditor
{
    partial class EditForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ContentsBox = new System.Windows.Forms.RichTextBox();
            this.operationTimer = new System.Windows.Forms.Timer(this.components);
            this.delTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.btnDownload = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ContentsBox
            // 
            this.ContentsBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ContentsBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.ContentsBox.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.ContentsBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.ContentsBox.Location = new System.Drawing.Point(0, 31);
            this.ContentsBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ContentsBox.Name = "ContentsBox";
            this.ContentsBox.Size = new System.Drawing.Size(635, 551);
            this.ContentsBox.TabIndex = 0;
            this.ContentsBox.Text = "";
            this.ContentsBox.WordWrap = false;
            this.ContentsBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ContentsBox_KeyDown);
            this.ContentsBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ContentsBox_KeyPress);
            this.ContentsBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ContentsBox_KeyUp);
            this.ContentsBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.ContentsBox_PreviewKeyDown);
            // 
            // operationTimer
            // 
            this.operationTimer.Enabled = true;
            this.operationTimer.Interval = 200;
            this.operationTimer.Tick += new System.EventHandler(this.operationTimer_Tick);
            // 
            // delTimer
            // 
            this.delTimer.Enabled = true;
            this.delTimer.Interval = 1000;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(6, 3, 0, 3);
            this.menuStrip1.Size = new System.Drawing.Size(634, 29);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnDownload});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(43, 20);
            this.toolStripMenuItem1.Text = "파일";
            // 
            // btnDownload
            // 
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(122, 22);
            this.btnDownload.Text = "다운로드";
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.FileName = "*.txt";
            this.saveFileDialog1.Filter = "텍스트 문서(.txt)|*.txt|모든 파일|*.*";
            // 
            // EditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 511);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.ContentsBox);
            this.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "EditForm";
            this.Text = "EditForm";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Timer operationTimer;
        private System.Windows.Forms.Timer delTimer;
        public System.Windows.Forms.RichTextBox ContentsBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem btnDownload;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}