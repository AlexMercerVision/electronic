using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Collections;
namespace VisionApplication
{

    /**
     * BinaryWriter写入的是数据的内存模型，打开文件看到的是该数字对应的ASCII码
     * StreamWriter写入的是数据的每个字符对应的ASCII码数据，打开文件看到的是该数字的每个字符(明文)
     * */
    class FileRW
    {
         //将数据库数据导出为文本数据
       

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
        public void WritePssWrd(ref bool bRem, ref string strAcc, ref string strPss)
        {
            string strPath = System.Environment.CurrentDirectory + "\\pwd";
            FileStream fs = new FileStream(strPath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            bRem = br.ReadBoolean();
            strAcc = br.ReadString();
            strPss = br.ReadString();
            fs.Close();
        }
    }
}
