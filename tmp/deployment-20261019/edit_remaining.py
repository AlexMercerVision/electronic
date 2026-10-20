from edit_support import read, write, replace
p='VisionApplication/FormSetPasswords.cs'; s=read(p)
s=replace(s,'this.textBox_OperaterPassword.Text = mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Administrator);',
    'this.textBox_OperaterPassword.Text = mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Operator);')
s=replace(s,'       mPasswordFile.WritePasswordFile();', '''       try { mPasswordFile.WritePasswordFile(); }
       catch (Exception ex)
       {
           DialogResult = DialogResult.None;
           MessageBox.Show("密码保存失败：" + ex.Message, "密码设置");
       }''')
write(p,s)

p='VisionApplication/FileRW.cs'; s=read(p)
s=replace(s,'System.Environment.CurrentDirectory + "\\\\pwd"','CommonCls.ApplicationPaths.Data("UserData", "pwd")',2)
s=replace(s,'            FileStream fs = new FileStream(strPath, FileMode.Create, FileAccess.Write);',
    '            Directory.CreateDirectory(Path.GetDirectoryName(strPath));\n            FileStream fs = new FileStream(strPath, FileMode.Create, FileAccess.Write);')
write(p,s)

p='CommonCls/ClsList.cs'; s=read(p)
s=replace(s,'"data\\\\predefined_classes.txt"','Path.Combine(ApplicationPaths.WorkingDataDirectory, "predefined_classes.txt")',2)
# ClsList's editor writes machine data, never the installed model label file.
s=replace(s,'                File.WriteAllLines(Path.Combine(ApplicationPaths.WorkingDataDirectory, "predefined_classes.txt"), classNames);',
    '                Directory.CreateDirectory(ApplicationPaths.WorkingDataDirectory);\n                File.WriteAllLines(Path.Combine(ApplicationPaths.WorkingDataDirectory, "predefined_classes.txt"), classNames);')
write(p,s)
