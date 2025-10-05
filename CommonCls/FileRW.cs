using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Collections;
namespace CommonCls
{
    //数据头文件相关信息
    public struct DataHeadInfo
    {
        public int iFileIndex;             //文件序号  从零开始
        public int iSerial;                //测点序号   从零开始
        public int iLen;                   //数据长度
        public float fFreq;                //采样频率
        public string strUnit;             //数据单位
        public string strHeadInfo;          //文件头信息
        public DateTime dt;                 //采样时间
        public int ChnnlID;
        public int SessionID;
        public int SampleMode;
        public float fScale;
        public float fK;
        public float fB;
        public float fFilter;
        public string strFileName;         //数据文件名称
    }

    /**
     * BinaryWriter写入的是数据的内存模型，打开文件看到的是该数字对应的ASCII码
     * StreamWriter写入的是数据的每个字符对应的ASCII码数据，打开文件看到的是该数字的每个字符(明文)
     * */
    public class FileRW
    {
         //将数据库数据导出为文本数据
        public void SaveDataToTxt(string strFileName, float[] fData)
        {           
            StreamWriter sw = new StreamWriter(strFileName, false);
            for (int i = 0; i < fData.Length;i++ )
            {              
                sw.WriteLine(fData[i]);
            }
            sw.Close();
            
        }
        //将数据库数据导出为文本数据
        public void SaveDataToTxt(string strFileName, double[] fData)
        {
            StreamWriter sw = new StreamWriter(strFileName, false);
            for (int i = 0; i < fData.Length; i++)
            {
                sw.WriteLine(fData[i]);
            }
            sw.Close();

        }
        //testResut
        public void ResultOutput(string strResutFileName,string strResult)
        {
            StreamWriter sw = new StreamWriter(strResutFileName, true);
            sw.WriteLine(strResult);
            sw.Close();
        }

        //调试输出
        public void DebugOutput(string strMsg)
        {
            StreamWriter sw = new StreamWriter("debug.txt",true);
            sw.WriteLine(strMsg);            
            sw.Close();
        }
        //保存数据文件
        public void SaveDataToFile(string strFileName, float[] fData )
        {
            FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write);
            for (int i = 0; i < fData.Length; i++)
            {
                byte[] bData = new byte[4];
                bData = BitConverter.GetBytes(fData[i]);
                fs.Write(bData, 0, bData.Length);
            }
            fs.Close();
        }

