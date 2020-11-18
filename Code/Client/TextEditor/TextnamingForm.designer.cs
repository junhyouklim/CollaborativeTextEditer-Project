namespace TextEditor
{
    partial class TextnamingForm
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
            this.tbox_textnaming = new System.Windows.Forms.TextBox();
            this.btn_ok = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbox_textnaming
            // 
            this.tbox_textnaming.Location = new System.Drawing.Point(12, 26);
            this.tbox_textnaming.Name = "tbox_textnaming";
            this.tbox_textnaming.Size = new System.Drawing.Size(213, 21);
            this.tbox_textnaming.TabIndex = 0;
            this.tbox_textnaming.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbox_textnaming_KeyDown);
            // 
            // btn_ok
            // 
            this.btn_ok.Location = new System.Drawing.Point(243, 26);
            this.btn_ok.Name = "btn_ok";
            this.btn_ok.Size = new System.Drawing.Size(74, 23);
            this.btn_ok.TabIndex = 1;
            this.btn_ok.Text = "입력";
            this.btn_ok.UseVisualStyleBackColor = true;
            this.btn_ok.Click += new System.EventHandler(this.btn_ok_Click);
            // 
            // TextnamingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(329, 63);
            this.Controls.Add(this.btn_ok);
            this.Controls.Add(this.tbox_textnaming);
            this.Name = "TextnamingForm";
            this.Text = "새로 만들 파일명을 입력하세요.";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_ok;
        public System.Windows.Forms.TextBox tbox_textnaming;
    }
}