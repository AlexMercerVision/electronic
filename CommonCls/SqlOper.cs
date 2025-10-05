using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Collections;
//using SQLDMO;
//using System.
//using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

namespace CommonCls
{

    //用户信息
    public struct struUserInfo
    {
        public string strID;
        public string strUserName;
        public string strPwd;
        public string strTel;
        public string strEamil;
        public string strAccess;
        public int iRole;
    }

    //查询事件/数据间隔
    public class AskSaveInt
    {
        public int iSerAskInt;      //服务器定时查询时间间隔
        public int iSerSaveInt;     //服务器端定时保存时间间隔
        public int iClientAskInt;   //客户端实时浏览查询间隔

    }
    public struct ClientStatus
    {
        public string strName;
        public int iMainNode;
        public int iSubNode;
        public string strLoginIP;
        public DateTime LoginDT;
        public string strLogoutIP;
        public DateTime LogoutDT;
        public bool bOnline;
        public bool bUser;
    }
    public struct NodeInfo   //测点信息
    {
        public int iParNode;
        public int iNode;
        public int indexLine;
        public string strLineName;
        public string strLineDesc;

        public string strIP;
        public string strComputerName;
        public int NodeStatus;
        public CUserParam cup;
        public byte[] sceneImg;
    }

    public struct DataInfoS
    {
        public int DataInfo_STATU;
        public int DataInfo_NM;
        public float DataInfo_DIS;      //2020/04/25 由double型改为float型
        public short[] DataInfo_DATA;
        public DataInfoS DClone()
        {
            DataInfoS disC = new DataInfoS();
            disC.DataInfo_STATU = this.DataInfo_STATU;
            disC.DataInfo_NM = this.DataInfo_NM;
            disC.DataInfo_DIS = this.DataInfo_DIS;
            disC.DataInfo_DATA = (short[])this.DataInfo_DATA.Clone();
            return disC;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    
    //树状列表测点信息
    struct TreeViewNodeType
    {
       public int IDNode;
        public int IDParent;
        public short NodeType;
        public string NodeName;
        public string NodeDescription;
        public int NodeStatus;
        //public short Channel;
        //public string ChannelName;
    } 

   

 

    public class SqlOper
    {
        string strIP = "127.0.0.1";
        string strConn = "Data source=(local);User id=sa;Password=st13594630257; Initial Catalog=master;database=xbDB1";
        string strConnection;// = MainFrm.strConnection;
     //   int iBit = 8;// MainFrm.iBit;
        private SqlConnection Conn;
        private SqlCommand Comm; 
        public string UserName;
       
        //附加数据库函数
        public void AttachDB(string strFileName1, string strFileName2, string strSqlDBName)
        {
            try
            {
                Conn = new SqlConnection(strConnection);
                Conn.Open();
                Comm = new SqlCommand();
                Comm.Connection = Conn;
                Comm.CommandText = "sp_attach_db";

                Comm.Parameters.Add(new SqlParameter(@"dbname", SqlDbType.NVarChar));
                Comm.Parameters[@"dbname"].Value = strSqlDBName;
                Comm.Parameters.Add(new SqlParameter(@"filename1", SqlDbType.NVarChar));
                Comm.Parameters[@"filename1"].Value = strFileName1;
                Comm.Parameters.Add(new SqlParameter(@"filename2", SqlDbType.NVarChar));
                Comm.Parameters[@"filename2"].Value = strFileName2;

                Comm.CommandType = CommandType.StoredProcedure;
                Comm.ExecuteNonQuery();

                MessageBox.Show("数据库操作成功", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                Conn.Close();
            }
        }
         
        //分离数据库
        public bool DetachDB(string strSqlDBName)
        {
            bool bSuccess = false;
            try
            {
                //ConnectionString = "Data Source="+strIP+";User id=sa;Password=skf; Initial Catalog=master";
                Conn = new SqlConnection(strConnection);
                Conn.Open();

                Comm = new SqlCommand();
                Comm.Connection = Conn;
                Comm.CommandText = @"sp_detach_db";

                Comm.Parameters.Add(new SqlParameter(@"dbname", SqlDbType.NVarChar));

                Comm.Parameters[@"dbname"].Value = strSqlDBName;

                Comm.CommandType = CommandType.StoredProcedure;
                Comm.ExecuteNonQuery();

                MessageBox.Show("分离数据库成功", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                bSuccess = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                Conn.Close();

            }
            if (bSuccess)
                return true;
            else
                return false;
        }

        //备份数据库
        public void BackupDataBase(string strSqlDBName, string strDBBackupPath)
        {
            try
            {
                // ConnectionString = "Data Source="+strIP+";User id=sa;Password=skf; Initial Catalog=master";

                Conn = new SqlConnection(strConnection);
                Conn.Open();

                Comm = new SqlCommand();
                Comm.Connection = Conn;
                Comm.CommandText = "use master;backup database @dbname to disk = @backupname;";

                Comm.Parameters.Add(new SqlParameter(@"dbname", SqlDbType.NVarChar));
                Comm.Parameters[@"dbname"].Value = strSqlDBName;
                Comm.Parameters.Add(new SqlParameter(@"backupname", SqlDbType.NVarChar));
                Comm.Parameters[@"backupname"].Value = strDBBackupPath;

                Comm.CommandType = CommandType.Text;
                Comm.ExecuteNonQuery();

                MessageBox.Show("备份数据库成功", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                Conn.Close();
            }
        }

        //替换数据库
        public void ReplaceDataBase(string strSqlDBName, string strBackupPath)
        {
            try
            {
                // ConnectionString = "Data Source="+strIP+";User id=sa;Password=skf; Initial Catalog=master";
                //string BackupFile = @DataBaseOfBackupPath + @DataBaseOfBackupName;
                Conn = new SqlConnection(strConnection);
                Conn.Open();

                Comm = new SqlCommand();
                Comm.Connection = Conn;
                Comm.CommandText = "use master;restore database @DataBaseName From disk = @BackupFile with replace;";

                Comm.Parameters.Add(new SqlParameter(@"DataBaseName", SqlDbType.NVarChar));
                Comm.Parameters[@"DataBaseName"].Value = strSqlDBName;
                Comm.Parameters.Add(new SqlParameter(@"BackupFile", SqlDbType.NVarChar));
                Comm.Parameters[@"BackupFile"].Value = strBackupPath;

                Comm.CommandType = CommandType.Text;
                Comm.ExecuteNonQuery();

                MessageBox.Show("还原数据库成功", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                Conn.Close();
            }
        }


        //查找局域网中存在的数据库
        public ArrayList AnoGetMSSQLDBNameList(string DSN)
        {
            ArrayList DBNameList = new ArrayList();
            SqlConnection Connection = new SqlConnection(String.Format("Server={0}; integrated security =sspi", DSN));
            DataTable DBNameTable = new DataTable();
            SqlDataAdapter Adapter = new SqlDataAdapter("select name from master..sysdatabases", Connection);
            Adapter.Fill(DBNameTable);

            int i = 0;
            foreach (DataRow Row in DBNameTable.Rows)
            {
                DBNameList.Add(Row.ItemArray[0]);

                i++;
            }

            return DBNameList;
        }
        //查找局域网中存在的数据库
        public ArrayList GetMSSQLDBNameList(string DSN, string UID, string PWD)
        {
            // “DataSource=dsName; AttatchDBFilename=”D:\FuancnTest.mdf”;Initial Catalog=aa_LogicName;User ID=id;Password=skfpw”；

            //SQLDMO.ApplicationClass sqlApp = new SQLDMO.ApplicationClass();
            //SQLDMO.NameList nameList;
            //int i = 0;

            //nameList = sqlApp.ListAvailableSQLServers();

            //for (i = 0; i < nameList.Count; i++)
            //{
            //    MessageBox.Show(nameList.Item(i).ToString());
            //}   


            //SqlDataSourceEnumerator instance = System.Data.Sql.SqlDataSourceEnumerator.Instance; DataTable dt = instance.GetDataSources();
            //DataView dv = new DataView(dt);
            //string strSql=dv[0].ToString();

            ArrayList DBNameList = new ArrayList();
            SqlConnection Connection = new SqlConnection(String.Format("Data Source={0};Initial Catalog=master;User ID={1};PWD={2}", DSN, UID, PWD));
            DataTable DBNameTable = new DataTable();
            SqlDataAdapter Adapter = new SqlDataAdapter("select name from master..sysdatabases", Connection);
            try
            {
                Adapter.Fill(DBNameTable);

                int i = 0;
                foreach (DataRow Row in DBNameTable.Rows)
                {
                    DBNameList.Add(Row.ItemArray[0]);

                    i++;
                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + "无法连接数据库");
                return null;
            }


            return DBNameList;
        }

        //
        public byte[] Object2Bytes(object obj)
        {
            int size = Marshal.SizeOf(obj);
            byte[] bytes = new byte[size];
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, structPtr, false);
            Marshal.Copy(structPtr, bytes, 0, size);
            Marshal.FreeHGlobal(structPtr);
            return bytes;
        }
        //添加操作日志
        public void InsertOperLog(int IDNode,DateTime dt, string strUserName, int OperateType, string strEvent)
        {
            SqlConnection myConn = new SqlConnection(strConn);
          //  DateTime dt = DateTime.Now;
            string strCmd;
            strCmd = "insert into operLog values(" + IDNode + ",'" + dt + "','" + strEvent + "'," + OperateType + ",'" + strUserName + "')";

            SqlCommand myCmd = new SqlCommand(strCmd, myConn);

            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return;
            }
            myConn.Close();
            return;
        }

        public void ReadOperLog(int IDNode, int TopNum, OperLog[] operLog)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select top "+TopNum+" operTime,operEvent,operCode,operUser from operLog where indexLine="+IDNode+" order by operTime Desc ";
            SqlCommand myCmd;
            SqlDataReader myReader;
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
                int ind = 0;
                while (myReader.Read())
                {
                    operLog[ind].dtOperTime= myReader.GetDateTime(0);
                    operLog[ind].strOperEvent = myReader.GetString(1);
                    operLog[ind].iEventStatus = myReader.GetInt32(2);
                    operLog[ind].strUserName = myReader.GetString(3);
                }
                myReader.Close();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
       
        //存入原始图片

        public void searchTime(int IDNode,ref DateTime dt,bool bForward)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select Top 1 measTime from sourceGroupImg where indexLine=" + IDNode + " and measTime>'" + dt + "' order by measTime asc ";
           if(!bForward)
                strCmd = "select Top 1 measTime from sourceGroupImg where indexLine=" + IDNode + " and measTime<'" + dt + "' order by measTime Desc ";

            SqlCommand myCmd;
            SqlDataReader myReader;
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();

                if (myReader.Read())
                {
                    dt = (DateTime)myReader[0];
                }
                myReader.Close();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
       
        public void InsertSourceImg(int IDNode,int surFaceIndex, DateTime dt,byte[][] bImage)
        {
            /*int num = objSourceImg.Length;
            byte[][] bImage = new byte[num][];
            for (int i=0;i< num; i++)
            {
                bImage[i] = Object2Bytes(objSourceImg[i]);
            }*/
            string strCmd = "insert into sourceGroupImg values(" + IDNode + "," + surFaceIndex + ",'" + dt + "',@Img1,@Img2,@Img3,@Img4,@Img5,@ImgFX)";

            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@Img1", SqlDbType.VarBinary);
            myCmd.Parameters["@Img1"].Value = bImage[0];
            myCmd.Parameters.Add("@Img2", SqlDbType.VarBinary);
            myCmd.Parameters["@Img2"].Value = bImage[1];
            myCmd.Parameters.Add("@Img3", SqlDbType.VarBinary);
            myCmd.Parameters["@Img3"].Value = bImage[2];
            myCmd.Parameters.Add("@Img4", SqlDbType.VarBinary);
            myCmd.Parameters["@Img4"].Value = bImage[3];
            myCmd.Parameters.Add("@Img5", SqlDbType.VarBinary);
            myCmd.Parameters["@Img5"].Value = bImage[4];
            myCmd.Parameters.Add("@ImgFX", SqlDbType.VarBinary);
            myCmd.Parameters["@ImgFX"].Value = bImage[5];
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }

        public void ReadSourceImg(int IDNode,  DateTime dt, byte[][] objSourceImg)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select AcqImg1,AcqImg2,AcqImg3,AcqImg4,AcqImg5 from sourceGroupImg where indexLine=" + IDNode + " and measTime='"+ dt+"'";
            SqlCommand myCmd;
            SqlDataReader myReader;
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
             
            //    while (myReader.Read())
                {
                    objSourceImg[0] = (byte[])myReader[0];
                    objSourceImg[1] = (byte[])myReader[1];
                    objSourceImg[2] = (byte[])myReader[2];
                    objSourceImg[3] = (byte[])myReader[3];
                    objSourceImg[4] = (byte[])myReader[4];
                    objSourceImg[5] = (byte[])myReader[5];
                }
                myReader.Close();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        public void ReadSourceImg(int IDNode,  DateTime dt,int ind, byte[] objSourceImg)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select AcqImg1,AcqImg2,AcqImg3,AcqImg4,AcqImg5 from sourceGroupImg where indexLine=" + IDNode  + " and measTime='" + dt + "'";
            SqlCommand myCmd;
            SqlDataReader myReader;
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
               
                if (myReader.Read())
                {
                    objSourceImg = (byte[])myReader[ind];                   
                }
                myReader.Close();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
      
        public void DelSourceImg(int id)
        {
            SqlConnection myConn = new SqlConnection(strConn);           
            string strCmd;
            strCmd = "delete from sourceGroupImg where ID= "+id;
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return;
            }
            myConn.Close();
            return;
        }

        public void DelSourceImg(DateTime dt)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd;
            strCmd = "delete from sourceGroupImg where measTime= " + dt;
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return;
            }
            myConn.Close();
            return;
        }

        public void DelSourceImg(DateTime dtB, DateTime dtE)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd;
            strCmd = "delete from sourceGroupImg where measTime between '" + dtB+ "' and '"+dtE+"'";
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return;
            }
            myConn.Close();
            return;
        }
        