        //  [12/14/2011 Administrator]
        //保存查询时间段日期
        public bool ReadDateBuf(ref DateTime dtB, ref DateTime dtE)
        {
            string strFileName = AppDomain.CurrentDomain.BaseDirectory + "dateInfo.inf";
            StreamReader sr = new StreamReader(strFileName);
            string strDate;
            bool bOk = false;
            try
            {
                if ((strDate = sr.ReadLine().Trim()) != null)
                {
                    dtB = DateTime.Parse(strDate);
                    dtE = DateTime.Parse(sr.ReadLine().Trim());
                    bOk = true;
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                sr.Close();
                return false;
            }
            return bOk;
        }
        //  [12/14/2011 Administrator]
        //保存查询时间段日期
        public void SaveDateBuf(DateTime dtB, DateTime dtE)
        {
            string strFileName = AppDomain.CurrentDomain.BaseDirectory + "dateInfo.inf";
            StreamWriter sw = new StreamWriter(strFileName, false);
            try
            {
                sw.WriteLine(dtB.ToString());
                sw.WriteLine(dtE.ToString());
                sw.Close();
            }
            catch (Exception ex)
            {
                sw.Close();
                return;
            }
        }
        //保存数据选择时间文件
        public void SaveDateToFile(string strDate,string strTimeB,string strTimeE)
        {
            string strFileName = "dateInfo.inf";
            FileStream fs = new FileStream(strFileName,FileMode.Create, FileAccess.Write);
             

            byte[] bDate = new byte[19];
            bDate = Encoding.ASCII.GetBytes(strDate);
            int len1 = bDate.Length;
            bDate = Encoding.ASCII.GetBytes(strTimeB);
            int len2 = bDate.Length;
            bDate = Encoding.ASCII.GetBytes(strTimeE);
            int len3 = bDate.Length;
            
            bDate = Encoding.ASCII.GetBytes(len1.ToString());
            fs.Write(bDate, 0, bDate.Length);
            bDate = Encoding.ASCII.GetBytes(len2.ToString());
            fs.Write(bDate, 0, bDate.Length);
            bDate = Encoding.ASCII.GetBytes(len3.ToString());
            fs.Write(bDate, 0, bDate.Length);

            bDate = Encoding.ASCII.GetBytes(strDate);
            fs.Write(bDate, 0, bDate.Length);

            bDate = Encoding.ASCII.GetBytes(strTimeB);
            fs.Write(bDate, 0, bDate.Length);
            
            bDate = Encoding.ASCII.GetBytes(strTimeE);
            fs.Write(bDate, 0, bDate.Length);
            fs.Close();
        }

        //读取数据选择时间文件
        public void ReadDateFromFile(ref string strDate,ref string strTimeB, ref string strTimeE)
        {
            string strFileName = "dateInfo1.inf";
            FileStream fs = new FileStream(strFileName, FileMode.Open,FileAccess.Read);
            byte[] bDataLen = new byte[2];
            fs.Read(bDataLen, 0, 2);
            strDate = Encoding.ASCII.GetString(bDataLen);
            int len1 = Int32.Parse(strDate);

            bDataLen = new byte[2];
            fs.Read(bDataLen, 0, 2);
            strDate = Encoding.ASCII.GetString(bDataLen);
            int len2 = Int32.Parse(strDate);

            bDataLen = new byte[2];
            fs.Read(bDataLen, 0, 2);
            strDate = Encoding.ASCII.GetString(bDataLen);
            int len3 = Int32.Parse(strDate);

            byte[] bDate = new byte[len1];
            fs.Read(bDate, 0, len1);
            strDate = Encoding.ASCII.GetString(bDate);

            bDate = new byte[len2];
            fs.Read(bDate, 0, len2);
            strTimeB = Encoding.ASCII.GetString(bDate);

            bDate = new byte[len3];
            fs.Read(bDate, 0, len3);
            strTimeE = Encoding.ASCII.GetString(bDate);
             
            fs.Close();
        }

        //保存数据选择时间文件
        public void SaveDateToFile1(string strDate, string strTimeB, string strTimeE)
        {
            string strFileName = "dateInfo1.inf";
            FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write);


            byte[] bDate = new byte[19];
            bDate = Encoding.ASCII.GetBytes(strDate);
            int len1 = bDate.Length;
            bDate = Encoding.ASCII.GetBytes(strTimeB);
            int len2 = bDate.Length;
            bDate = Encoding.ASCII.GetBytes(strTimeE);
            int len3 = bDate.Length;

            bDate = Encoding.ASCII.GetBytes(len1.ToString());
            fs.Write(bDate, 0, bDate.Length);
            bDate = Encoding.ASCII.GetBytes(len2.ToString());
            fs.Write(bDate, 0, bDate.Length);
            bDate = Encoding.ASCII.GetBytes(len3.ToString());
            fs.Write(bDate, 0, bDate.Length);

            bDate = Encoding.ASCII.GetBytes(strDate);
            fs.Write(bDate, 0, bDate.Length);

            bDate = Encoding.ASCII.GetBytes(strTimeB);
            fs.Write(bDate, 0, bDate.Length);

            bDate = Encoding.ASCII.GetBytes(strTimeE);
            fs.Write(bDate, 0, bDate.Length);
            fs.Close();
        }

        //读取数据选择时间文件
        public void ReadDateFromFile1(ref string strDate, ref string strTimeB, ref string strTimeE)
        {
            string strFileName = "dateInfo1.inf";
            FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
            byte[] bDataLen = new byte[2];
            fs.Read(bDataLen, 0, 2);
            strDate = Encoding.ASCII.GetString(bDataLen);
            int len1 = Int32.Parse(strDate);

            bDataLen = new byte[2];
            fs.Read(bDataLen, 0, 2);
            strDate = Encoding.ASCII.GetString(bDataLen);
            int len2 = Int32.Parse(strDate);

            bDataLen = new byte[2];
            fs.Read(bDataLen, 0, 2);
            strDate = Encoding.ASCII.GetString(bDataLen);
            int len3 = Int32.Parse(strDate);

            byte[] bDate = new byte[len1];
            fs.Read(bDate, 0, len1);
            strDate = Encoding.ASCII.GetString(bDate);

            bDate = new byte[len2];
            fs.Read(bDate, 0, len2);
            strTimeB = Encoding.ASCII.GetString(bDate);

            bDate = new byte[len3];
            fs.Read(bDate, 0, len3);
            strTimeE = Encoding.ASCII.GetString(bDate);

            fs.Close();
        }

