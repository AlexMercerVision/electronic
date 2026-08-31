using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CommonCls
{
    public partial class ClsList : Form
    {
        // 公共属性：选中的类别索引（从1开始，0表示未选择）
        public int SelectedClassIndex { get; private set; } = 1;

        // 引用主窗体的类别列表（直接操作该列表，以实现同步）
        private List<string> _classNames;

        // ---- 构造函数（兼容原有无参调用） ----
        public ClsList()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            _classNames = new List<string>();  // 默认空列表
            InitializeList();
        }

        // ---- 构造函数（兼容原有单参数调用） ----
        public ClsList(int selectedIndex) : this()
        {
            // 此构造函数只设置选中索引，但无类别列表，所以无法有效工作。
            // 建议废弃此构造，改为调用双参数版本。
            // 若仍调用此构造，则无法从列表中选择，因为 _classNames 为空。
            // 保留仅为了兼容，但业务上会失效，因此我们强制加载默认类别文件。
            LoadDefaultClasses();
            if (selectedIndex > 0 && selectedIndex <= _classNames.Count)
                listCls.SelectedIndex = selectedIndex - 1;
        }

        // ---- 主要构造函数（双参数） ----
        public ClsList(List<string> classNames, int selectedIndex = -1)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            _classNames = classNames ?? new List<string>();
            InitializeList(selectedIndex);
        }

        // 初始化列表（内部方法）
        private void InitializeList(int selectedIndex = -1)
        {
            listCls.Items.Clear();
            foreach (var cls in _classNames)
                listCls.Items.Add(cls);

            if (selectedIndex >= 0 && selectedIndex < _classNames.Count)
            {
                listCls.SelectedIndex = selectedIndex;
                txtClsName.Text = _classNames[selectedIndex];
                SelectedClassIndex = selectedIndex + 1;
            }
            else
            {
                txtClsName.Text = "";
                SelectedClassIndex = 0;
            }

            // 绑定文本框内容变化事件
            txtClsName.TextChanged += TxtClsName_TextChanged;
        }

        // 若 _classNames 为空，尝试从默认文件加载
        private void LoadDefaultClasses()
        {
            if (_classNames == null || _classNames.Count == 0)
            {
                var fileOper = new FileOper();
                _classNames = fileOper.ReadRemarkImageFile("data\\predefined_classes.txt");
                if (_classNames == null || _classNames.Count == 0)
                {
                    _classNames = new List<string> { "liewen", "posz", "liewenWP", "poszWP", "flaking" };
                    SaveClassNamesToFile(_classNames);
                }
            }
        }

        // 窗体 Load 事件（必须存在，因为设计器注册了）
        private void ClsList_Load(object sender, EventArgs e)
        {
            // 无需额外操作
        }

        // 文本框内容变化时，自动匹配列表项
        private void TxtClsName_TextChanged(object sender, EventArgs e)
        {
            string input = txtClsName.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                listCls.ClearSelected();
                return;
            }

            int idx = _classNames.FindIndex(c => c.Equals(input, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                listCls.SelectedIndex = idx;
                SelectedClassIndex = idx + 1;
            }
            else
            {
                listCls.ClearSelected();
                SelectedClassIndex = 0;   // 0 表示新增
            }
        }

        // 点击“确定”按钮
        private void btOk_Click(object sender, EventArgs e)
        {
            string input = txtClsName.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入类别名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 检查是否已存在（不区分大小写）
            int existIdx = _classNames.FindIndex(c => c.Equals(input, StringComparison.OrdinalIgnoreCase));
            if (existIdx >= 0)
            {
                SelectedClassIndex = existIdx + 1;
                this.DialogResult = DialogResult.OK;
                Close();
                return;
            }

            // 新增类别
            _classNames.Add(input);
            SaveClassNamesToFile(_classNames);

            // 重新填充列表
            listCls.Items.Clear();
            foreach (var cls in _classNames)
                listCls.Items.Add(cls);
            listCls.SelectedIndex = _classNames.Count - 1;
            SelectedClassIndex = _classNames.Count;
            txtClsName.Text = input;

            this.DialogResult = DialogResult.OK;
            Close();
        }

        // 取消按钮
        private void btCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        // 双击列表项
        private void listCls_DoubleClick(object sender, EventArgs e)
        {
            if (listCls.SelectedIndex >= 0)
            {
                txtClsName.Text = listCls.SelectedItem.ToString();
                SelectedClassIndex = listCls.SelectedIndex + 1;
                this.DialogResult = DialogResult.OK;
                Close();
            }
        }

        // 列表选中项改变
        private void listCls_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listCls.SelectedIndex >= 0)
            {
                txtClsName.Text = listCls.SelectedItem.ToString();
                SelectedClassIndex = listCls.SelectedIndex + 1;
            }
        }

        // 保存类别到文件（静态方法）
        public static void SaveClassNamesToFile(List<string> classNames)
        {
            try
            {
                File.WriteAllLines("data\\predefined_classes.txt", classNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存类别文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 实例保存方法
        private void SaveClassNamesToFile()
        {
            SaveClassNamesToFile(_classNames);
        }
    }
}