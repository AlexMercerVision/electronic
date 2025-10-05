using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections;

 

namespace CommonCls
{

    public struct saveParam
    {
       public int iSaveType ;       //保存间隔
       public string strPath ;
       public string strSigSavePath;    //单帧保存地址
       public string strConSavePath;    //连续帧保存地址
       public string strFileType;
       public int iSaveTime ;           //连续保存时间 ，单位s
       public int bDepthBitType ;       //true 16位  false  8位
       public int bInfrBitType ;        //true 16位  false  8位
       public int iSaveBodyData;
       public int iSaveCloudData ;
    }
    public struct trilt
    {
        public Int32 verX;
        public Int32 verY;
        public Int32 verZ;
    }
    class FileOper
    {
        //文件读取操作
        private StreamReader objReader;      //文件读取实例
        private ArrayList al;                //用于暂存文件中数据
        public int vertexNum;               //记录顶点个数
        public myVertex[] pVertexs;         //记录顶点数据
        public trilt[] myTtrilt;         //记录顶点数据

        public trilt[] loadTruiFile(String fileName)
        {
            objReader = new StreamReader(fileName);
            al = new ArrayList();
            string texLineTem = "";    //暂存读取行数据

            try
            {
                for (; objReader.Peek() != -1; )
                {
                    texLineTem = objReader.ReadLine();
                    al.Add(texLineTem);
                    vertexNum++;
                }

                myTtrilt = new trilt[vertexNum];

                int vFlag = 0;     //顶点循环标志 三角面片循环标志

                foreach (string temp in al)
                {
                    string[] tempArray = temp.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (tempArray.Length == 0)
                        continue;
                    myTtrilt[vFlag].verX = (Int32)(float.Parse(tempArray[0]))-1;
                    myTtrilt[vFlag].verY = (Int32)(float.Parse(tempArray[1]))-1;
                    myTtrilt[vFlag].verY = (Int32)(float.Parse(tempArray[2]))-1;

                    vFlag++;          //顶点标志递增   
                }
                objReader.Close();
            }
            catch (IOException e)
            {
                MessageBox.Show("读取点云文件失败,请确定文件格式正确");
            }
            return myTtrilt;
        }
        public myVertex[] loadXYZFile(String fileName)
        {
            objReader = new StreamReader(fileName);
            al = new ArrayList();
            string texLineTem = "";    //暂存读取行数据

            try
            {
                for (; objReader.Peek() != -1; )
                {
                    texLineTem = objReader.ReadLine();
                    al.Add(texLineTem);
                    vertexNum++;
                }

                pVertexs = new myVertex[vertexNum];

                int vFlag = 0;     //顶点循环标志  

                foreach (string temp in al)
                {
                    string[] tempArray = temp.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (tempArray.Length == 0)
                        continue;
                    pVertexs[vFlag].x = float.Parse(tempArray[0]);                    
                    pVertexs[vFlag].y = float.Parse(tempArray[1]);
                    pVertexs[vFlag].z = float.Parse(tempArray[2]);

                    vFlag++;          //顶点标志递增   
                }
                objReader.Close();
            }
            catch (IOException e)
            {
                MessageBox.Show("读取点云文件失败,请确定文件格式正确");
            }
            return pVertexs;
        }


        public myVertex[] loadPointCloudFile(String fileName,ref bool bColor)
        {
            objReader = new StreamReader(fileName);
            al = new ArrayList();
            string texLineTem = "";    //暂存读取行数据

            try
            {
                for (; objReader.Peek() != -1; )
                {
                    texLineTem = objReader.ReadLine();
                    al.Add(texLineTem);
                    vertexNum++;
                }

                pVertexs = new myVertex[vertexNum];

                int vFlag = 0;     //顶点循环标志 三角面片循环标志

                foreach (string temp in al)
                {
                    string[] tempArray = temp.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (tempArray.Length == 0)
                        continue;
                    pVertexs[vFlag].x = float.Parse(tempArray[0]);
                    pVertexs[vFlag].z = float.Parse(tempArray[2]);
                    pVertexs[vFlag].y = float.Parse(tempArray[1]);
                    if (tempArray.Length >= 6)
                    {
                        pVertexs[vFlag].r = float.Parse(tempArray[3]);
                        pVertexs[vFlag].g = float.Parse(tempArray[4]);
                        pVertexs[vFlag].b = float.Parse(tempArray[5]);
                        bColor = true;
                    }
                    vFlag++;          //顶点标志递增   
                }
                objReader.Close();
            }
            catch (IOException e)
            {
                MessageBox.Show("读取点云文件失败,请确定文件格式正确");
            }
            return pVertexs;
        }