        //读取源数据文件
        public int ReadData(string strFileName, ref float[] data)
        {
            strFileName = "F:\\c#\\WindConData\\" + strFileName;
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return 0;
            }
            
            FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);

            byte[] bytData = new byte[4];
            for (int i = 0; i < data.Length; i++)
            {
                fs.Read(bytData, 0, bytData.Length);
                data[i] = BitConverter.ToSingle(bytData, 0);
            }
            fs.Close();
            return 1;
        }

        //读取数据文件
        public bool ReadSourcData(string strFileName, ref double[] data, int iColoumnIndex, int iStart,int iTotalLen)
        {
            strFileName = strFileName.Substring(0, strFileName.Length - 3) + "dat";      
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return false;
            }
            
            if (data==null)
            {
                data = new double[iTotalLen];
            }
            int iLen = data.Length;
            FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            br.ReadBytes(((iColoumnIndex-1) * iTotalLen+iStart) * 4);
            for (int i = 0; i < iLen; i++)
            {
                data[i] = br.ReadSingle();
            }
            br.Close();
            fs.Close();
            return true;
        }

         
        //读取数据头文件
        public bool ReadHeadData(string strFileName,int index,ref int iChnlCount,ref float fFreq,ref int iLen)
        {
            strFileName = strFileName.Substring(0, strFileName.Length - 3) + "set";      
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return false;
            }
            FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs,Encoding.GetEncoding("gb2312"));
            iChnlCount=br.ReadInt32();
            if (index>=iChnlCount )
            {
                index = 0;
            }
            string strInfo;
            for (int i=0;i<iChnlCount;i++)
            {
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                fFreq = br.ReadSingle();
                iLen = br.ReadInt32();
                br.ReadInt32();
                strInfo=br.ReadString();
                strInfo = br.ReadString();
                strInfo = br.ReadString();
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
                if (i==index)
                {
                    break;
                }
            }
            
            br.Close();
            fs.Close();
            return true;
        }
         //读取数据头文件
        public bool ReadDataFileInfo(string[] strFiles,ref ArrayList al)
        {
             string strFileName;
             al=new ArrayList();
             DataHeadInfo dhi=new DataHeadInfo();
             for (int i = 0; i < strFiles.Length;i++ )
             {
                 strFileName = strFiles[i].Substring(0, strFiles[i].Length - 3) + "set";
                 if (!File.Exists(strFileName))
                 {
                     //MessageBox.Show("文件不存在");
                     return false;
                 }

                 FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
                 BinaryReader br = new BinaryReader(fs, Encoding.GetEncoding("gb2312"));
                 dhi.iFileIndex = i;                            //文件序号
                 int iIndex=strFileName.LastIndexOf(@"\");
                 dhi.strFileName = strFileName.Substring(iIndex + 1, strFileName.Length - iIndex - 5);      //文件名
                 int iChnlCount = br.ReadInt32();
                
                 string strInfo;
                 for (int j = 0; j < iChnlCount; j++)
                 {
                     dhi.iSerial = j;
                     br.ReadInt32();
                     br.ReadInt32();
                     br.ReadInt32();
                     dhi.fFreq = br.ReadSingle();
                     dhi.iLen = br.ReadInt32();
                     br.ReadInt32();
                     dhi.strHeadInfo= br.ReadString();
                     dhi.strUnit = br.ReadString();
                     strInfo = br.ReadString();
                     br.ReadSingle();
                     br.ReadSingle();
                     br.ReadSingle();
                     br.ReadSingle();

                     al.Add(dhi);
                 }

                 br.Close();
                 fs.Close();

             }
             /*if (dhis == null || dhis.Length!=al.Count)
              {
                  dhis = new DataHeadInfo[al.Count];
              }
            // dhis = (DataHeadInfo[])al.ToArray();
             al.CopyTo(dhis);*/
             /*for (int k = 0; k < al.Count;k++ )
             {
                 dhis[k] = (DataHeadInfo)al[k];
                 
             }*/

             return true;
            
        }
        
        //读取振动数据
        public void ReadVibData(string strFileName,int iLen,int iOffset, ref double[] data)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int i = 0;
            int j = 0;
            while ((strData = sr.ReadLine()) != null && i<iLen)
            {
                j++;
                if (j<=iOffset)
                {
                    continue;
                }
                data[i] = double.Parse(strData);
                i++;
            }
            sr.Close();
            // FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);


        }

        //读取振动数据
        public void ReadVibData(string strFileName, ref double[] data)
        {
             
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return ;
            }
            string strData;
            StreamReader sr=new StreamReader(strFileName);
            int i = 0;
            while ((strData=sr.ReadLine())!=null)
            {
                data[i] = double.Parse(strData);
                i++;
            }
            sr.Close();
           // FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);

             
        }

        //读取振动数据
        public void ReadVibData(string strFileName, int iLen, int iOffset, ref float[] data)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int i = 0;
            int j = 0;
            while ((strData = sr.ReadLine()) != null && i < iLen)
            {
                j++;
                if (j <= iOffset)
                {
                    continue;
                }
                data[i] = float.Parse(strData);
                i++;
            }
            sr.Close();
            // FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);


        }
        //读取振动数据
        public void ReadVibData(string strFileName, ref float[] data)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int i = 0;
            while ((strData = sr.ReadLine()) != null)
            {
                data[i] = float.Parse(strData);
                i++;
            }
            sr.Close();
            // FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);


        }

        //读取故障诊断参数
        public void ReadDiagProp(string strFileName, ref int nk,ref int nkk,ref int NK,ref int method,ref int diagType,ref int mainFaultType)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
                      
            if ((strData = sr.ReadLine()) != null)
            {
                nk=Int32.Parse(strData);
            }
            if ((strData = sr.ReadLine()) != null)
            {
                nkk=Int32.Parse(strData);
            }
            if ((strData = sr.ReadLine()) != null)
            {
                NK=Int32.Parse(strData);
            }
            if ((strData = sr.ReadLine()) != null)
            {
                method = Int32.Parse(strData);
            }
            if ((strData = sr.ReadLine()) != null)
            {
                diagType = Int32.Parse(strData);
            }
            if ((strData = sr.ReadLine()) != null)
            {
                mainFaultType = Int32.Parse(strData);
            }
            sr.Close();            
        }

        //保存故障诊断参数
        public void SaveDiagProp(string strFileName, int nk, int nkk, int NK,int method,int diagType,int  mainFaultType)
        {
            FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write);

            StreamWriter sw=new StreamWriter(fs,System.Text.Encoding.GetEncoding("utf-8")); 
             
            //string strProp;
            sw.WriteLine(nk.ToString());
            sw.WriteLine(nkk.ToString());
            sw.WriteLine(NK.ToString());
            sw.WriteLine(method.ToString());
            sw.WriteLine(diagType.ToString());
            sw.WriteLine(mainFaultType.ToString());
            sw.Close();
            fs.Close();
        }

        //
        public int ReadMeasurement()
        {
            string strFileName = "IDMeasurement.txt";
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return 0;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int  nIDMeasurement=0;
            if ((strData = sr.ReadLine()) != null)
            {
                nIDMeasurement = Int32.Parse(strData);
            }
            sr.Close(); 
            return nIDMeasurement;
        }

        public void SaveMeasurement(int IDMeasurement)
        {
            try
            {
                StreamWriter sw = new StreamWriter("IDMeasurement.txt", false);
                sw.WriteLine(IDMeasurement.ToString());
                sw.Close();

            }
            catch(Exception ex)
            {
                return;
            }         

        }

        //保存和读取文件夹路径
        public void SavePath(string strPath)
        {
            try
            {
                StreamWriter sw = new StreamWriter(System.Environment.CurrentDirectory + "\\DataPath.txt",false);

                sw.WriteLine(strPath.Trim());
                sw.Close();

            }
            catch (Exception ex)
            {
                return;
            }
        }
        public bool ReadPath(ref string strPath)
        {
            try
            {
                StreamReader sr = new StreamReader(System.Environment.CurrentDirectory + "\\DataPath.txt", true);
                strPath=sr.ReadLine().Trim();
                sr.Close();

            }
            catch (Exception ex)
            {
                return false;
            }
            if (strPath!="")
            {
                return true;
            }
            else 
                return false;
        }

        public void SaveRet(int iRet)
        {
            try
            {
                StreamWriter sw = new StreamWriter("ret.txt", true);

                sw.WriteLine(iRet.ToString()); 
                sw.Close();

            }
            catch (Exception ex)
            {
                return;
            }
        }
 
        //读取文本数据
        public void ReadMultiLineData(string strFileName, ref double[] data1, int index,int iStart)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int i = 0,j=0;
           
            while ((strData = sr.ReadLine()) != null)
            {
                if (j < iStart)
                {
                   j++;
                    continue;
                }
                string[] b = strData.Split();
                while (b[index]=="")
                {
                    index++;
                    if (index>=b.Length)
                    {
                        break;
                    }
                }
                data1[i] = double.Parse(b[index]);              
                i++;
                if (i >= data1.Length)
                    break;
            }
            sr.Close();
        }
        //读取文本信息
        public bool  ReadDataInfo(string strFileName, ref int len,ref  int columnNum)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return false;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int i = 0;
            columnNum = -1;
            while ((strData = sr.ReadLine()) != null)
            {
                if (columnNum == -1)
                {
                    string[] b = strData.Split();
                    int DimNum = b.GetLength(0);
                    columnNum = DimNum;
                    for (int j = 0; j < DimNum; j++)
                    {
                        if (b[j] == "")
                        {
                            columnNum--;
                        }
                        
                    }
                }
                if (columnNum<1)
                {
                    return false;
                }
                i++;              
            }
            sr.Close();
            len = i;
            return true;
        }

        //读取文本数据
        public void ReadMultiLineData(string strFileName, ref float[] data1,ref float[] data2)
        {
             
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int i = 0;
            while ((strData = sr.ReadLine()) != null)
            {
                string[] b = strData.Split();
                data1[i] = float.Parse(b[1]);
                data2[i] = float.Parse(b[2]);
                i++;
                if (i >= data1.Length)
                    break;
            }
            sr.Close(); 


        }

        //读取文本数据(多列)
        public void ReadMultiLineData1(string strFileName, ref float[] data1)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int i = 0;
            while ((strData = sr.ReadLine()) != null)
            {
                string[] b = strData.Split();
                data1[i] = float.Parse(b[1]);               
                i++;
                if (i >= data1.Length)
                    break;
            }
            sr.Close();


        }
        //读取文本数据(多列)
        public void ReadMultiLineData1(string strFileName, ref double[,] data1)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int m = data1.GetLength(0);
            int n = data1.GetLength(1);
           
            int c = 0, r = 0;
            while ((strData = sr.ReadLine()) != null)
            {
                string[] b = strData.Split();
                for (int j = 0; j < b.Length; j++)
                {
                    if (b[j] != "")
                    {
                        if (data1[c, r] != double.Parse(b[j]))
                            data1[c, r] = double.Parse(b[j]);
                        r++;
                        if (r >= n)
                        {
                            c++;
                            r = 0;
                        }
                    }
                }
                
            }
             sr.Close();

        }

        //读取文本数据(多列)
        public void ReadMultiLineData1(string strFileName, int iDim,ref double[,,] data1)
        {

            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);
            int m = data1.GetLength(1);
            int n = data1.GetLength(2);

            int c = 0, r = 0;
            while ((strData = sr.ReadLine()) != null)
            {
                string[] b = strData.Split();
                for (int j = 0; j < b.Length; j++)
                {
                    if (b[j] != "")
                    {
                        if (data1[iDim, c, r] != double.Parse(b[j]))
                            data1[iDim,c,r] = double.Parse(b[j]);
                        r++;
                        if (r >= n)
                        {
                            c++;
                            r = 0;
                        }
                    }
                }

            }
            sr.Close();

        }

        //获取文件夹下所有文件名
        public void GetFileName(int iType,string dirPath,ref string[] strFileName)
        {
            ArrayList list = new ArrayList();
            ArrayList listPrim = new ArrayList();
            if (Directory.Exists(dirPath))
            {
                list.AddRange(Directory.GetFiles(dirPath));
            }
            int num = list.Count;
            int i;
            string strFNOld="";
            for (i = 0; i < num; i++)
            {
                string strFN = (string)list[i];
                if (strFN.Substring(strFN.Length - 3, 3) == "set")
                {
                    switch (iType)
                    {
                        case 0:    //所有数据
                            listPrim.Add(strFN);
                            break;
                        case 1:    //每个时间点取一组数据
                            if (Int32.Parse(strFN.Substring(strFN.Length - 5, 1)) % 2 == 1)
                            {
                                listPrim.Add(strFN);
                            }
                            break;
                        case 2:  //每天取一组数据
                            {
                                int cIndex=strFN.IndexOf('#');
                                string strTime = strFN.Substring(cIndex +1, 8);
                                if (strTime!=strFNOld)
                                {
                                    listPrim.Add(strFN);
                                    strFNOld = strTime;
                                }
                            }
                            break;
                        default:
                            {
                                int tIndex = iType % 300;
                                int cIndex1 = strFN.IndexOf('#')+10;
                                int cIndex2 = strFN.IndexOf('.');
                                int No = Int32.Parse(strFN.Substring(cIndex1, cIndex2 - cIndex1));
                                if (tIndex == No)
                                {
                                    listPrim.Add(strFN);                                     
                                }
                            }
                            break;

                    }
                   
                }
               
            }
            num=listPrim.Count;
            strFileName = new string[num];
            for (i = 0; i < num; i++)
            {
                strFileName[i] = (string)listPrim[i];
            }
        }


        //获取文件夹下所有文件名
        public void GetFileDate( string dirPath, ref DateTime[] dt)
        {
            ArrayList list = new ArrayList();
           // ArrayList listPrim = new ArrayList();
            if (Directory.Exists(dirPath))
            {
                list.AddRange(Directory.GetFiles(dirPath));
            }
            int num = list.Count;
            int i;
            dt = new DateTime[num];
            for (i = 0; i < num; i++)
            {
                string strFN = (string)list[i];
                string strFN1 = strFN.Substring(strFN.Length - 19, 10);
                strFN1 = strFN1.Replace('.', '-');
                strFN = strFN.Substring(strFN.Length - 8, 8);
                strFN = strFN.Replace('.', ':');
                strFN = strFN1 + ' ' + strFN;
                dt[i] = DateTime.Parse(strFN);
            }
            
        }


       //  [1/5/2013 Administrator]
       /*
        public int iFileIndex;             //文件序号  从零开始
               public int iSerial;                //测点序号   从零开始
               public int iLen;                   //数据长度
               public float fFreq;                //采样频率
               public string strUnit;             //数据单位
               public string strHeadInfo;          //文件头信息
               public DateTime dt;                 //采样时间
               public int ChnnlID;
               public int SessionID;
               public int SampleMode;
               public float fScale;
               public float fK;
               public float fB;
               public float fFilter;
               public string strFileName;  */
       
        //读取数据头文件
        public bool ReadHeadData(string strFileName, int index, ref DataHeadInfo dhi)
        {
            strFileName = strFileName.Substring(0, strFileName.Length - 3) + "set";
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return false;
            }
            FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            int iChnlCount = br.ReadInt32();
            if (index >= iChnlCount)
            {
                index = 0;
            }
            string strInfo;
            for (int i = 0; i < iChnlCount; i++)
            {
               dhi.iSerial= br.ReadInt32();
               dhi.ChnnlID= br.ReadInt32();
               dhi.SampleMode = br.ReadInt32();
               dhi.fFreq = br.ReadSingle();
               dhi.iLen = br.ReadInt32();
               dhi.SessionID = br.ReadInt32();
               dhi.strHeadInfo = br.ReadString();
               dhi.strUnit = br.ReadString();
                strInfo = br.ReadString();
                int iPos = strInfo.IndexOf('/', 8);                
                strInfo = strInfo.Substring(0, iPos) + " " + strInfo.Substring(iPos + 1, strInfo.Length - iPos - 1);
                strInfo = strInfo.Replace('/', '-');
                dhi.dt = DateTime.Parse(strInfo);
                dhi.fScale=br.ReadSingle();
                dhi.fK = br.ReadSingle();
                dhi.fB = br.ReadSingle();
                dhi.fFilter = br.ReadSingle();
                if (i == index)
                {
                    break;
                }
            }

            br.Close();
            fs.Close();
            return true;
        }


        //读取文本数据(多列)
        public bool ReadMidFeature(string strFileName, ref double[,] data)
        {
            strFileName = System.Environment.CurrentDirectory + "\\" + strFileName;
            if (!File.Exists(strFileName))
            {
               // MessageBox.Show("文件不存在");
                return false;
            }
            string strData;
            StreamReader sr = new StreamReader(strFileName);            
            int num=0;
            while ((strData = sr.ReadLine()) != null)
            {
                string[] b = strData.Split();
                int col=0;
                for(int i = 0; i < b.Length;i++ )
                {
                    if(b[i]=="")
                        continue;

                    data[num, col] = float.Parse(b[i]);
                    col++;
                }
                num++;
            }
            sr.Close();
            return true;
        }
        //保存文本数据(多列)
        public void WriteMidFeature(string strFileName, double[,] data)
        {
            strFileName = System.Environment.CurrentDirectory + "\\" + strFileName;
            int m = data.GetLength(0);
            int n = data.GetLength(1);
            string strData;
            StreamWriter sr = new StreamWriter(strFileName);
            
            for (int i = 0; i < m;i++ )
            {
                strData = "";
                
                for (int j = 0; j < n; j++)
                {
                    strData += data[i, j].ToString() + "    ";
                }
                sr.WriteLine(strData);
            }              
           
            sr.Close();
        }

        //保存密码
        public void WritePssWrd(bool bRem,string strAcc,string strPss)
        { 
            string strPath = System.Environment.CurrentDirectory + "\\pwd";
            FileStream fs = new FileStream(strPath, FileMode.Create, FileAccess.Write);
            BinaryWriter br = new BinaryWriter(fs);
            br.Write(bRem);
            br.Write(strAcc);
            br.Write(strPss);
            br.Close();
            fs.Close();
        }

        //保存密码
        public void ReadPssWrd(ref bool bRem, ref string strAcc, ref string strPss)
        {
            string strPath = System.Environment.CurrentDirectory + "\\pwd";
            FileStream fs = new FileStream(strPath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            bRem = br.ReadBoolean();
            strAcc = br.ReadString();
            strPss = br.ReadString();
            br.Close();
            fs.Close();
        }

        //
        public void CheckOpenGL()
        {            
            string strPath = System.Environment.CurrentDirectory + "\\Tao.OpenGl.dll";
            string strPath1 = System.Environment.CurrentDirectory + "\\Tao.OpenGl(bk).dll";
             if (!File.Exists(strPath))
             {
                 File.Copy(strPath1, strPath, true);
             }
             strPath = System.Environment.CurrentDirectory + "\\Tao.Platform.Windows.dll";
             if (!File.Exists(strPath))
             {
                 strPath1 = System.Environment.CurrentDirectory + "\\Tao.Platform.Windows(bk).dll";
                 File.Copy(strPath1, strPath, true);
             }
        }

        //输出维数约简3D图
        public void Save3DResult(double[][] dOutput)
        {
            try
            {
                // FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "output.txt", false);
                int len = dOutput.GetLength(0);
                for (int i = 0; i < len; i++)
                {
                    sw.WriteLine(dOutput[i][0].ToString() + " " + dOutput[i][1].ToString() + " " + dOutput[i][2].ToString());
                }
                sw.Close();
            }
            catch (Exception ex)
            {
                return;
            }

        }

        //读取数据文件
        public bool ReadDiagData(string strFileName, ref double[] data, int iColoumnIndex, int iStart, int iTotalLen)
        {
            if (!File.Exists(strFileName))
            {
                MessageBox.Show("文件不存在");
                return false;
            }
            int iLen = data.Length;
            FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            br.ReadInt32();
            br.ReadDouble();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadBytes(((iColoumnIndex - 1) * iTotalLen + iStart) * sizeof(double));
            for (int i = 0; i < iLen; i++)
            {
                data[i] = br.ReadDouble();
            }
            br.Close();
            fs.Close();
            return true;
        }

    }
}
