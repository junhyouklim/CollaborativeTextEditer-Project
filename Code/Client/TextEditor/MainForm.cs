using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace TextEditor
{
    public partial class MainForm : Form
    {
        TextnamingForm t; // 새파일 파일명 입력 창  
        MainForm_Tcp tcp_write, tcp_read;
        enum TYPE { EDIT_SERV, PROC_INFO, MAIN, MAIN_READ };
        string file_list;
        delegate void Refresh_Delegate();
        
        public MainForm()
        {
            InitializeComponent();
            CenterToScreen(); // 중앙 위치
            
            tcp_write = new MainForm_Tcp();
            tcp_write.Connect_To_Server((int)TYPE.MAIN);
            
            tcp_read = new MainForm_Tcp();
            tcp_read.Connect_To_Server((int)TYPE.MAIN_READ);
                 
            file_list = "";
            tcp_write.Get_FileNames(ref file_list);
            FileList_Update();            
            listBox1.MouseDoubleClick += new MouseEventHandler(ListItem_DoubleClick);
            
            Thread thd = new Thread(new ThreadStart(FileList_Refresh_Thread)); // 파일 변경 정보 Read 쓰레드 생성 
            thd.IsBackground = true;
            thd.Start();
        }

        // 서버로부터 변경되는 파일 리스트 받고, 리프레시 하는 쓰레드
        private void FileList_Refresh_Thread()
        {
            while (true)
            {
                file_list = "";
                tcp_read.Get_FileNames(ref file_list);
                
                if (listBox1.InvokeRequired)
                {
                    Refresh_Delegate dele = FileList_Update;
                    listBox1.Invoke(dele);                       
                }                    
            }
        }
        //
        private void FileList_Update()
        {
            listBox1.Items.Clear();
            listBox1.Items.Add("새 파일");              
            string[] files = file_list.Split('/');
            foreach (string file_name in files)
            {
                if(file_name != "")
                    listBox1.Items.Add(file_name);       
            }
        }

        // 파일 더블 클릭 
        private void ListItem_DoubleClick(object sender, MouseEventArgs e)
        {
            string name = "";
            int select = listBox1.IndexFromPoint(e.Location);
            if (select == -1) return;
            string file = listBox1.SelectedItem.ToString();
            if (file == "") return;

            if (file == "새 파일") // 새파일 생성 
            {
                t = new TextnamingForm(this);
                t.ShowDialog(); // TextnamingForm 띄운다. -> 본체를 가지고 간다.

                if (!string.IsNullOrEmpty(t.tbox_textnaming.Text.Trim()))
                {
                    if (t.respawn == 0)
                    {
                        return;
                    }
                    else
                    {
                        name = t.tbox_textnaming.Text.Trim();
                        //WriteLine($"폼에서 가져온 {name}");
                    }
                }
                else
                {
                    return;
                }                
                bool check = tcp_write.Create_File(name);
                if (check == false)
                    MessageBox.Show("파일 생성 실패\n파일명을 확인해주세요");
            }
            else // 파일 열기
            {
                Console.WriteLine("선택한 파일명 : [{0}]", file);

                int proc_port = tcp_write.Get_ProcessServer_Port(file);
                
                EditForm edit = new EditForm(file, proc_port);
                edit.Show();
            }
        }

        // 파일 삭제 버튼
        private void DeleteBtn_Click(object sender, EventArgs e)
        {
            string name = "";

            if (listBox1.SelectedIndex != -1)
            {
                name = listBox1.SelectedItem.ToString();               
                Console.WriteLine("삭제될 파일 이름: " + name);

                bool check = tcp_write.Delete_File(name);
                if (check == false)
                    MessageBox.Show("파일 삭제 실패");
                else
                    MessageBox.Show("파일 삭제 성공");
            }
            else
            {
                MessageBox.Show("삭제할 파일을 선택해 주세요");
            }
        }
    }
}