        public void DelDetRes(int id)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd;
            strCmd = "delete from detRes where sourceID= " + id;
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return;
            }
            myConn.Close();
            return;
        }
        
        //存入检测结果
        public void InsertDetRes(int sourceID, string strCode, int manuMark, Object[] objSourceImg)
        {
            int num = objSourceImg.Length;
            byte[][] bImage = new byte[num][];
            for (int i = 0; i < num; i++)
            {
                bImage[i] = Object2Bytes(objSourceImg[i]);
            }
            string strCmd = "insert into detRes values(" + sourceID + "," + strCode + "," + manuMark + ",@Img1,@Img2,@Img3,@Img4,@Img5)";

            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@Img1", SqlDbType.VarBinary);
            myCmd.Parameters["@Img1"].Value = bImage[0];
            myCmd.Parameters.Add("@Img2", SqlDbType.VarBinary);
            myCmd.Parameters["@Img2"].Value = bImage[1];
            myCmd.Parameters.Add("@Img3", SqlDbType.VarBinary);
            myCmd.Parameters["@Img3"].Value = bImage[2];
            myCmd.Parameters.Add("@Img4", SqlDbType.VarBinary);
            myCmd.Parameters["@Img4"].Value = bImage[3];
            myCmd.Parameters.Add("@Img5", SqlDbType.VarBinary);
            myCmd.Parameters["@Img5"].Value = bImage[4];          
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        
        
        //更新检测结果
        public void UpdateDetRes(int sourceID, string strCode, int manuMark, Object[] objSourceImg)
        {
            int num = objSourceImg.Length;
            byte[][] bImage = new byte[num][];
            for (int i = 0; i < num; i++)
            {
                bImage[i] = Object2Bytes(objSourceImg[i]);
            }
            string strCmd = "update detRes set detCode='"+strCode+"',manuMark="+manuMark+ ",resImg1=@Img1,resImg2=@Img2,resImg3=@Img3,resImg4=@Img4,resImg5=@Img5 where sourceID=" + sourceID;
              
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@Img1", SqlDbType.VarBinary);
            myCmd.Parameters["@Img1"].Value = bImage[0];
            myCmd.Parameters.Add("@Img2", SqlDbType.VarBinary);
            myCmd.Parameters["@Img2"].Value = bImage[1];
            myCmd.Parameters.Add("@Img3", SqlDbType.VarBinary);
            myCmd.Parameters["@Img3"].Value = bImage[2];
            myCmd.Parameters.Add("@Img4", SqlDbType.VarBinary);
            myCmd.Parameters["@Img4"].Value = bImage[3];
            myCmd.Parameters.Add("@Img5", SqlDbType.VarBinary);
            myCmd.Parameters["@Img5"].Value = bImage[4];
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
       
        //存入参数
        public void InsertGroupParam(int iParIndex,int nodeIndex,string strLineName,string strLineDesc)
        {
            string strCmd = "insert into nodeParam(IDParent,NodeIndex,nameLine,descripLine) values(" +
                iParIndex + "," + nodeIndex  + ",'" + strLineName + "','" + strLineDesc + "')";
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn); 
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        public void UpdateGroupParam(int iParIndex, int iNode, string strLineName, string strLineDesc)
        {
            string strCmd = "Update nodeParam set nameLine='" + strLineName + "',descripLine='" + strLineDesc +
                "' where NodeIndex=" + iNode+ " and IDParent="+ iParIndex;
          
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
           
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }


        public void InsertNewLineParam(NodeInfo ni )
        {
            string strCmd = "insert into nodeParam values(" + ni.iParNode + "," + ni.iNode + "," + ni.indexLine + ",'" + ni.strLineName + "','" + ni.strLineDesc +"','" + ni.strIP + "','" + ni.strComputerName + "',0,@ImgScene)";
           
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@ImgScene", SqlDbType.VarBinary);
            myCmd.Parameters["@ImgScene"].Value = ni.sceneImg; 
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }

        public void UpdateLineParam(NodeInfo ni)
        {
            string strCmd = "Update nodeParam set indexLine="+ni.indexLine+", nameLine='" + ni.strLineName + "',descripLine='" + ni.strLineDesc + "',IPLine='" + ni.strIP + "',ComputerName='" + ni.strComputerName + 
                "',sceneImg=@scene where NodeIndex="+ni.iNode + " and IDParent=" + ni.iParNode;
            byte[] bScene;
            
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@scene", SqlDbType.VarBinary);
            myCmd.Parameters["@scene"].Value = ni.sceneImg; 
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        public NodeInfo ReadLineInfo(int iMainNode, int iSubNode,ref string strGroupName,ref string strGroupDesc)
        {
            NodeInfo ni = new NodeInfo();
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select * from nodeParam where IDParent=" + iMainNode + " and NodeIndex=" + iSubNode;

            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                SqlDataReader myReader = myCmd.ExecuteReader();
                if (myReader.Read())
                {
                    ni.indexLine = myReader.GetInt32(3);
                    ni.strLineName = myReader.GetString(4).Trim();
                    ni.strLineDesc = myReader.GetString(5).Trim();
                    ni.strIP= myReader.GetString(6).Trim();
                    ni.strComputerName = myReader.GetString(7).Trim();
                    ni.sceneImg=(byte[])myReader[9]; 
                    myReader.Close();
                }
                strCmd = "select * from nodeParam where IDParent=0 and NodeIndex=" + iSubNode;
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
                if (myReader.Read())
                {
                    strGroupName = myReader.GetString(4);
                    strGroupDesc = myReader.GetString(5); 
                    myReader.Close();
                }
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
            return ni;
        }

        public void InsertNodeParam(int IDNode, string strName, string strDesc,string strIP,string strComputerName, Object scene)
        {
            string strCmd = "insert into nodeParam values(0," + IDNode + "," + strName + "," + strDesc + strIP + "," + strComputerName + ",@ImgScene)";

            byte[] bScene;
            if (scene == null)
                bScene = new byte[0];
            else
                bScene = Object2Bytes(scene);
     //       byte[] bImage= Object2Bytes(userParam);
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@scene", SqlDbType.VarBinary);
            myCmd.Parameters["@scene"].Value = bScene;
        //    myCmd.Parameters.Add("@Img1", SqlDbType.VarBinary);
        //    myCmd.Parameters["@Img1"].Value = bImage;
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        //更新参数
        public void UpdateNodeParam(int IDNode, string strName, string strDesc, string strIP, string strComputerName, Object scene, Object userParam)
        {
            string strCmd = "Update nodeParam set nameLine='" +strName+"',descripLine='"+strDesc+"',IPLine='"+strIP+"',ComputerName='"+ strComputerName + "',sceneImg=@scene,userParam=@userParam where indexLine=IDNode";
             
         //   byte[] bImage = Object2Bytes(userParam);
            byte[] bScene = Object2Bytes(scene);
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@scene", SqlDbType.VarBinary);
            myCmd.Parameters["@scene"].Value = bScene;
         //   myCmd.Parameters.Add("@Img1", SqlDbType.VarBinary);
          //  myCmd.Parameters["@Img1"].Value = bImage;

            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }

        public void ReadParNodeInfo(int iNode, ref string strName, ref string strDesc)
        {
            NodeInfo ni = new NodeInfo();
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select nameLine,descripLine from  nodeParam where IDParent=0 and NodeIndex=" + iNode;

            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                SqlDataReader myReader = myCmd.ExecuteReader();

                if (myReader.Read())
                {
                    strName = myReader.GetString(0);
                    strDesc = myReader.GetString(1);
                    myReader.Close();
                }
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();

        }

        //实时状态
        public void InsertRuntimeParam(int IDNode,  DateTime dt, Object objSourceImg)
        {
          
            byte[] bImage = Object2Bytes(objSourceImg);
           
            string strCmd = "insert into runtimeParam values(" + IDNode + ",'" + dt + "',@Img1)";

            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@Img1", SqlDbType.VarBinary);
            myCmd.Parameters["@Img1"].Value = bImage[0];
            
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }

        public void UpdateRuntimeParam(int IDNode, DateTime dt, Object objSourceImg)
        {

            byte[] bImage = Object2Bytes(objSourceImg);

            string strCmd = "update runtimeParam set updateTime= '" + dt + "',latestParam=" + objSourceImg + " where indexLine=" + IDNode; ;
            //    "values(" + IDNode + "," + dt + ",@Img1)";

            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            myCmd.Parameters.Add("@Img1", SqlDbType.VarBinary);
            myCmd.Parameters["@Img1"].Value = bImage[0];

            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }


        //查询新增节点编号
        public int GetMaxNodeIndex(int iMainNode)
        {
            string strCmd = "select top 1 NodeIndex from nodeParam where IDParent=" + iMainNode + " Order by NodeIndex Desc";
            try
            {
                return (Int32)ExecuteSQLScalar(strCmd);
            }
            catch
            {
                return 0;
            }
        }


        /// <summary>
        /// ///////////////////////////////////////
        /// </summary>
        /// <param name="strLineName"></param>
        public void DelTreeNode(string strLineName)
        {
           // SqlConnection myConn = new SqlConnection(strConn);
            int iNodeName = Int32.Parse(strLineName) ;
            int iParNode = iNodeName / 100;
            int iNodeType= iNodeName % 100;
            string strCmd="";
           
      //      SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
              //  myConn.Open();
                if (iParNode != 0)
                {
                    strCmd = "delete from TreeNode where IDParent=" + iParNode + " and NodeType= " + iNodeType;
                    
                }                   
                else
                {
                    strCmd = "delete from TreeNode where IDParent=" + iNodeType+ "or NodeType="+iNodeType;
                   // myCmd.ExecuteNonQuery();
                }

                ExecuteSQLNonQuery(strCmd);

            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
           
            //  myConn.Close();
        }
        
 
       public string ReadTreeNodeName(int iMainNode, int iSubNode)
        {
            string strName = "";
            string strCmd = "select nodename from TreeNode where IDParent="+iMainNode+ " and NodeType= "+ iSubNode;

            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                SqlDataReader myReader = myCmd.ExecuteReader();
                if (myReader.Read())
                    strName = myReader.GetString(0).Trim();

            }
            catch
            {

            }
            return strName;
        }

      
       
         
        //treeview2的相关操作——————————————————————————————————————————————————
        //加载管理视图
        public void LoadTreeViewManag(TreeView tv)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select * from nodeParam";
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                SqlDataReader myReader = myCmd.ExecuteReader();
                int i = 0;
                int iParNode = 0;
                tv.Nodes[0].Name = "0";
                tv.Nodes[0].StateImageIndex = 1;
                while (myReader.Read())
                {
                    //添加一级Nodev
                    int nNodeNum;
                    i++;
                    iParNode= myReader.GetInt32(1);
                    TreeNode tn = new TreeNode();
                    if (iParNode==0)
                    {                         
                        tn.Name = myReader[2].ToString().Trim();
                        tn.Text= myReader[4].ToString().Trim();
                        tn.ToolTipText= myReader[5].ToString().Trim();
                        tn.StateImageIndex = 1;
                        tn.SelectedImageIndex = tn.ImageIndex;
                        tv.Nodes[0].Nodes.Add(tn);
                    }
                    else
                    {
                        int iSubType= myReader.GetInt32(2);
                        int iName = iParNode * 100 + iSubType;
                        tn.Name = iName.ToString();
                        tn.Text = myReader[4].ToString().Trim();
                        tn.ToolTipText = myReader[5].ToString().Trim();
                        tn.ImageIndex = 2;
                        tn.SelectedImageIndex = tn.ImageIndex;
                        tn.StateImageIndex = 1;
                        tv.Nodes[0].Nodes[iParNode.ToString()].Nodes.Add(tn);
                    }
                      
                }
                myReader.Close();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }

        public void LoadTreeViewHierarch(TreeView tv)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            try
            {
                myConn.Open();
                //打开node表，读入记录
                ArrayList al = new ArrayList();
                ArrayList subal = new ArrayList();
                string strCommand = "select distinct  NodeIndex,IDParent，indexLine ,nameLine,descripLine,IPLine,ComputerName from nodeParam order by NodeIndex";
                string strCommand1 = "select nameLine,descripLine from  nodeParam where IDParent=0";
                SqlCommand myCmd = new SqlCommand(strCommand1, myConn);
                SqlDataReader myReader = myCmd.ExecuteReader();
                TreeViewNodeType tvnt;
                tvnt = new TreeViewNodeType();

                //将所有主类型存放在ArrayList al中
                while (myReader.Read())
                {
                    tvnt.IDNode = myReader.GetInt32(0);
                    tvnt.NodeName = myReader["nameLine"].ToString().Trim();
                    tvnt.NodeDescription = myReader["descripLine"].ToString().Trim();
                    al.Add(tvnt);
                }
                myReader.Close();

                //将所有子类型存放在ArrayList subal中
                myCmd = new SqlCommand(strCommand, myConn);
                myReader = myCmd.ExecuteReader();
                while (myReader.Read())
                {
                    int MainType = myReader.GetInt32(1);
                    tvnt.IDNode = myReader.GetInt32(0) + MainType * 100;
                    tvnt.NodeName = myReader["nameLine"].ToString().Trim();
                    tvnt.NodeDescription = myReader["descripLine"].ToString().Trim();
                    subal.Add(tvnt);
                }
                myReader.Close();

                //每条记录对应一个树节点，并根据IDParent确定父节点
                TreeNode[] tvs = new TreeNode[al.Count];
                tv.Nodes[0].Name = "0";
                tv.Nodes[0].StateImageIndex = 1;
                for (int i = 0; i < al.Count; i++)
                {

                    tvnt = (TreeViewNodeType)al[i];

                    tvs[i] = new TreeNode();
                    tvs[i].Text = tvnt.NodeName;
                    tvs[i].Name = tvnt.IDNode.ToString();
                    tvs[i].ToolTipText = tvnt.NodeDescription;

                    tvs[i].ImageIndex = 6;
                  //  tvs[i].SelectedImageIndex = 6;
                    tvs[i].StateImageIndex = 1;
                    //一级节点                  
                    tv.Nodes[0].Nodes.Add(tvs[i]);
                    //子节点

                }
                for (int i = 0; i < subal.Count; i++)
                {
                    TreeNode tn = new TreeNode();
                    tvnt = (TreeViewNodeType)subal[i];

                    tn.Text = tvnt.NodeName;
                    tn.Name = tvnt.IDNode.ToString();
                    tn.ToolTipText = tvnt.NodeDescription;

                    tn.ImageIndex = 6;
                 //   tn.SelectedImageIndex = 6;
                    tn.StateImageIndex = 1;

                    int IDParent = tvnt.IDNode / 100;
                    for (int j = 0; j < al.Count; j++)
                    {
                        if (tvs[j].Name == IDParent.ToString())
                        {
                            tvs[j].Nodes.Add(tn);
                            break;
                        }
                    }
                }

                tv.ExpandAll();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        
        //删除树节点
        public void DeleteTreeViewNode(string NodeName)
        { 
            int IDNode = Int32.Parse(NodeName);
            int iMainIndex = IDNode / 100;
            int iSubIndex = IDNode % 100;
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd;
            if(iMainIndex==0)  //组节点
            {
                strCmd = "delete from nodeParam where NodeIndex='" + iSubIndex + "' and IDParent=" + iMainIndex + " or  IDParent="+ iSubIndex;
            }
            else
            {
                strCmd = "delete from nodeParam where NodeIndex='" + iSubIndex + "' and IDParent=" + iMainIndex;
            }
            //查找IDNode  or IDParent=NodeName的节点将其删除，及删除节点及其子节点
            SqlCommand myCmd = new SqlCommand(strCmd, myConn);
            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();

            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();

        }

 
        //在树状列表中添加前面插入的节点
        public void AddTreeViewNode(TreeNode tn)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd = new SqlCommand("select IDNode,NodeType,NodeName,NodeDescription,NodeStatus from TreeNode where IDNode=(select max(IDNode) from TreeNode) ", myConn);

            try
            {
                myConn.Open();
                SqlDataReader myReader = myCmd.ExecuteReader();
                myReader.Read();
                tn.Name = myReader["IDNode"].ToString();
                tn.Text = myReader["NodeName"].ToString();
                tn.ToolTipText = myReader["NodeDescription"].ToString();
                tn.ImageIndex = Int16.Parse(myReader["NodeType"].ToString());
                tn.StateImageIndex = Int32.Parse(myReader["NodeStatus"].ToString());
                myReader.Close();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();

        }

     //软件启动后先检查还未进行分析的数据，分析并保存，同时保存趋势数据




        //插入操作日志
        //OperateType 1: 用户登录 2：用户注销
        //            3:参数设置   4：浏览分析  5：故障诊断

        public void InsertSystemLog(int IDNode, string strUserName, int OperateType, string Description)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            DateTime dt = DateTime.Now;
            string strCmd;
            strCmd = "insert into SystemLog values(" + IDNode + ",'" + dt + "','" + strUserName + "'," + OperateType + ",'" + Description + "')";

            SqlCommand myCmd = new SqlCommand(strCmd, myConn);

            try
            {
                myConn.Open();
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return;
            }
            myConn.Close();
            return;
        } 
        //删除历史数据
        public void DeleteHistoryData(int idMeasurement)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            SqlCommand myCmd;
            try
            {
                myConn.Open();
                string strCmd = "delete from OriData where ID=" + idMeasurement;
                myCmd = new SqlCommand(strCmd, myConn);
                myCmd.ExecuteNonQuery();

                strCmd = "delete from EventLog where MeasID=" + idMeasurement;
                myCmd = new SqlCommand(strCmd, myConn);
                myCmd.ExecuteNonQuery();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return;
            }
            myConn.Close();
            return;
        }

       
        //读取父节点类型
        public int ReadNodeFType(string strNodeFName)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select NodeFID  from VibSet  where NodeFName='" + strNodeFName + "'";
            SqlCommand myCmd;
            SqlDataReader myReader;
            int iMainType = -1;
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
                if (myReader.Read())
                {
                    // myReader.IsDBNull()
                    iMainType = myReader.GetInt32(0);
                }
                myReader.Close();

            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();

            if (iMainType == -1)
            {
                strCmd = "select max(NodeFID)  from VibSet ";
                iMainType = (int)ExecuteSQLScalar(strCmd) + 1;
            }

            return iMainType;
        }


        //读取子节点类型
        public int ReadNodeCType(int iNodeFID, string strNodeCName)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select NodeCID  from VibSet  where NodeFID=" + iNodeFID + " and NodeCName='" + strNodeCName + "'";
            SqlCommand myCmd;
            SqlDataReader myReader;
            int iSubType = -1;

            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
                if (myReader.Read())
                {
                    iSubType = myReader.GetInt32(0);
                }
                myReader.Close();

            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();

            if (iSubType == -1)
            {
                strCmd = "select max(NodeCID)  from VibSet  where NodeFID=" + iNodeFID;
                iSubType = (int)ExecuteSQLScalar(strCmd) + 1;
            }

            return iSubType;
        }

 
        //用户登录
        public string Login(string strAccount, string strPwd,ref int iRole)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select * from UserAccount where UserID='" + strAccount + "' and UserPwd='" + strPwd + "'";
            SqlDataReader myReader;
            SqlCommand myCmd;
            string strUserName = "";
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
                if (myReader.Read())
                {
                    strUserName = myReader[2].ToString();
                    iRole=myReader.GetInt32(5);
                }
                myReader.Close();

            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
                myConn.Close();
                return "";
            }
            myConn.Close();
            return strUserName;
        }

         
        //查找系统日志
        public void ShowSystemLog(ref SqlDataAdapter myAdapter, ref DataTable dt, ref DataView dv)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            try
            {
                myAdapter = new SqlDataAdapter("select '' as 测点, OperateDate as 时间,OperateUser as 用户 , Description as 描述, OperateType ,SystemLog.IDNode from SystemLog where IDNode=0", myConn);
                SqlCommandBuilder myBuilder = new SqlCommandBuilder(myAdapter);
                dt = new DataTable();
                myAdapter.Fill(dt);
                myAdapter = new SqlDataAdapter("select NodeName as 测点, OperateDate as 时间,OperateUser as 用户 , Description as 描述, OperateType ,SystemLog.IDNode from SystemLog inner join TreeNode on(SystemLog.IDNode=TreeNode.IDNode)", myConn);
                myBuilder = new SqlCommandBuilder(myAdapter);
                myAdapter.Fill(dt);
                
                dv = new DataView(dt);               
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }

        //查找一个数据库的所有用户表
        public bool SearchTable(string strDatabase)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select name from " + strDatabase + "..SysObjects where xtype='u'";
            SqlCommand myCmd;
            SqlDataReader myReader;
            string strBDName;
            bool bPass = false;
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
                while (myReader.Read())
                {
                    strBDName = myReader[0].ToString();
                    if (strBDName == "Authentication")
                    {
                        bPass = true;
                        break;
                    }
                }
                myReader.Close();
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
            return bPass;
        }
 
        //图片资料库操作*****************************************************************************

  

        //用户表操作---------------------------------------
        //读取用户信息
        public void ReadWholeUserInfo(ArrayList al)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "select * from UserAccount ";
            SqlCommand myCmd;
            SqlDataReader myReader;
            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myReader = myCmd.ExecuteReader();
                while (myReader.Read())
                {
                    struUserInfo stru_UserInfo;
                    stru_UserInfo.strID = myReader["UserID"].ToString().Trim();
                    stru_UserInfo.strUserName = myReader["UserName"].ToString().Trim();
                    stru_UserInfo.strPwd = myReader["UserPwd"].ToString().Trim();
                    stru_UserInfo.strTel = myReader["Phone"].ToString().Trim();
                    stru_UserInfo.strEamil = myReader["Email"].ToString().Trim();
                    stru_UserInfo.strAccess = myReader["UserAccess"].ToString().Trim();
                    stru_UserInfo.iRole = Int32.Parse(myReader["Role"].ToString());
                    al.Add(stru_UserInfo);
                }
                myReader.Close();
            }
            catch (System.Exception SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        public void DeleteUserInfo(string UserID)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd = "delete from UserAccount where UserID='" + UserID + "'";
            SqlCommand myCmd;

            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myCmd.ExecuteNonQuery();
            }
            catch (System.Exception SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }

        public void AddorEditUserInfo(struUserInfo ui, bool bAddorEdit)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            string strCmd;
            if (bAddorEdit)
            {
                strCmd = "insert into UserAccount(UserID,UserName,UserPwd,Phone,Email,UserAccess,Role) values('" +
                    ui.strID + "','" + ui.strUserName + "','" + ui.strPwd + "','" + ui.strTel + "','" +
                    ui.strEamil + "','" + ui.strAccess + "','" + ui.iRole + "')";
            }
            else
            {
                strCmd = "update UserAccount set UserName='" + ui.strUserName +
                    "',UserPwd='" + ui.strPwd + "',Phone='" + ui.strTel + "',Email='" + ui.strEamil +
                    "',UserAccess='" + ui.strAccess + "',Role= '" + (short)ui.iRole + "' where UserID='" + ui.strID + "'";
            }
            SqlCommand myCmd;

            try
            {
                myConn.Open();
                myCmd = new SqlCommand(strCmd, myConn);
                myCmd.ExecuteNonQuery();
            }
            catch (System.Exception SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
        //用户管理操作*********************************************************************************

       
        /// <summary> 2020/04/25
        /// 更改了原数据表格式，字段设计，保存原始short格式的数据，TestPar结构体以字节流形式整体保存
        /// </summary>
        /// <param name="di"></param>
        /// <param name="iMainNodeType"></param>
        /// <param name="iSubNodeType"></param>
        /// <param name="tp"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
      
   
          
        //-----------------执行 添加/删除/修改 记录操作--------------------------------
        public int ExecuteSQLNonQuery(string cmdText)
        {
             if (cmdText=="")
             {
                 return 0;
             }
            int affectedCount=-1;
            SqlConnection myConn = new SqlConnection(strConn);
            try
            {
                myConn.Open();
                SqlCommand cmdSQL = new SqlCommand();
                cmdSQL.Connection = myConn;                
                cmdSQL.CommandType = CommandType.Text;                
                cmdSQL.CommandText = cmdText;

                //执行 添加/删除/修改 记录操作
                affectedCount = cmdSQL.ExecuteNonQuery();

                //MessageBox.Show("本次操作影响的总行数为： " + affectedCount.ToString(), "WindObserver系统消息", MessageBoxButtons.OK, MessageBoxIcon.Information);


            }
            catch (SqlException err)
            {
                MessageBox.Show(err.Message, "WindObserver系统消息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                myConn.Close();
            }
            myConn.Close();
          //  myConn = null;
            return affectedCount;

        }
        //-----------------执行 添加/删除/修改 记录操作--------------------------------

        //-----------------执行 记录数、最大值、最小值等操作--------------------------------
        public object ExecuteSQLScalar(string cmdText)
        {
            if (cmdText == "")
            {
                return 0;
            }
            object affectedCount = -1;
            SqlConnection myConn = new SqlConnection(strConn);
            try
            {
                myConn.Open();
                SqlCommand cmdSQL = new SqlCommand();
                cmdSQL.Connection = myConn;
                cmdSQL.CommandType = CommandType.Text;
                cmdSQL.CommandText = cmdText;
               
                affectedCount = cmdSQL.ExecuteScalar();



            }
            catch (SqlException err)
            {
                MessageBox.Show(err.Message, "WindObserver系统消息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                myConn.Close();
            }
            myConn.Close();
         //   myConn = null;
            int a = 0;
            if (affectedCount==null || !int.TryParse(affectedCount.ToString(),out a))
            {
                affectedCount =(Int32)0;
            }
            return affectedCount;

        }
        //-----------------执行记录数、最大值、最小值等操作--------------------------------


        //软件启动后先检查还未进行分析的数据，分析并保存，同时保存趋势数据

        //显示事件日志

        public void ShowEventInfo(ref SqlDataAdapter myAdapter, ref DataTable dt, ref DataView dv, int iMainType, int iSubType)
        {
            SqlConnection myConn = new SqlConnection(strConn);
            try
            {
                myAdapter = new SqlDataAdapter("select NodeName as 路段名, EventDate as 时间, Event_STATU as 状态," +
                    "Event_TP as 事件类型,Event_PN as 事件位置, Event_AC as 事件损耗, Event_EL as 事件反射衰减损耗, Event_ER as 累积损耗," +
                    "Acknowledged as 告知, MeasID from V_EventLog where IDNodeMainType=" + iMainType + " and IDNodeSubType=" + iSubType + " order by EventDate Desc", myConn);
                SqlCommandBuilder myBuilder = new SqlCommandBuilder(myAdapter);

                dt = new DataTable();
                myAdapter.Fill(dt);
                dv = new DataView(dt);
            }
            catch (SqlException SqlLexc)
            {
                MessageBox.Show("错误：" + SqlLexc);
            }
            myConn.Close();
        }
       
          
    }    
}
 