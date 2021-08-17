namespace WinFormsSample
{
    partial class frmFIXAPISample
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
            this.lblHeartbeatMessage = new System.Windows.Forms.Label();
            this.btnDepthMarketDataRequest = new System.Windows.Forms.Button();
            this.btnNewOrderSingle = new System.Windows.Forms.Button();
            this.btnOrderStatusRequest = new System.Windows.Forms.Button();
            this.btnRequestForPositions = new System.Windows.Forms.Button();
            this.gbPriceStream = new System.Windows.Forms.GroupBox();
            this.btnSpotMarketData = new System.Windows.Forms.Button();
            this.gbTradeStream = new System.Windows.Forms.GroupBox();
            this.btnStopOrder = new System.Windows.Forms.Button();
            this.btnLimitOrder = new System.Windows.Forms.Button();
            this.btnSecurityListRequest = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.clearSentButton = new System.Windows.Forms.Button();
            this.txtMessageSend = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.clearReceivedButton = new System.Windows.Forms.Button();
            this.txtMessageReceived = new System.Windows.Forms.TextBox();
            this.gbPriceStream.SuspendLayout();
            this.gbTradeStream.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHeartbeatMessage
            // 
            this.lblHeartbeatMessage.AutoSize = true;
            this.lblHeartbeatMessage.Location = new System.Drawing.Point(14, 38);
            this.lblHeartbeatMessage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblHeartbeatMessage.Name = "lblHeartbeatMessage";
            this.lblHeartbeatMessage.Size = new System.Drawing.Size(0, 15);
            this.lblHeartbeatMessage.TabIndex = 3;
            // 
            // btnDepthMarketDataRequest
            // 
            this.btnDepthMarketDataRequest.Location = new System.Drawing.Point(8, 56);
            this.btnDepthMarketDataRequest.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnDepthMarketDataRequest.Name = "btnDepthMarketDataRequest";
            this.btnDepthMarketDataRequest.Size = new System.Drawing.Size(187, 27);
            this.btnDepthMarketDataRequest.TabIndex = 5;
            this.btnDepthMarketDataRequest.TabStop = false;
            this.btnDepthMarketDataRequest.Text = "Depth Market Data Request";
            this.btnDepthMarketDataRequest.UseVisualStyleBackColor = true;
            this.btnDepthMarketDataRequest.Click += new System.EventHandler(this.btnMarketDataRequest_Click);
            // 
            // btnNewOrderSingle
            // 
            this.btnNewOrderSingle.Location = new System.Drawing.Point(8, 22);
            this.btnNewOrderSingle.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnNewOrderSingle.Name = "btnNewOrderSingle";
            this.btnNewOrderSingle.Size = new System.Drawing.Size(187, 27);
            this.btnNewOrderSingle.TabIndex = 14;
            this.btnNewOrderSingle.Text = "New Market Order Single";
            this.btnNewOrderSingle.UseVisualStyleBackColor = true;
            this.btnNewOrderSingle.Click += new System.EventHandler(this.btnNewOrderSingle_Click);
            // 
            // btnOrderStatusRequest
            // 
            this.btnOrderStatusRequest.Location = new System.Drawing.Point(8, 123);
            this.btnOrderStatusRequest.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOrderStatusRequest.Name = "btnOrderStatusRequest";
            this.btnOrderStatusRequest.Size = new System.Drawing.Size(187, 27);
            this.btnOrderStatusRequest.TabIndex = 15;
            this.btnOrderStatusRequest.Text = "Order Status Request";
            this.btnOrderStatusRequest.UseVisualStyleBackColor = true;
            this.btnOrderStatusRequest.Click += new System.EventHandler(this.btnOrderStatusRequest_Click);
            // 
            // btnRequestForPositions
            // 
            this.btnRequestForPositions.Location = new System.Drawing.Point(8, 156);
            this.btnRequestForPositions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnRequestForPositions.Name = "btnRequestForPositions";
            this.btnRequestForPositions.Size = new System.Drawing.Size(187, 27);
            this.btnRequestForPositions.TabIndex = 16;
            this.btnRequestForPositions.Text = "Request for Positions";
            this.btnRequestForPositions.UseVisualStyleBackColor = true;
            this.btnRequestForPositions.Click += new System.EventHandler(this.btnRequestForPositions_Click);
            // 
            // gbPriceStream
            // 
            this.gbPriceStream.Controls.Add(this.btnSpotMarketData);
            this.gbPriceStream.Controls.Add(this.btnDepthMarketDataRequest);
            this.gbPriceStream.Location = new System.Drawing.Point(18, 16);
            this.gbPriceStream.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gbPriceStream.Name = "gbPriceStream";
            this.gbPriceStream.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gbPriceStream.Size = new System.Drawing.Size(211, 296);
            this.gbPriceStream.TabIndex = 17;
            this.gbPriceStream.TabStop = false;
            this.gbPriceStream.Text = "Price Stream";
            // 
            // btnSpotMarketData
            // 
            this.btnSpotMarketData.Location = new System.Drawing.Point(8, 22);
            this.btnSpotMarketData.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSpotMarketData.Name = "btnSpotMarketData";
            this.btnSpotMarketData.Size = new System.Drawing.Size(187, 27);
            this.btnSpotMarketData.TabIndex = 14;
            this.btnSpotMarketData.Text = "Spot Market Data Request";
            this.btnSpotMarketData.UseVisualStyleBackColor = true;
            this.btnSpotMarketData.Click += new System.EventHandler(this.btnSpotMarketData_Click);
            // 
            // gbTradeStream
            // 
            this.gbTradeStream.Controls.Add(this.btnStopOrder);
            this.gbTradeStream.Controls.Add(this.btnLimitOrder);
            this.gbTradeStream.Controls.Add(this.btnSecurityListRequest);
            this.gbTradeStream.Controls.Add(this.btnRequestForPositions);
            this.gbTradeStream.Controls.Add(this.btnOrderStatusRequest);
            this.gbTradeStream.Controls.Add(this.btnNewOrderSingle);
            this.gbTradeStream.Location = new System.Drawing.Point(236, 16);
            this.gbTradeStream.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gbTradeStream.Name = "gbTradeStream";
            this.gbTradeStream.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gbTradeStream.Size = new System.Drawing.Size(205, 296);
            this.gbTradeStream.TabIndex = 18;
            this.gbTradeStream.TabStop = false;
            this.gbTradeStream.Text = "Trade Stream";
            // 
            // btnStopOrder
            // 
            this.btnStopOrder.Location = new System.Drawing.Point(8, 89);
            this.btnStopOrder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnStopOrder.Name = "btnStopOrder";
            this.btnStopOrder.Size = new System.Drawing.Size(187, 27);
            this.btnStopOrder.TabIndex = 19;
            this.btnStopOrder.Text = "New Stop Order Single";
            this.btnStopOrder.UseVisualStyleBackColor = true;
            this.btnStopOrder.Click += new System.EventHandler(this.btnStopOrder_Click);
            // 
            // btnLimitOrder
            // 
            this.btnLimitOrder.Location = new System.Drawing.Point(8, 56);
            this.btnLimitOrder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnLimitOrder.Name = "btnLimitOrder";
            this.btnLimitOrder.Size = new System.Drawing.Size(187, 27);
            this.btnLimitOrder.TabIndex = 18;
            this.btnLimitOrder.Text = "New Limit Order Single";
            this.btnLimitOrder.UseVisualStyleBackColor = true;
            this.btnLimitOrder.Click += new System.EventHandler(this.btnLimitOrder_Click);
            // 
            // btnSecurityListRequest
            // 
            this.btnSecurityListRequest.Location = new System.Drawing.Point(8, 190);
            this.btnSecurityListRequest.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSecurityListRequest.Name = "btnSecurityListRequest";
            this.btnSecurityListRequest.Size = new System.Drawing.Size(187, 27);
            this.btnSecurityListRequest.TabIndex = 14;
            this.btnSecurityListRequest.Text = "Security List Request";
            this.btnSecurityListRequest.UseVisualStyleBackColor = true;
            this.btnSecurityListRequest.Click += new System.EventHandler(this.btnSecurityListRequest_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.clearSentButton);
            this.groupBox1.Controls.Add(this.txtMessageSend);
            this.groupBox1.Location = new System.Drawing.Point(449, 16);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Size = new System.Drawing.Size(205, 296);
            this.groupBox1.TabIndex = 19;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "FIX Message Sent";
            // 
            // clearSentButton
            // 
            this.clearSentButton.Location = new System.Drawing.Point(8, 262);
            this.clearSentButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.clearSentButton.Name = "clearSentButton";
            this.clearSentButton.Size = new System.Drawing.Size(187, 27);
            this.clearSentButton.TabIndex = 15;
            this.clearSentButton.Text = "Clear";
            this.clearSentButton.UseVisualStyleBackColor = true;
            this.clearSentButton.Click += new System.EventHandler(this.clearSentButton_Click);
            // 
            // txtMessageSend
            // 
            this.txtMessageSend.Location = new System.Drawing.Point(8, 22);
            this.txtMessageSend.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtMessageSend.Multiline = true;
            this.txtMessageSend.Name = "txtMessageSend";
            this.txtMessageSend.ReadOnly = true;
            this.txtMessageSend.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessageSend.Size = new System.Drawing.Size(189, 234);
            this.txtMessageSend.TabIndex = 7;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.clearReceivedButton);
            this.groupBox2.Controls.Add(this.txtMessageReceived);
            this.groupBox2.Location = new System.Drawing.Point(662, 16);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox2.Size = new System.Drawing.Size(205, 296);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "FIX Message Received";
            // 
            // clearReceivedButton
            // 
            this.clearReceivedButton.Location = new System.Drawing.Point(8, 262);
            this.clearReceivedButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.clearReceivedButton.Name = "clearReceivedButton";
            this.clearReceivedButton.Size = new System.Drawing.Size(187, 26);
            this.clearReceivedButton.TabIndex = 16;
            this.clearReceivedButton.Text = "Clear";
            this.clearReceivedButton.UseVisualStyleBackColor = true;
            this.clearReceivedButton.Click += new System.EventHandler(this.clearReceivedButton_Click);
            // 
            // txtMessageReceived
            // 
            this.txtMessageReceived.Location = new System.Drawing.Point(8, 22);
            this.txtMessageReceived.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtMessageReceived.Multiline = true;
            this.txtMessageReceived.Name = "txtMessageReceived";
            this.txtMessageReceived.ReadOnly = true;
            this.txtMessageReceived.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessageReceived.Size = new System.Drawing.Size(189, 234);
            this.txtMessageReceived.TabIndex = 9;
            // 
            // frmFIXAPISample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 320);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.gbTradeStream);
            this.Controls.Add(this.gbPriceStream);
            this.Controls.Add(this.lblHeartbeatMessage);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "frmFIXAPISample";
            this.ShowIcon = false;
            this.Text = "FIX API Sample";
            this.gbPriceStream.ResumeLayout(false);
            this.gbTradeStream.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblHeartbeatMessage;
        private System.Windows.Forms.Button btnDepthMarketDataRequest;
        private System.Windows.Forms.Button btnNewOrderSingle;
        private System.Windows.Forms.Button btnOrderStatusRequest;
        private System.Windows.Forms.Button btnRequestForPositions;
        private System.Windows.Forms.GroupBox gbPriceStream;
        private System.Windows.Forms.GroupBox gbTradeStream;
        private System.Windows.Forms.Button btnSecurityListRequest;
        private System.Windows.Forms.Button btnSpotMarketData;
        private System.Windows.Forms.Button btnStopOrder;
        private System.Windows.Forms.Button btnLimitOrder;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtMessageSend;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtMessageReceived;
        private System.Windows.Forms.Button clearSentButton;
        private System.Windows.Forms.Button clearReceivedButton;
    }
}

