namespace W3LPL
{
    partial class Form1
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
                if (w3lpl != null) w3lpl.Dispose();
                if (tooltip != null) tooltip.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.buttonStart = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.labelQDepth = new System.Windows.Forms.Label();
            this.textBoxClusterServer = new System.Windows.Forms.TextBox();
            this.textBoxCallsign = new System.Windows.Forms.TextBox();
            this.labelStatusQServer = new System.Windows.Forms.Label();
            this.textBoxPortLocal = new System.Windows.Forms.TextBox();
            this.checkedListBoxNewSpotters = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonBackup = new System.Windows.Forms.Button();
            this.checkedListBoxReviewedSpotters = new W3LPL.ColorCodedCheckedListBox();
            this.form1BindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.form1BindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.form1BindingSource2 = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource2)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(17, 16);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(4);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(100, 28);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.ButtonStart_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(13, 120);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(4);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(767, 154);
            this.richTextBox1.TabIndex = 3;
            this.richTextBox1.Text = "";
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // labelQDepth
            // 
            this.labelQDepth.AutoSize = true;
            this.labelQDepth.Location = new System.Drawing.Point(250, 54);
            this.labelQDepth.Name = "labelQDepth";
            this.labelQDepth.Size = new System.Drawing.Size(16, 17);
            this.labelQDepth.TabIndex = 6;
            this.labelQDepth.Text = "0";
            this.labelQDepth.Click += new System.EventHandler(this.LabelQDepth_Click);
            // 
            // textBoxClusterServer
            // 
            this.textBoxClusterServer.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::W3LPL.Properties.Settings.Default, "ClusterServer", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxClusterServer.Location = new System.Drawing.Point(253, 19);
            this.textBoxClusterServer.Name = "textBoxClusterServer";
            this.textBoxClusterServer.Size = new System.Drawing.Size(178, 22);
            this.textBoxClusterServer.TabIndex = 7;
            this.textBoxClusterServer.Text = global::W3LPL.Properties.Settings.Default.ClusterServer;
            this.textBoxClusterServer.Leave += new System.EventHandler(this.TextBoxCluster_Leave);
            // 
            // textBoxCallsign
            // 
            this.textBoxCallsign.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxCallsign.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::W3LPL.Properties.Settings.Default, "Callsign", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxCallsign.Location = new System.Drawing.Point(134, 19);
            this.textBoxCallsign.Name = "textBoxCallsign";
            this.textBoxCallsign.Size = new System.Drawing.Size(100, 22);
            this.textBoxCallsign.TabIndex = 4;
            this.textBoxCallsign.Text = global::W3LPL.Properties.Settings.Default.Callsign;
            // 
            // labelStatusQServer
            // 
            this.labelStatusQServer.AutoSize = true;
            this.labelStatusQServer.Location = new System.Drawing.Point(103, 54);
            this.labelStatusQServer.Name = "labelStatusQServer";
            this.labelStatusQServer.Size = new System.Drawing.Size(54, 17);
            this.labelStatusQServer.TabIndex = 8;
            this.labelStatusQServer.Text = "W3LPL";
            // 
            // textBoxPortLocal
            // 
            this.textBoxPortLocal.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxPortLocal.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::W3LPL.Properties.Settings.Default, "PortLocal", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxPortLocal.Location = new System.Drawing.Point(17, 51);
            this.textBoxPortLocal.Name = "textBoxPortLocal";
            this.textBoxPortLocal.Size = new System.Drawing.Size(71, 22);
            this.textBoxPortLocal.TabIndex = 9;
            this.textBoxPortLocal.Text = global::W3LPL.Properties.Settings.Default.PortLocal;
            // 
            // checkedListBoxNewSpotters
            // 
            this.checkedListBoxNewSpotters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBoxNewSpotters.CheckOnClick = true;
            this.checkedListBoxNewSpotters.FormattingEnabled = true;
            this.checkedListBoxNewSpotters.Location = new System.Drawing.Point(478, 21);
            this.checkedListBoxNewSpotters.Name = "checkedListBoxNewSpotters";
            this.checkedListBoxNewSpotters.Size = new System.Drawing.Size(137, 89);
            this.checkedListBoxNewSpotters.TabIndex = 11;
            this.checkedListBoxNewSpotters.SelectedIndexChanged += new System.EventHandler(this.CheckedListBoxNewSpotters_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(478, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 17);
            this.label1.TabIndex = 12;
            this.label1.Text = "New Spotters";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(643, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 17);
            this.label2.TabIndex = 13;
            this.label2.Text = "Reviewed Spotters";
            // 
            // buttonBackup
            // 
            this.buttonBackup.Location = new System.Drawing.Point(17, 80);
            this.buttonBackup.Name = "buttonBackup";
            this.buttonBackup.Size = new System.Drawing.Size(100, 28);
            this.buttonBackup.TabIndex = 14;
            this.buttonBackup.Text = "Backup";
            this.buttonBackup.UseVisualStyleBackColor = true;
            this.buttonBackup.Click += new System.EventHandler(this.ButtonBackup_Click);
            // 
            // checkedListBoxReviewedSpotters
            // 
            this.checkedListBoxReviewedSpotters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBoxReviewedSpotters.CheckedColor = System.Drawing.Color.Green;
            this.checkedListBoxReviewedSpotters.CheckOnClick = true;
            this.checkedListBoxReviewedSpotters.FormattingEnabled = true;
            this.checkedListBoxReviewedSpotters.IndeterminateColor = System.Drawing.Color.Orange;
            this.checkedListBoxReviewedSpotters.Location = new System.Drawing.Point(637, 21);
            this.checkedListBoxReviewedSpotters.Name = "checkedListBoxReviewedSpotters";
            this.checkedListBoxReviewedSpotters.Size = new System.Drawing.Size(137, 89);
            this.checkedListBoxReviewedSpotters.TabIndex = 10;
            this.checkedListBoxReviewedSpotters.UncheckedColor = System.Drawing.Color.Red;
            this.checkedListBoxReviewedSpotters.SelectedIndexChanged += new System.EventHandler(this.CheckedListBoxReviewedSpotters_SelectedIndexChanged);
            this.checkedListBoxReviewedSpotters.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CheckedListBoxReviewedSpotters_MouseUp);
            // 
            // form1BindingSource1
            // 
            this.form1BindingSource1.DataSource = typeof(W3LPL.Form1);
            // 
            // form1BindingSource
            // 
            this.form1BindingSource.DataSource = typeof(W3LPL.Form1);
            // 
            // form1BindingSource2
            // 
            this.form1BindingSource2.DataSource = typeof(W3LPL.Form1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(793, 287);
            this.Controls.Add(this.buttonBackup);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkedListBoxNewSpotters);
            this.Controls.Add(this.checkedListBoxReviewedSpotters);
            this.Controls.Add(this.textBoxPortLocal);
            this.Controls.Add(this.labelStatusQServer);
            this.Controls.Add(this.textBoxClusterServer);
            this.Controls.Add(this.labelQDepth);
            this.Controls.Add(this.textBoxCallsign);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.buttonStart);
            this.DataBindings.Add(new System.Windows.Forms.Binding("Location", global::W3LPL.Properties.Settings.Default, "Location", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = global::W3LPL.Properties.Settings.Default.Location;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "W3LPL V0.18";
            this.Activated += new System.EventHandler(this.Form1_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResizeEnd += new System.EventHandler(this.Form1_ResizeEnd);
            this.LocationChanged += new System.EventHandler(this.Form1_LocationChanged);
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TextBox textBoxCallsign;
        private System.Windows.Forms.Label labelQDepth;
        private System.Windows.Forms.TextBox textBoxClusterServer;
        private System.Windows.Forms.Label labelStatusQServer;
        private System.Windows.Forms.TextBox textBoxPortLocal;
        private System.Windows.Forms.BindingSource form1BindingSource1;
        private System.Windows.Forms.BindingSource form1BindingSource;
        private System.Windows.Forms.BindingSource form1BindingSource2;
        public System.Windows.Forms.CheckedListBox checkedListBoxNewSpotters;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private ColorCodedCheckedListBox checkedListBoxReviewedSpotters;
        private System.Windows.Forms.Button buttonBackup;
    }
}

