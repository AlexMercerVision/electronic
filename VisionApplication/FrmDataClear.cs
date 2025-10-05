using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CommonCls;

namespace xbDefectDect
{
    public partial class FrmDataClear : Form
    {
        int idNode;
        int iNodeType;
        string strCmd="";
        string strDataChoose ;
        string strDataChoose1;
        int iDataType;
        DateTime dtB;
        DateTime dtE;
        public FrmDataClear()
        {
            InitializeComponent();
        }
        public FrmDataClear(TreeNode tn)
            :this()
        {
            SetNode(tn);
        }
        public void SetNode(TreeNode tn)
        {
          //  this.tn = tn;
            txtNode.Text = tn.Text;
            idNode = Int32.Parse(tn.Name);
            iNodeType = tn.ImageIndex + 1;
            strDataChoose = "where IDNode= " + idNode.ToString();
            strDataChoose1 = "where IDNode= " + idNode.ToString();
        }
        private void btOk_Click(object sender, EventArgs e)
        {
            //加强条件检查
            if (!chkRawData.Checked && !chkSpectureAmp.Checked && !chkSpecturePhase.Checked && !chkTrendData.Checked)
            {
                MessageBox.Show("请选择删除数据类型!");
                return;
            }

            //n天以前
            if (rdBtDaysAgo.Checked)
            {
                if (txtDays.Text=="")
                {
                    MessageBox.Show("请输入正确的天数!");
                    return;
                }
                dtB = DateTime.Now.AddDays(-1 * Int32.Parse(txtDays.Text));
                strDataChoose1 += " and DateT<'" + dtB + "'";
                strDataChoose += " and MeasDate<'" + dtB + "'";

            }
            //日期以前
            else if (rdBtDateAgo.Checked)
            {
                dtB = dtpDate.Value;
                strDataChoose += " and DateT<'" + dtB + "'";
                strDataChoose += " and MeasDate<'" + dtB + "'";
            }
            //日期之间
            else if (rdBtDateBetween.Checked)
            {
                dtB = dtpB.Value;
                dtE = dtpE.Value;
                strDataChoose += " and DateT>'" + dtB + "' and DateT<'" + dtE + "'";
                strDataChoose += " and MeasDate>'" + dtB + "' and MeasDate<'" + dtE + "'";
            }
            else if (rdBtEveryDays.Checked)
            {
                if (txtEveryDays.Text == "")
                {
                    MessageBox.Show("请输入正确的天数!");
                    return;
                }
            }
            else if (rdBtAlarm.Checked)
            {
            }
            else if (rdBtWarming.Checked)
            {

            }

            int affectNum = 0;

            //删除原始数据
            if (chkRawData.Checked)
            {
                iDataType = 2;
                strCmd = "delete from MeasurementBinaryRaw where IDMeasurement in (select IDMeasurement from Measurement " + strDataChoose + ") and DataType =" + iDataType;
                affectNum += (new SqlOper()).ExecuteSQLNonQuery(strCmd);
            }
            //删除幅值谱
            if (chkSpectureAmp.Checked)
            {
                iDataType = 0;
                strCmd = "delete from MeasurementBinaryRaw where IDMeasurement in (select IDMeasurement from Measurement " + strDataChoose + ") and DataType =" + iDataType;
                affectNum += (new SqlOper()).ExecuteSQLNonQuery(strCmd);
            }
            //删除相位谱
            if (chkSpecturePhase.Checked)
            {
                iDataType = 1;
                strCmd = "delete from MeasurementBinaryRaw where IDMeasurement in (select IDMeasurement from Measurement " + strDataChoose + ") and DataType =" + iDataType;
                affectNum += (new SqlOper()).ExecuteSQLNonQuery(strCmd);
            }
            //删除趋势数据
            else if (chkTrendData.Checked)
            {
                if (iNodeType == 6)
                {
                    strCmd = "delete from SingleTrend " + strDataChoose;
                }
                else if (iNodeType == 7)
                {
                    strCmd = "delete from TrendValue "+strDataChoose1;
                }
                affectNum += (new SqlOper()).ExecuteSQLNonQuery(strCmd);
            }
           
           
            string strMsg = "共删除" + affectNum.ToString() + "条记录";
            MessageBox.Show(strMsg);
            //插入日志

            strDataChoose = "where IDNode= " + idNode.ToString();
            strDataChoose1 = "where IDNode= " + idNode.ToString();
                strCmd="";
        }

        private void btCancle_Click(object sender, EventArgs e)
        {
            
        }

        private void FrmDataClear_Load(object sender, EventArgs e)
        {

        }
    }
}