        //保存 保存参数
        public void SaveSavePara(saveParam sp)
        {
            FileStream fs = new FileStream("..\\userdata\\Param.ini", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(sp.strPath);
            sw.WriteLine(sp.iSaveType);
            sw.WriteLine(sp.iSaveTime);
            
            sw.WriteLine(sp.iSaveBodyData);
            sw.WriteLine(sp.iSaveCloudData);
            sw.WriteLine(sp.bDepthBitType);
            sw.WriteLine(sp.bInfrBitType);
            sw.WriteLine(sp.strFileType);
            sw.Flush();
            sw.Close();
            fs.Close(); 
        }

        public saveParam ReadSavePara( )
        {
            saveParam sp=new saveParam();
            objReader = new StreamReader("..\\userdata\\Param.ini");
            string texLineTem = "";    //暂存读取行数据
            try
            {
                texLineTem = objReader.ReadLine();
                sp.strPath = texLineTem.Trim();
                if (sp.strPath.Substring(sp.strPath.Length - 1, 1) != "\\")
                {
                    sp.strPath = sp.strPath + "\\";
                }
               

                texLineTem = objReader.ReadLine();
                sp.iSaveType =int.Parse(texLineTem.Trim());

                texLineTem = objReader.ReadLine();
                sp.iSaveTime = int.Parse(texLineTem.Trim());

                texLineTem = objReader.ReadLine();
                sp.iSaveBodyData = int.Parse(texLineTem.Trim());

                texLineTem = objReader.ReadLine();
                sp.iSaveCloudData = int.Parse(texLineTem.Trim());

                texLineTem = objReader.ReadLine();
                sp.bDepthBitType = int.Parse(texLineTem.Trim());

                texLineTem = objReader.ReadLine();
                sp.bInfrBitType = int.Parse(texLineTem.Trim());

                texLineTem = objReader.ReadLine();
                sp.strFileType = texLineTem.Trim();

            }
            catch (IOException e)
            {
                objReader.Close();
                MessageBox.Show("读取参数文件失败");
            }

            objReader.Close();
            if (!Directory.Exists(sp.strPath))
            {
                sp.strPath = System.IO.Directory.GetCurrentDirectory();
                SaveSavePara(sp);
            }
            return sp;
        }

        //保存骨骼数据，myBody 自定义结构体数组
        public void SaveBodyData(myBody[] bodies, string path)
        {            
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            for (int j=0;j< bodies.Length; j++)
            {
                myBody x = bodies[j];
                if (x.iTracked)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        sw.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", (JointType)i, x.mJoint[i].x, x.mJoint[i].y, x.mJoint[i].z, x.csp[i].X, x.csp[i].Y,
                             x.mJointOri[i].Orientation.X, x.mJointOri[i].Orientation.Y, x.mJointOri[i].Orientation.Z, x.mJointOri[i].Orientation.W, x.mJoint[i].TrackingState);
                    }
                }
                sw.Write("\r\n");
            }
            sw.Flush();
            sw.Close();
            fs.Close();
        }

        public void SaveBodyData(myBody bodies, string path)
        {
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
          
                myBody x = bodies;
                if (x.iTracked)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        sw.WriteLine("{0} {1} {2} {3}", (JointType)i, x.mJoint[i].x, x.mJoint[i].y, x.mJoint[i].z);
                    }
                }
                sw.Write("\r\n");
           
            sw.Flush();
            sw.Close();
            fs.Close();
        }

        //保存骨骼数据，Body 结构体数组
        public void SaveBodyData(Body[] bodies, ColorSpacePoint[][] csp, string path)
        {
        //    string myPhotos = "bodyData\\";
         //   string path = Path.Combine(myPhotos, "body_" + time + ".txt");
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);

            StreamWriter sw = new StreamWriter(fs);
            int j = 0;
            foreach (Body x in bodies)
            {

                for (int i = 0; i < 25; i++)
                {
                    sw.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", (JointType)i, x.Joints[(JointType)i].Position.X, x.Joints[(JointType)i].Position.Y, x.Joints[(JointType)i].Position.Z, csp[j][i].X, csp[j][i].Y,
                         x.JointOrientations[(JointType)i].Orientation.X, x.JointOrientations[(JointType)i].Orientation.Y, x.JointOrientations[(JointType)i].Orientation.Z, x.JointOrientations[(JointType)i].Orientation.W, x.Joints[(JointType)i].TrackingState);
                }
                j++;
                sw.Write("\r\n");
            }
            sw.Flush();
            sw.Close();
            fs.Close();
        }

       

        public void ReadBodyData(String fileName, myBody[] mBody, ColorSpacePoint[][] csp)
        {
            objReader = new StreamReader(fileName);
            string texLineTem = "";    //暂存读取行数据
            int iBodyIndex = 0;
            int iJointIndex = 0;
            mBody[0].iTracked = false;
            try
            {
                for (; objReader.Peek() != -1;)
                {
                    texLineTem = objReader.ReadLine();
                    string[] tempArray = texLineTem.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (tempArray.Length == 0)
                        continue;

                    if (float.Parse(tempArray[3]) != 0)
                        mBody[iBodyIndex].iTracked = true;
                    mBody[iBodyIndex].mJoint[iJointIndex].x = float.Parse(tempArray[1]);
                    mBody[iBodyIndex].mJoint[iJointIndex].y = float.Parse(tempArray[2]);
                    mBody[iBodyIndex].mJoint[iJointIndex].z = float.Parse(tempArray[3]);

                    if (tempArray.Length > 6)  //新文件格式，三维坐标+二维坐标
                    {
                        csp[iBodyIndex][iJointIndex].X = float.Parse(tempArray[4]);
                        csp[iBodyIndex][iJointIndex].Y = float.Parse(tempArray[5]);
                    }
                    if (tempArray.Length > 10)  //新文件格式，三维坐标+二维坐标
                    {
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.X= float.Parse(tempArray[6]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.Y = float.Parse(tempArray[7]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.Z = float.Parse(tempArray[8]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.W = float.Parse(tempArray[9]);

                    }
                    //兼容不同的文件格式
                    if (tempArray.Length == 5)
                    {
                        if (tempArray[4] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[4] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;
                    }
                    else if (tempArray.Length == 7)
                    {
                        if (tempArray[6] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[6] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;

                    }
                    else if (tempArray.Length == 11)
                    {
                        if (tempArray[10] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[10] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;

                    }
                    else
                    {
                        mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                    }
                    //  mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = TrackingState.Parse(tempArray[4]);
                    iJointIndex++;
                    if (iJointIndex == 25)
                    {
                        iBodyIndex++;
                        iJointIndex = 0;
                        if (iBodyIndex < 6)
                            mBody[iBodyIndex].iTracked = false;
                    }

                }
                objReader.Close();
            }
            catch (IOException e)
            {
                MessageBox.Show("读取骨骼文件失败,请确定文件格式正确");
            }
        }
        public void ReadBodyData(String fileName, myBody[] mBody)
        {
            objReader = new StreamReader(fileName);
            string texLineTem = "";    //暂存读取行数据
            int iBodyIndex = 0;
            int iJointIndex = 0;
            mBody[0].iTracked = false;
            try
            {
                for (; objReader.Peek() != -1;)
                {
                    texLineTem = objReader.ReadLine();
                    string[] tempArray = texLineTem.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (tempArray.Length == 0)
                        continue;

                    if (float.Parse(tempArray[3]) != 0)
                        mBody[iBodyIndex].iTracked = true;
                    mBody[iBodyIndex].mJoint[iJointIndex].x = float.Parse(tempArray[1]);
                    mBody[iBodyIndex].mJoint[iJointIndex].y = float.Parse(tempArray[2]);
                    mBody[iBodyIndex].mJoint[iJointIndex].z = float.Parse(tempArray[3]);

                    if (tempArray.Length > 6)  //新文件格式，三维坐标+二维坐标
                    {
                        mBody[iBodyIndex].csp[iJointIndex].X = float.Parse(tempArray[4]);
                        mBody[iBodyIndex].csp[iJointIndex].Y = float.Parse(tempArray[5]);
                    }
                    if (tempArray.Length > 10)  //新文件格式，关节点方向
                    {
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.X = float.Parse(tempArray[6]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.Y = float.Parse(tempArray[7]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.Z = float.Parse(tempArray[8]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.W = float.Parse(tempArray[9]);

                    }
                    //兼容不同的文件格式
                    if (tempArray.Length == 5)
                    {
                        if (tempArray[4] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[4] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;
                    }
                    else if (tempArray.Length == 7)
                    {
                        if (tempArray[6] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[6] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;

                    }
                    else if (tempArray.Length == 11)
                    {
                        if (tempArray[10] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[10] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;

                    }
                    else
                    {
                        mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                    }
                    //  mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = TrackingState.Parse(tempArray[4]);
                    iJointIndex++;
                    if (iJointIndex == 25)
                    {
                        iBodyIndex++;
                        iJointIndex = 0;
                        if (iBodyIndex < 6)
                            mBody[iBodyIndex].iTracked = false;
                    }

                }
                objReader.Close();
            }
            catch (IOException e)
            {
                MessageBox.Show("读取骨骼文件失败,请确定文件格式正确");
            }
        }

        public myBody[] ReadBodyData(String fileName)
        {
            myBody[] mBody = new myBody[6];
            for (int i = 0; i < 6; i++)
            {
                mBody[i].mJoint = new myJoint[25];
                mBody[i].csp = new ColorSpacePoint[25];
                mBody[i].mJointOri = new JointOrientation[25];
            }
             objReader = new StreamReader(fileName);
            string texLineTem = "";    //暂存读取行数据
            int iBodyIndex = 0;
            int iJointIndex = 0;
            mBody[0].iTracked = false;
            try
            {
                for (; objReader.Peek() != -1; )
                {
                    texLineTem = objReader.ReadLine();
                    string[] tempArray = texLineTem.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (tempArray.Length == 0)
                        continue;

                    if (float.Parse(tempArray[3]) != 0)
                        mBody[iBodyIndex].iTracked = true;
                    mBody[iBodyIndex].mJoint[iJointIndex].x = float.Parse(tempArray[1]);
                    mBody[iBodyIndex].mJoint[iJointIndex].y = float.Parse(tempArray[2]);
                    mBody[iBodyIndex].mJoint[iJointIndex].z = float.Parse(tempArray[3]);

                    if (tempArray.Length > 6)  //新文件格式，三维坐标+二维坐标
                    {
                        mBody[iBodyIndex].csp[iJointIndex].X = float.Parse(tempArray[4]);
                        mBody[iBodyIndex].csp[iJointIndex].Y = float.Parse(tempArray[5]);
                    }
                    if (tempArray.Length > 10)  //新文件格式，关节点方向
                    {
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.X = float.Parse(tempArray[6]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.Y = float.Parse(tempArray[7]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.Z = float.Parse(tempArray[8]);
                        mBody[iBodyIndex].mJointOri[iJointIndex].Orientation.W = float.Parse(tempArray[9]);

                    }


                    if (tempArray.Length == 5)
                    {
                        if (tempArray[4] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[4] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;
                    }
                    else if (tempArray.Length == 7)
                    {
                        if (tempArray[6] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[6] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;

                    }
                    else if (tempArray.Length == 11)
                    {
                        if (tempArray[10] == "Tracked")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                        else if (tempArray[10] == "Inferred")
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)1;
                        else
                            mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)0;

                    }
                    else
                    {
                        mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = (TrackingState)2;
                    }
                    //  mBody[iBodyIndex].mJoint[iJointIndex].TrackingState = TrackingState.Parse(tempArray[4]);
                    iJointIndex++;
                    if (iJointIndex == 25)
                    {
                        iBodyIndex++;
                        iJointIndex = 0;
                        if (iBodyIndex < 6)
                            mBody[iBodyIndex].iTracked = false;
                    }

                }
                objReader.Close();
            }
            catch (IOException e)
            {
                MessageBox.Show("读取骨骼文件失败,请确定文件格式正确");
            }
            return mBody;
        }
    }
}
