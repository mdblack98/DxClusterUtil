namespace DXClusterUtil
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
                if (clusterClient != null) clusterClient.Dispose();
                if (tooltip != null) tooltip.Dispose();
                if (qrz != null) qrz.Dispose();
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
            components = new System.ComponentModel.Container();
            Properties.Settings settings1 = new Properties.Settings();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            buttonStart = new System.Windows.Forms.Button();
            timer1 = new System.Windows.Forms.Timer(components);
            labelQDepth = new System.Windows.Forms.Label();
            textBoxClusterServer = new System.Windows.Forms.TextBox();
            textBoxCallsign = new System.Windows.Forms.TextBox();
            labelStatusQServer = new System.Windows.Forms.Label();
            textBoxPortLocal = new System.Windows.Forms.TextBox();
            checkedListBoxNewSpotters = new System.Windows.Forms.CheckedListBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            buttonBackup = new System.Windows.Forms.Button();
            buttonCopy = new System.Windows.Forms.Button();
            checkBoxCached = new System.Windows.Forms.CheckBox();
            checkBoxFiltered = new System.Windows.Forms.CheckBox();
            richTextBox1 = new System.Windows.Forms.RichTextBox();
            labelQRZCache = new System.Windows.Forms.Label();
            textBoxPassword = new System.Windows.Forms.TextBox();
            labelClusterCache = new System.Windows.Forms.Label();
            numericUpDownRTTYOffset = new System.Windows.Forms.NumericUpDown();
            listBoxIgnoredSpotters = new System.Windows.Forms.ListBox();
            label3 = new System.Windows.Forms.Label();
            comboBoxTimeIntervalAfter = new System.Windows.Forms.ComboBox();
            form1BindingSource1 = new System.Windows.Forms.BindingSource(components);
            comboBoxTimeIntervalForDump = new System.Windows.Forms.ComboBox();
            checkedListBoxReviewedSpotters = new ColorCodedCheckedListBox();
            form1BindingSource = new System.Windows.Forms.BindingSource(components);
            form1BindingSource2 = new System.Windows.Forms.BindingSource(components);
            checkBoxUSA = new System.Windows.Forms.CheckBox();
            timer2 = new System.Windows.Forms.Timer(components);
            numericUpDownCwMinimum = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)numericUpDownRTTYOffset).BeginInit();
            ((System.ComponentModel.ISupportInitialize)form1BindingSource1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)form1BindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)form1BindingSource2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownCwMinimum).BeginInit();
            SuspendLayout();
            // 
            // buttonStart
            // 
            buttonStart.Location = new System.Drawing.Point(15, 15);
            buttonStart.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new System.Drawing.Size(88, 27);
            buttonStart.TabIndex = 0;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += ButtonStart_Click;
            // 
            // timer1
            // 
            timer1.Interval = 1000;
            timer1.Tick += Timer1_Tick;
            // 
            // labelQDepth
            // 
            labelQDepth.AutoSize = true;
            labelQDepth.Location = new System.Drawing.Point(202, 51);
            labelQDepth.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            labelQDepth.Name = "labelQDepth";
            labelQDepth.Size = new System.Drawing.Size(34, 15);
            labelQDepth.TabIndex = 6;
            labelQDepth.Text = "Time";
            labelQDepth.Click += LabelQDepth_Click;
            // 
            // textBoxClusterServer
            // 
            settings1.Cached = false;
            settings1.Callsign = "";
            settings1.ClusterServer = "";
            settings1.CWMinimum = 0;
            settings1.Filtered = false;
            settings1.HasSetDefaults = false;
            settings1.Ignore = "";
            settings1.Location = new System.Drawing.Point(0, 19);
            settings1.Password = "";
            settings1.PortLocal = "7373";
            settings1.ReviewedSpotters = "";
            settings1.rttyOffset = new decimal(new int[] { 600, 0, 0, int.MinValue });
            settings1.SettingsKey = "";
            settings1.Size = new System.Drawing.Size(705, 263);
            settings1.TimeInterval = 60;
            settings1.TimeIntervalAfter = 6;
            settings1.TimeIntervalForDump = 15;
            settings1.USA = true;
            textBoxClusterServer.DataBindings.Add(new System.Windows.Forms.Binding("Text", settings1, "ClusterServer", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBoxClusterServer.Location = new System.Drawing.Point(264, 17);
            textBoxClusterServer.Margin = new System.Windows.Forms.Padding(2);
            textBoxClusterServer.Name = "textBoxClusterServer";
            textBoxClusterServer.Size = new System.Drawing.Size(125, 23);
            textBoxClusterServer.TabIndex = 3;
            textBoxClusterServer.TextChanged += TextBoxClusterServer_TextChanged;
            textBoxClusterServer.Leave += TextBoxCluster_Leave;
            // 
            // textBoxCallsign
            // 
            textBoxCallsign.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            textBoxCallsign.Location = new System.Drawing.Point(117, 17);
            textBoxCallsign.Margin = new System.Windows.Forms.Padding(2);
            textBoxCallsign.Name = "textBoxCallsign";
            textBoxCallsign.Size = new System.Drawing.Size(68, 23);
            textBoxCallsign.TabIndex = 1;
            // 
            // labelStatusQServer
            // 
            labelStatusQServer.AutoSize = true;
            labelStatusQServer.Location = new System.Drawing.Point(86, 51);
            labelStatusQServer.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            labelStatusQServer.Name = "labelStatusQServer";
            labelStatusQServer.Size = new System.Drawing.Size(73, 15);
            labelStatusQServer.TabIndex = 8;
            labelStatusQServer.Text = "Client Status";
            // 
            // textBoxPortLocal
            // 
            textBoxPortLocal.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            textBoxPortLocal.Location = new System.Drawing.Point(15, 47);
            textBoxPortLocal.Margin = new System.Windows.Forms.Padding(2);
            textBoxPortLocal.Name = "textBoxPortLocal";
            textBoxPortLocal.Size = new System.Drawing.Size(62, 23);
            textBoxPortLocal.TabIndex = 4;
            // 
            // checkedListBoxNewSpotters
            // 
            checkedListBoxNewSpotters.CheckOnClick = true;
            checkedListBoxNewSpotters.FormattingEnabled = true;
            checkedListBoxNewSpotters.Location = new System.Drawing.Point(399, 21);
            checkedListBoxNewSpotters.Margin = new System.Windows.Forms.Padding(2);
            checkedListBoxNewSpotters.Name = "checkedListBoxNewSpotters";
            checkedListBoxNewSpotters.Size = new System.Drawing.Size(121, 94);
            checkedListBoxNewSpotters.TabIndex = 12;
            checkedListBoxNewSpotters.SelectedIndexChanged += CheckedListBoxNewSpotters_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(415, 3);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(77, 15);
            label1.TabIndex = 12;
            label1.Text = "New Spotters";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(539, 3);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(103, 15);
            label2.TabIndex = 13;
            label2.Text = "Reviewed Spotters";
            // 
            // buttonBackup
            // 
            buttonBackup.Location = new System.Drawing.Point(15, 75);
            buttonBackup.Margin = new System.Windows.Forms.Padding(2);
            buttonBackup.Name = "buttonBackup";
            buttonBackup.Size = new System.Drawing.Size(88, 27);
            buttonBackup.TabIndex = 6;
            buttonBackup.Text = "Backup";
            buttonBackup.UseVisualStyleBackColor = true;
            buttonBackup.Click += ButtonBackup_Click;
            // 
            // buttonCopy
            // 
            buttonCopy.Location = new System.Drawing.Point(115, 75);
            buttonCopy.Margin = new System.Windows.Forms.Padding(2);
            buttonCopy.Name = "buttonCopy";
            buttonCopy.Size = new System.Drawing.Size(88, 27);
            buttonCopy.TabIndex = 7;
            buttonCopy.Text = "Copy Log";
            buttonCopy.UseVisualStyleBackColor = true;
            buttonCopy.Click += Button1_Click;
            // 
            // checkBoxCached
            // 
            checkBoxCached.AutoSize = true;
            checkBoxCached.Location = new System.Drawing.Point(216, 81);
            checkBoxCached.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBoxCached.Name = "checkBoxCached";
            checkBoxCached.Size = new System.Drawing.Size(53, 19);
            checkBoxCached.TabIndex = 8;
            checkBoxCached.Text = "Dups";
            checkBoxCached.UseVisualStyleBackColor = true;
            checkBoxCached.CheckedChanged += CheckBoxCached_CheckedChanged;
            // 
            // checkBoxFiltered
            // 
            checkBoxFiltered.AutoSize = true;
            checkBoxFiltered.Location = new System.Drawing.Point(273, 81);
            checkBoxFiltered.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBoxFiltered.Name = "checkBoxFiltered";
            checkBoxFiltered.Size = new System.Drawing.Size(65, 19);
            checkBoxFiltered.TabIndex = 9;
            checkBoxFiltered.Text = "Filtered";
            checkBoxFiltered.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            richTextBox1.Font = new System.Drawing.Font("Lucida Sans Typewriter", 9F);
            richTextBox1.Location = new System.Drawing.Point(7, 133);
            richTextBox1.Margin = new System.Windows.Forms.Padding(2);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new System.Drawing.Size(750, 139);
            richTextBox1.TabIndex = 14;
            richTextBox1.Text = resources.GetString("richTextBox1.Text");
            richTextBox1.WordWrap = false;
            // 
            // labelQRZCache
            // 
            labelQRZCache.AutoSize = true;
            labelQRZCache.Location = new System.Drawing.Point(220, 107);
            labelQRZCache.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelQRZCache.Name = "labelQRZCache";
            labelQRZCache.Size = new System.Drawing.Size(66, 15);
            labelQRZCache.TabIndex = 21;
            labelQRZCache.Text = "QRZ Cache";
            // 
            // textBoxPassword
            // 
            textBoxPassword.Location = new System.Drawing.Point(190, 17);
            textBoxPassword.Margin = new System.Windows.Forms.Padding(2);
            textBoxPassword.Name = "textBoxPassword";
            textBoxPassword.PasswordChar = '*';
            textBoxPassword.Size = new System.Drawing.Size(68, 23);
            textBoxPassword.TabIndex = 2;
            // 
            // labelClusterCache
            // 
            labelClusterCache.AutoSize = true;
            labelClusterCache.Location = new System.Drawing.Point(302, 107);
            labelClusterCache.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelClusterCache.Name = "labelClusterCache";
            labelClusterCache.Size = new System.Drawing.Size(78, 15);
            labelClusterCache.TabIndex = 23;
            labelClusterCache.Tag = "Cluster cache";
            labelClusterCache.Text = "Cluster cache";
            // 
            // numericUpDownRTTYOffset
            // 
            numericUpDownRTTYOffset.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numericUpDownRTTYOffset.Location = new System.Drawing.Point(332, 47);
            numericUpDownRTTYOffset.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            numericUpDownRTTYOffset.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            numericUpDownRTTYOffset.Minimum = new decimal(new int[] { 2000, 0, 0, int.MinValue });
            numericUpDownRTTYOffset.Name = "numericUpDownRTTYOffset";
            numericUpDownRTTYOffset.Size = new System.Drawing.Size(56, 23);
            numericUpDownRTTYOffset.TabIndex = 5;
            numericUpDownRTTYOffset.ValueChanged += NumericUpDownRTTYOffset_ValueChanged;
            // 
            // listBoxIgnoredSpotters
            // 
            listBoxIgnoredSpotters.FormattingEnabled = true;
            listBoxIgnoredSpotters.Location = new System.Drawing.Point(662, 20);
            listBoxIgnoredSpotters.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            listBoxIgnoredSpotters.Name = "listBoxIgnoredSpotters";
            listBoxIgnoredSpotters.Size = new System.Drawing.Size(93, 109);
            listBoxIgnoredSpotters.TabIndex = 11;
            listBoxIgnoredSpotters.Click += ListBox1_Click;
            listBoxIgnoredSpotters.SelectedIndexChanged += ListBoxIgnore_SelectedIndexChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(680, 3);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(48, 15);
            label3.TabIndex = 26;
            label3.Text = "Ignored";
            // 
            // comboBoxTimeIntervalAfter
            // 
            comboBoxTimeIntervalAfter.DataBindings.Add(new System.Windows.Forms.Binding("SelectedItem", form1BindingSource1, "TimeIntervalAfter", true));
            comboBoxTimeIntervalAfter.FormattingEnabled = true;
            comboBoxTimeIntervalAfter.Items.AddRange(new object[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            comboBoxTimeIntervalAfter.Location = new System.Drawing.Point(70, 104);
            comboBoxTimeIntervalAfter.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            comboBoxTimeIntervalAfter.Name = "comboBoxTimeIntervalAfter";
            comboBoxTimeIntervalAfter.Size = new System.Drawing.Size(44, 23);
            comboBoxTimeIntervalAfter.TabIndex = 10;
            comboBoxTimeIntervalAfter.SelectedIndexChanged += ComboBoxTimeForDump_SelectedIndexChanged;
            // 
            // form1BindingSource1
            // 
            form1BindingSource1.DataSource = typeof(Form1);
            // 
            // comboBoxTimeIntervalForDump
            // 
            comboBoxTimeIntervalForDump.DataBindings.Add(new System.Windows.Forms.Binding("SelectedItem", form1BindingSource1, "TimeIntervalForDump", true));
            comboBoxTimeIntervalForDump.FormattingEnabled = true;
            comboBoxTimeIntervalForDump.Items.AddRange(new object[] { "1", "15", "30", "60" });
            comboBoxTimeIntervalForDump.Location = new System.Drawing.Point(16, 104);
            comboBoxTimeIntervalForDump.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            comboBoxTimeIntervalForDump.Name = "comboBoxTimeIntervalForDump";
            comboBoxTimeIntervalForDump.Size = new System.Drawing.Size(46, 23);
            comboBoxTimeIntervalForDump.TabIndex = 27;
            comboBoxTimeIntervalForDump.SelectedIndexChanged += ComboBoxInterval_SelectedIndexChanged;
            // 
            // checkedListBoxReviewedSpotters
            // 
            checkedListBoxReviewedSpotters.CheckOnClick = true;
            checkedListBoxReviewedSpotters.FormattingEnabled = true;
            checkedListBoxReviewedSpotters.Location = new System.Drawing.Point(531, 21);
            checkedListBoxReviewedSpotters.Margin = new System.Windows.Forms.Padding(2);
            checkedListBoxReviewedSpotters.Name = "checkedListBoxReviewedSpotters";
            checkedListBoxReviewedSpotters.Size = new System.Drawing.Size(121, 94);
            checkedListBoxReviewedSpotters.TabIndex = 13;
            checkedListBoxReviewedSpotters.SelectedIndexChanged += CheckedListBoxReviewedSpotters_SelectedIndexChanged;
            checkedListBoxReviewedSpotters.MouseUp += CheckedListBoxReviewedSpotters_MouseUp;
            // 
            // form1BindingSource
            // 
            form1BindingSource.DataSource = typeof(Form1);
            // 
            // form1BindingSource2
            // 
            form1BindingSource2.DataSource = typeof(Form1);
            // 
            // checkBoxUSA
            // 
            checkBoxUSA.AutoSize = true;
            checkBoxUSA.DataBindings.Add(new System.Windows.Forms.Binding("Checked", form1BindingSource1, "USA", true));
            checkBoxUSA.Location = new System.Drawing.Point(346, 81);
            checkBoxUSA.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBoxUSA.Name = "checkBoxUSA";
            checkBoxUSA.Size = new System.Drawing.Size(48, 19);
            checkBoxUSA.TabIndex = 28;
            checkBoxUSA.Text = "USA";
            checkBoxUSA.UseVisualStyleBackColor = true;
            checkBoxUSA.CheckedChanged += CheckBoxUSA_CheckedChanged;
            // 
            // timer2
            // 
            timer2.Interval = 200;
            timer2.Tick += Timer2_Tick;
            // 
            // numericUpDownCwMinimum
            // 
            numericUpDownCwMinimum.Location = new System.Drawing.Point(121, 105);
            numericUpDownCwMinimum.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numericUpDownCwMinimum.Name = "numericUpDownCwMinimum";
            numericUpDownCwMinimum.Size = new System.Drawing.Size(38, 23);
            numericUpDownCwMinimum.TabIndex = 29;
            numericUpDownCwMinimum.ValueChanged += numericUpDownCwMinimum_ValueChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(770, 275);
            Controls.Add(numericUpDownCwMinimum);
            Controls.Add(checkBoxUSA);
            Controls.Add(comboBoxTimeIntervalForDump);
            Controls.Add(comboBoxTimeIntervalAfter);
            Controls.Add(label3);
            Controls.Add(listBoxIgnoredSpotters);
            Controls.Add(numericUpDownRTTYOffset);
            Controls.Add(labelClusterCache);
            Controls.Add(textBoxPassword);
            Controls.Add(labelQRZCache);
            Controls.Add(richTextBox1);
            Controls.Add(checkBoxFiltered);
            Controls.Add(checkBoxCached);
            Controls.Add(buttonCopy);
            Controls.Add(buttonBackup);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(checkedListBoxNewSpotters);
            Controls.Add(checkedListBoxReviewedSpotters);
            Controls.Add(textBoxPortLocal);
            Controls.Add(labelStatusQServer);
            Controls.Add(textBoxClusterServer);
            Controls.Add(labelQDepth);
            Controls.Add(textBoxCallsign);
            Controls.Add(buttonStart);
            DataBindings.Add(new System.Windows.Forms.Binding("Location", settings1, "Location", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "DxClusterUtil 241205 by W9MDB";
            Activated += Form1_Activated;
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ResizeEnd += Form1_ResizeEnd;
            Validated += Form1_Validated;
            ((System.ComponentModel.ISupportInitialize)numericUpDownRTTYOffset).EndInit();
            ((System.ComponentModel.ISupportInitialize)form1BindingSource1).EndInit();
            ((System.ComponentModel.ISupportInitialize)form1BindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)form1BindingSource2).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownCwMinimum).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
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
        private System.Windows.Forms.Button buttonCopy;
        private System.Windows.Forms.CheckBox checkBoxCached;
        private System.Windows.Forms.CheckBox checkBoxFiltered;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Label labelQRZCache;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label labelClusterCache;
        private System.Windows.Forms.NumericUpDown numericUpDownRTTYOffset;
        private System.Windows.Forms.ListBox listBoxIgnoredSpotters;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxTimeIntervalAfter;
        private System.Windows.Forms.ComboBox comboBoxTimeIntervalForDump;
        private System.Windows.Forms.CheckBox checkBoxUSA;
        private System.Windows.Forms.Timer timer2;
        public System.Windows.Forms.NumericUpDown numericUpDownCwMinimum;
    }
}

