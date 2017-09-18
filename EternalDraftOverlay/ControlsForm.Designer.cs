namespace EternalDraftOverlay
{
    partial class ControlsForm
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
            this.rescanButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // rescanButton
            // 
            this.rescanButton.Location = new System.Drawing.Point(1, 1);
            this.rescanButton.Name = "rescanButton";
            this.rescanButton.Size = new System.Drawing.Size(75, 23);
            this.rescanButton.TabIndex = 0;
            this.rescanButton.Text = "Rescan";
            this.rescanButton.UseVisualStyleBackColor = true;
            this.rescanButton.Click += new System.EventHandler(this.rescanButton_Click);
            // 
            // Controls
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(114, 46);
            this.Controls.Add(this.rescanButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Controls";
            this.Text = "Controls";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button rescanButton;
    }
}