
namespace KTIRobot
{
    partial class MainForm
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
            this.ExitBtn = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.GripClose = new System.Windows.Forms.Button();
            this.GripOpen = new System.Windows.Forms.Button();
            this.notifybar = new System.Windows.Forms.StatusStrip();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ExitBtn
            // 
            this.ExitBtn.Location = new System.Drawing.Point(704, 357);
            this.ExitBtn.Name = "ExitBtn";
            this.ExitBtn.Size = new System.Drawing.Size(75, 23);
            this.ExitBtn.TabIndex = 0;
            this.ExitBtn.Text = "Exit";
            this.ExitBtn.UseVisualStyleBackColor = true;
            this.ExitBtn.Click += new System.EventHandler(this.ExitBtn_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.GripClose);
            this.groupBox1.Controls.Add(this.GripOpen);
            this.groupBox1.Location = new System.Drawing.Point(698, 55);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(90, 131);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Gripper";
            // 
            // GripClose
            // 
            this.GripClose.Location = new System.Drawing.Point(8, 76);
            this.GripClose.Name = "GripClose";
            this.GripClose.Size = new System.Drawing.Size(75, 23);
            this.GripClose.TabIndex = 1;
            this.GripClose.Text = "Close";
            this.GripClose.UseVisualStyleBackColor = true;
            this.GripClose.Click += new System.EventHandler(this.GripClose_Click);
            // 
            // GripOpen
            // 
            this.GripOpen.Location = new System.Drawing.Point(6, 46);
            this.GripOpen.Name = "GripOpen";
            this.GripOpen.Size = new System.Drawing.Size(75, 23);
            this.GripOpen.TabIndex = 0;
            this.GripOpen.Text = "Open";
            this.GripOpen.UseVisualStyleBackColor = true;
            this.GripOpen.Click += new System.EventHandler(this.GripOpen_Click);
            // 
            // notifybar
            // 
            this.notifybar.Location = new System.Drawing.Point(0, 428);
            this.notifybar.Name = "notifybar";
            this.notifybar.Size = new System.Drawing.Size(800, 22);
            this.notifybar.TabIndex = 2;
            this.notifybar.Text = "Notification";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.notifybar);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.ExitBtn);
            this.Name = "MainForm";
            this.Text = "KTI Robot";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ExitBtn;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button GripOpen;
        private System.Windows.Forms.Button GripClose;
        private System.Windows.Forms.StatusStrip notifybar;
    }
}

