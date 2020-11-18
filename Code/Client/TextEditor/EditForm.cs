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
using System.Diagnostics; // 디버그
using System.IO; // 파일 저장
using System.Runtime.InteropServices; // .dll 사용
using System.Text.RegularExpressions;

namespace TextEditor
{
    public partial class EditForm : Form
    {
        // 서버접속+스레드진입용,커서정보read용,operation정보read용,operation정보write용
        EditForm_Tcp TCP, cursor_read, READ, WRITE;
        int my_id; // 서버에서 배정 받은 id         
        string my_file; // 내가 작업중인 파일명        

        // 커서 관련
        [DllImport("user32.dll")]
        public static extern bool GetCaretPos(out System.Drawing.Point lpPoint);
        bool isCursorRead = false; // 커서정보 read 쓰레드 반복 조건
        List<CursorInfo> cursors = new List<CursorInfo>(); // 같은 파일작업하는 커서정보 담음  
        private delegate void CrossThreadDelegate_Cursor(int index);        

        // 한글,영문키 인식API
        [DllImport("imm32.dll")]
        private static extern IntPtr ImmGetContext(IntPtr hwnd);
        [DllImport("imm32.dll")]
        private static extern bool ImmGetConversionStatus(IntPtr himc, ref int lpdw, ref int lpdw2);

        // Operation 관련
        private Stopwatch stopwatch;
        public const double TIME_LIMIT = 2.5;

        private Operation operation; // 현재 작업
        private Operation old_operation; // 방금 큐에 넣어진 이전 작업

        private Queue<Operation> Task; // 내 operation을 모두 쌓아놓는 list
        private bool acknowledge = true; // 서버에 전달한 작업이 모든 클라이언트의 TextBox에 적용됐는지 확인변수

        // 서버로부터 시그널(1,2)을 대기하고 시그널에 따라서 operation을 전달 받는 Read용 스레드
        private Thread readThread;
        bool IsHangul = false; // 한글,영문상태확인
        
        private int startIndex;
        private int startLength;
        private bool startDelete = false;
        
        private bool EOF = false;
        private bool _IsEOF = false;
        LineInfo row = new LineInfo();
        int current_line;
        int current_index;

        enum TYPE { EDIT, EDIT_READ, EDIT_WRITE, CURSOR }; // 클라이언트 타입       
        enum CURSOR { INSERT, DELETE, UPDATE } // 커서 행동 시그널
        enum OperatorSignal { DEFAULT, INSERT, BACK, DELETE, ENTER } // operation 종류
        enum SELECTIONKEY { BACKSPACE = 1, DELETE }       

        public EditForm() { }
        public EditForm(string file, int ServerPort)
        {
            InitializeComponent();
            this.ContentsBox.Font = new Font("맑은고딕", 10);
            row.my_position_line = -1;
                       
            // 작업파일명을 넘겨서 operation을 초기화한다.
            CenterToParent(); // 부모 중앙에 위치
            my_file = file;
            this.Text = my_file; // 상단 제목 변경

            // 서버에 Signal 보내는 소켓
            TCP = new EditForm_Tcp(this); 
            TCP.Connect_To_Server((int)TYPE.EDIT, ServerPort);
            TCP.Get_ID(ref my_id);
            
            // 선택한 파일내용을 받기 전에 서버의 operation_list가 빌때까지 대기상태 진입
            TCP.Wait_For_Sync();

            string contents = "";
            TCP.Open_File(my_file, ref contents); // 선택한 파일 내용 받음
            ContentsBox.Text = contents;
            ContentsBox.SelectionStart = 0; // 커서위치 0으로 변경         

            // Stopwatch, Tasklist, Operation 객체생성
            InitializeBasic(file, my_id);

            // 서버측으로부터 Operation객체를 전달받는 소켓
            READ = new EditForm_Tcp(this, ref cursors, ref row);
            READ.Connect_To_Server((int)TYPE.EDIT_READ, ServerPort);
            readThread = new Thread(queThread);
            readThread.Start();

            // 서버측으로 Operation객체를 전달하는 소켓
            WRITE = new EditForm_Tcp(this); // 서버쪽 acknowledge_sock
            WRITE.Connect_To_Server((int)TYPE.EDIT_WRITE, ServerPort);

            // 커서의 정보를 전달받는 소켓
            cursor_read = new EditForm_Tcp(); // 서버쪽 cursor_sock
            cursor_read.Connect_To_Server((int)TYPE.CURSOR, ServerPort);
            Thread thd = new Thread(new ThreadStart(Cursor_Read_Thread)); // 커서 Read 쓰레드 생성 
            thd.IsBackground = true;
            isCursorRead = true;
            thd.Start();
                        
            // 서버에 모든 커서 정보 요청 >> 서버는 cursor_read쪽에 커서 정보 보냄
            TCP.Request_All_CursorInfo();

            // 메모장 닫을때 서버에 신호 보냄 
            FormClosing += new FormClosingEventHandler(Closing_Event);
        }

        // EditForm 종료 이벤트. 서버에 종료한다고 시그널 보내야 서버쪽에서 클라 정보 정리 가능 
        private void Closing_Event(object sender, FormClosingEventArgs e)
        {
            isCursorRead = false; // 커서정보 read 쓰레드 중지             
            TCP.Send_Disconnect_Signal();
        }

        //============================================================
        //          Operation 관련 메서드
        //============================================================
        private void InitializeBasic(string file, int clnt_id)
        {
            stopwatch = new Stopwatch();
            Task = new Queue<Operation>();
            operation = new Operation(file, clnt_id);
        }       

        // 작업 가능한 라인인지 체크 
        private void Check_Working_Line()
        {
            if (row.my_position_line != -1 && row.my_position_line != current_line)
            {
                ContentsBox.ReadOnly = true;               
                return;
            }

            foreach (CursorInfo cursor in cursors)
            {
                if (cursor.line == current_line)
                {
                    ContentsBox.ReadOnly = true;
                    break;
                }
                else
                {
                    ContentsBox.ReadOnly = false;
                }
            }           
        }

        // 잘못된 라인에서 작업시 경고 메시지 
        private bool Wrong_Line_Message()
        {
            int cur_line = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);

            if (ContentsBox.ReadOnly)
            {
                if (row.my_position_line != cur_line && row.my_position_line != -1)
                {
                    string Alarm = (row.my_position_line + 1).ToString() + "번째 줄에서 \n ESC를 눌러 라인해제";
                    AutoClosingMessageBox.Show(Alarm, "알람", 1000);
                    return true;
                }

                foreach (CursorInfo prime in cursors)
                {
                    if (prime.line == current_line)
                    {
                        string Alarm = "이미 사용중인 라인입니다.";
                        AutoClosingMessageBox.Show(Alarm, "알람", 1000);
                        return true;
                    }
                }
            }
            return false;
        }

        private void ContentsBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {     
            current_line = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);
            current_index = ContentsBox.SelectionStart - ContentsBox.GetFirstCharIndexFromLine(current_line);
            
            Check_Working_Line(); // 작업 가능한 라인인지 체크 

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    return;
            }

            bool isWrong = Wrong_Line_Message(); // 잘못된 라인에서 작업시 경고 메시지창 출력
            if (isWrong) return;
    
            if(e.KeyCode != Keys.Escape)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (operation.mode == (int)OperatorSignal.INSERT && !_IsEOF) // 태환 <- 형섭 추가
                    {
                        _IsEOF = true;
                        Debug.WriteLine("(e.KeyCode == Keys.Enter => 누름 => operation.mode == (int)OperatorSignal.INSERT && !_IsEOF");
                        Sequential_InsertAndEnter(); // ending_index_number -> 정립
                        Push_Operation();
                    }

                    if (!EOF && !_IsEOF) // 꾹 누름 -> 처음 값만 저장하도록 -> 키 업 될때까지
                    {
                        Debug.WriteLine("(e.KeyCode == Keys.Enter => 누름 => (!EOF && !_IsEOF)");
                        ExecuteEOF(1);
                    }
                    current_line = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);
                }
                if (!startDelete)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Back:
                            Console.WriteLine("Back: {0}", Keys.Back);
                            SetStartingOperation((int)OperatorSignal.BACK);
                            break;
                        case Keys.Delete:
                            Console.WriteLine("Delete: {0}", Keys.Delete);
                            SetStartingOperation((int)OperatorSignal.DELETE);
                            break;
                    }
                }
            }
        }

        private void ContentsBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (ContentsBox.ReadOnly) return;           

            if (e.KeyCode == Keys.Escape)
            {
                Debug.WriteLine("＃＃KeyDown -> X");
            }
            else
            {
                switch (e.KeyCode)
                {
                    // 줄이동 -> 커서만 아래줄로 이동 또는 이전 커서위치 우측문장을 데리고 아래줄로 이동
                    case Keys.Enter:
                        break;
                    case Keys.Back:
                    case Keys.Delete: // 수정
                                      //deleteText = false;
                        break;         
                    case Keys.ProcessKey:
                        try
                        {
                            IntPtr hIMC;
                            int dwConversion = 0;
                            int dwSentence = 0;
                            bool bCheck;
                            hIMC = ImmGetContext(ContentsBox.Handle);
                            //TextBox 한영키 상태값 얻기...
                            bCheck = ImmGetConversionStatus(hIMC, ref dwConversion, ref dwSentence);
                            if (dwConversion == 0)
                            {
                                //Console.WriteLine("한영키 상태 : 영어");
                                IsHangul = false;
                            }
                            else
                            {
                                //Console.WriteLine("한영키 상태 : 한글");
                                IsHangul = true;
                            }
                        }
                        catch
                        {
                        }               
                        break;
                    default:
                        break;
                }

                if (char.GetUnicodeCategory((char)e.KeyCode) ==
                System.Globalization.UnicodeCategory.OtherLetter)
                {
                    Console.WriteLine("한글입니다");
                }
            }
        }

        private void ContentsBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (ContentsBox.ReadOnly) return;
           
            if(e.KeyChar != (char)Keys.Escape)
            {
                // 특수문자 정규식
                var regex = new Regex(@"[^a-zA-Z0-9\s]");
                bool isInput = false;
                
                // 백스페이스 이외의 특수문자를 입력했을 때
                if (e.KeyChar != 8 && regex.IsMatch(e.KeyChar.ToString()))
                    isInput = true;
                // 숫자 또는 영어문자 또는 스페이스바를 입력했을 때
                else if (Char.IsDigit(e.KeyChar) || Char.IsLetter(e.KeyChar) || e.KeyChar == ' ')
                    isInput = true;
                // 새 Insert Operation 정보를 등록한다.
                if (isInput == true)
                {
                    // 첫입력에 대한 처리
                    if (operation.mode == (int)OperatorSignal.DEFAULT && !stopwatch.IsRunning)
                    {
                        // mode,입력라인을 설정한다.
                        operation.mode = (int)OperatorSignal.INSERT;
                        operation.Starting_Line_Number = current_line;
                        operation.Starting_Index_Number = current_index;
                        // 2초 제한시간 시작 --> 2초도달시 INSERT Operation 전달
                        stopwatch.Start();
                        Console.WriteLine("시간측정 시작!");
                    }
                    // 입력후 위치하는 커서위치를 설정하다.
                    operation.Ending_Index_Number = current_index + 1;
                }
            }
        }
          

        private void ContentsBox_KeyUp(object sender, KeyEventArgs e)
        {
         
            // 커서정보 전송
            Point point = new Point();
            GetCaretPos(out point);
            TCP.Send_Cursor_Info(point.X, point.Y, row.my_position_line);            

            if (ContentsBox.ReadOnly) return;
            
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    return;
            }        

            row.my_position_line = current_line;
            
            if (e.KeyCode == Keys.Escape)
            {                
                if (operation.mode != (int)OperatorSignal.DEFAULT)
                {
                    Push_Operation();
                }
                row.my_position_line = -1;
            }
            else
            {
                if (IsHangul == true && ((int)e.KeyValue > 64 && (int)e.KeyValue < 91))
                {
                    // 첫입력에 대한 처리
                    if (operation.mode == (int)OperatorSignal.DEFAULT && !stopwatch.IsRunning)
                    {
                        // mode,입력라인을 설정한다.
                        operation.mode = (int)OperatorSignal.INSERT;
                        operation.Starting_Line_Number = current_line;
                        operation.Starting_Index_Number = current_index;
                        // 2초 제한시간 시작 --> 2초도달시 INSERT Operation 전달
                        stopwatch.Start();
                        Console.WriteLine("한글이 입력되었습니다");
                        Console.WriteLine("시간측정 시작!");
                    }
                }

                switch (e.KeyCode)
                {                
                    case Keys.Back:
                        SetEndingOperation((int)OperatorSignal.BACK);
                        break;
                    case Keys.Delete:
                        SetEndingOperation((int)OperatorSignal.DELETE);
                        break;
                    case Keys.Enter:
                        if (!EOF && _IsEOF) // INSERT -> ENTER
                        {
                            row.my_position_line += 1;

                            foreach (CursorInfo prime in cursors)
                            {
                                //Debug.WriteLine("row.my_position_line: " + row.my_position_line + " / prime.line: " + prime.line);
                                if (row.my_position_line - 1 < prime.line)
                                {
                                    prime.line += 1;
                                }
                            }
                        }

                        if (EOF && !_IsEOF)
                        {
                            row.my_position_line += 1;
                            //Debug.WriteLine("ENTER -> current_line => " + current_line + " / row.my_position_line => " + row.my_position_line);
                            ExecuteEOF(2);
                            Push_Operation(); // Enqueue

                            foreach (CursorInfo prime in cursors)
                            {
                                if (row.my_position_line -1 < prime.line)
                                {
                                    prime.line += 1;
                                }
                            }
                        }
                        _IsEOF = false;
                        break;
                    default:
                        break;
                }
            }                   
            
        }

        private void SetEndingOperation(int signalkey)
        {
            int line = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);
            int index = ContentsBox.SelectionStart - ContentsBox.GetFirstCharIndexFromLine(line);

            if (signalkey == (int)OperatorSignal.BACK)
            {
                operation.Ending_Line_Number = line;
                operation.Ending_Index_Number = index;
                if (operation.starting_line_number != line && startLength > startIndex + 1)
                {
                    operation.Message = operation.message.Remove(0, startIndex + 1); ;
                }
                else
                    operation.Message = string.Empty;
                operation.DeleteMessage = ContentsBox.Lines[line];
            }
            else
            {

            }
            Push_Operation();

            startDelete = false;
        }
        // Operation 속성을 '/' 로 구분짓다
        private void Combine_Operation_Info(List<string> property_list, ref string split_str)
        {
            int i = 0;
            foreach (string str in property_list)
            {
                split_str += str;
                if (i != property_list.Count - 1)
                    split_str += "/";
                i++;
            }
        }

        private void Refine_Operation_Info(ref string split_str)
        {
            int operation_mode = old_operation.mode;
            List<string> operation_property = new List<string>();

            if (operation_mode == (int)OperatorSignal.INSERT) // 태환 <- 형섭 추가
            {
                // id_performed_clnt,mode,starting_line_number,work_zone_name,message
                // INSERT Operation 에 필요한 속성을 추가한다.
                operation_property.Add(Convert.ToString(old_operation.id_performed_clnt));
                operation_property.Add(Convert.ToString(old_operation.mode));
                operation_property.Add(Convert.ToString(old_operation.Starting_Line_Number));
                operation_property.Add(old_operation.work_zone_name);
                operation_property.Add(old_operation.message);
                //
                operation_property.Add(Convert.ToString(old_operation.Ending_Line_Number));
                operation_property.Add(Convert.ToString(old_operation.Ending_Index_Number));
                operation_property.Add(Convert.ToString(old_operation.Starting_Index_Number));

                // operation 속성들을 '/' 로 구분지어 string 형식으로 묶는다.
                Combine_Operation_Info(operation_property, ref split_str);

                Console.WriteLine("---------------------[서버 => INSERT]--------------------------");
                Console.WriteLine($"id : {old_operation.id_performed_clnt} ,mode :{old_operation.mode}");
                Console.WriteLine($"starting_line_number : {old_operation.starting_line_number}");
                Console.WriteLine($"work_zone_name : {old_operation.work_zone_name}");
                Console.WriteLine($"message : {old_operation.message}");
                Console.WriteLine($"ending_line_number : {old_operation.ending_line_number}");
                Console.WriteLine($"ending_index_number : {old_operation.ending_index_number}");
                Console.WriteLine($"starting_index_number : {old_operation.starting_index_number}");
                Console.WriteLine($"split_str : {split_str}");
                Console.WriteLine("-------------------------------------------------------------------");
            }
            else if (operation_mode == (int)OperatorSignal.DELETE || operation_mode == (int)OperatorSignal.BACK)
            {
                operation_property.Add(Convert.ToString(old_operation.id_performed_clnt));
                operation_property.Add(Convert.ToString(old_operation.mode));
                operation_property.Add(Convert.ToString(old_operation.starting_line_number));
                operation_property.Add(Convert.ToString(old_operation.starting_index_number));
                operation_property.Add(Convert.ToString(old_operation.ending_line_number));
                operation_property.Add(Convert.ToString(old_operation.ending_index_number));
                operation_property.Add(old_operation.message);
                operation_property.Add(old_operation.deletemessage);
                operation_property.Add(old_operation.work_zone_name);

                Combine_Operation_Info(operation_property, ref split_str);

                Console.WriteLine("---------------------[서버 => DELETE]--------------------------");
                Console.WriteLine($"id : {old_operation.id_performed_clnt} ,mode :{old_operation.mode}");
                Console.WriteLine($"work_zone_name : {old_operation.work_zone_name}");
                Console.WriteLine($"starting_line_number : {old_operation.starting_line_number}");
                Console.WriteLine($"ending_line_number : {old_operation.ending_line_number}");
                Console.WriteLine($"split_str : {split_str}");
                Console.WriteLine("-------------------------------------------------------------------");
            }
            else if (operation_mode == (int)OperatorSignal.ENTER)
            {
                operation_property.Add(Convert.ToString(old_operation.id_performed_clnt));
                operation_property.Add(Convert.ToString(old_operation.mode));
                operation_property.Add(Convert.ToString(old_operation.Starting_Line_Number));
                operation_property.Add(Convert.ToString(old_operation.Starting_Index_Number));
                operation_property.Add(Convert.ToString(old_operation.Ending_Line_Number));
                operation_property.Add(old_operation.work_zone_name);

                Combine_Operation_Info(operation_property, ref split_str);

                Debug.WriteLine("---------------------[서버 => ENTER]--------------------------");
                Debug.WriteLine($"id: { old_operation.id_performed_clnt}");
                Debug.WriteLine($"Mode : {old_operation.mode}");
                Debug.WriteLine($"Starting_Line_Number : {old_operation.Starting_Line_Number}");
                Debug.WriteLine($"Starting_Index_Number : {old_operation.Starting_Index_Number}");
                Debug.WriteLine($"Ending_Line_Number : {old_operation.Ending_Line_Number}");
                Debug.WriteLine($"work_zone_name : {old_operation.work_zone_name}");
                Debug.WriteLine($"split_str : {split_str}");
                Debug.WriteLine("-------------------------------------------------------------------");
            }
        }

        private void operationTimer_Tick(object sender, EventArgs e)
        {
            // 현재 커서가 위치하는 라인
            int current_line_loc = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);
            // 현재 커서가 위치하는 라인의 인덱스
            int current_index_loc = ContentsBox.SelectionStart - ContentsBox.GetFirstCharIndexFromLine(current_line_loc);
            // 서버측으로 전달할 Operation 정보를 담은 문자열
            string split_str = null;

            // Insert Operation을 큐에 넣을 조건
            if (operation.mode == (int)OperatorSignal.INSERT
                && stopwatch.Elapsed.TotalMilliseconds / 1000 >= TIME_LIMIT && !_IsEOF) // 태환 <- 형섭 추가
            {
                Console.WriteLine("Push Operation()");
                Push_Operation();
            }
            else if (operation.mode == (int)OperatorSignal.DELETE)
            {

            }
            else if (operation.mode == (int)OperatorSignal.ENTER)
            {

            }

            // Operation 큐의 Operation을 서버측에 하나씩 전달할 조건
            if (Task.Count > 0 && acknowledge == true)
            {
                // 제일 앞의 Operation이 모든 클라의 TextBox에 적용되기 전까지 잠근다. 
                acknowledge = false;

                // 작업의 모드에 따라 필요한 Operation 데이터를 구별하여 split_str에 문자열형식으로 추가한다.

                switch (old_operation.mode)
                {
                    case (int)OperatorSignal.INSERT:
                        // 입력중인 라인의 전체 택스트를 메시지로 설정
                        if (old_operation.Starting_Line_Number != old_operation.Ending_Line_Number && old_operation.Ending_Line_Number != 0) // 태환 <- 형섭 추가
                        {
                            old_operation.message = ContentsBox.Lines[old_operation.Starting_Line_Number];
                            old_operation.message += ContentsBox.Lines[old_operation.Starting_Line_Number + 1];
                            Debug.WriteLine("operationTimer_Tick -> INSERT -> if => old_operation.message: " + old_operation.message);
                        }
                        else
                        {
                            old_operation.message = ContentsBox.Lines[old_operation.Starting_Line_Number];
                            Debug.WriteLine("operationTimer_Tick -> INSERT -> else => old_operation.message: " + old_operation.message);
                        }
                        // 서버측에서 받는 형식으로 Operation 데이터를 재정의한다.
                        Refine_Operation_Info(ref split_str);
                        break;
                    case (int)OperatorSignal.BACK:
                    case (int)OperatorSignal.DELETE:
                        // 서버측에서 받는 형식으로 Operation 데이터를 재정의한다.
                        Refine_Operation_Info(ref split_str);
                        break;
                    case (int)OperatorSignal.ENTER:
                        Debug.WriteLine("operationTimer_Tick -> ENTER");
                        Refine_Operation_Info(ref split_str);
                        break;
                    default:
                        break;
                }
                // 작업을 서버에 전달한다.
                TCP.Write_Operation_To_Server(split_str);
                ContentsBox.Invalidate(); //준혁 추가
            }
        }

        public void Push_Operation()
        {
            Operation temp = new Operation();
            old_operation = new Operation();
            // 새 Operation 을 위해 시간측정 초기화
            stopwatch.Reset();

            // 기존에 작성된 Operation을 큐에 넣는다. 
            // --> 큐안의 Operation은 operationTimer_Tick 에서 서버측으로 전달된다.
            old_operation = operation.DeepCopy();
            Task.Enqueue(old_operation);
            operation.InitializeOperation();

            Console.WriteLine($"초기화 후 mode : {operation.mode}");
            Console.WriteLine($"초기화 후 starting_line_number : {operation.starting_line_number}");
            Console.WriteLine($"초기화 후 message : {operation.message}");
        }

        // Read용(operation,acknowledge) Thread
        private void queThread()
        {
            while (true)
            {
                //Debug.WriteLine("private void queThread()");
                READ.queThread(ref Task, ref operation, ref acknowledge, ref WRITE);
                Show_Rectangle_on_Screen(0); //준혁 추가
            }
        }
        private void Show_Rectangle_on_Screen(int temp)
        {
            temp = 0;

            if (ContentsBox.InvokeRequired)
            {
                var d = new CrossThreadDelegate_Cursor(Show_Rectangle_on_Screen);
                Invoke(d, new object[] { temp });
            }
            else
            {
                Refresh();
                foreach (CursorInfo cur in cursors)
                {
                    if (cur.line != -1)
                    {
                        int index = ContentsBox.GetFirstCharIndexFromLine(cur.line);
                        Point point = ContentsBox.GetPositionFromCharIndex(index);
                        Console.WriteLine("cur.line: {0}", cur.line);
                        Rectangle rect = CreateRectangle(point);
                        DrawRectangle(rect, cur.caret.BackColor);
                    }
                }
            }
        }
        private Rectangle CreateRectangle(Point point)
        {
            Rectangle rec = new Rectangle(ContentsBox.Location.X, point.Y, ContentsBox.Width, ContentsBox.Font.Height + 1);
            return rec;
        }
        private void DrawRectangle(Rectangle rect, Color color)
        {
            Pen p = new Pen(color);
            Graphics g = ContentsBox.CreateGraphics();
            g.DrawRectangle(p, rect);
        }
        // 파일 -> 저장
        private void btnDownload_Click(object sender, EventArgs e)
        {
            SaveText(); //저장 메서드 호출
        }

        private void SaveText()
        {
            DialogResult objDr = saveFileDialog1.ShowDialog();

            if (objDr != DialogResult.Cancel)
            {
                string strFileName = saveFileDialog1.FileName;
                SaveFile(strFileName);
            }
        }

        private void SaveFile(string strFileName)
        {
            StreamWriter objSw = new StreamWriter(strFileName);

            objSw.Write(this.ContentsBox.Text);
            objSw.Flush();
            objSw.Close();
            //_IsTextChanged = false;
            this.Text = strFileName; //제목에 파일명 출력
        }

        //================================================
        //          Cursor 관련 메서드
        //================================================

        // 커서 정보 Read하는 쓰레드
        private void Cursor_Read_Thread()
        {
            while (isCursorRead)
            {
                string temp = cursor_read.Read_CursorInfo();
                if (temp == "") break;

                string[] cursor_info = temp.Split('/');
                int signal = Int32.Parse(cursor_info[0]);
                int id = Int32.Parse(cursor_info[1]);
                int X = Int32.Parse(cursor_info[2]);
                int Y = Int32.Parse(cursor_info[3]);
                string color = cursor_info[4];
                int line = Int32.Parse(cursor_info[5]);

                switch (signal)
                {
                    case (int)CURSOR.INSERT:
                        cursors.Add(new CursorInfo(id, X, Y, color, this.ContentsBox.Font.Height, line));
                        Controls_Add_Cursor(0);
                        break;

                    case (int)CURSOR.DELETE:
                        for (int i = 0; i < cursors.Count; i++)
                        {
                            if (cursors[i].id == id)
                            {
                                //Console.WriteLine("삭제할 커서 id [{0}]", cursors[i].id);
                                Controls_Remove_Cursor(i);
                                cursors.RemoveAt(i);                               
                                break;
                            }
                        }
                        break;

                    case (int)CURSOR.UPDATE:
                        for (int i = 0; i < cursors.Count; i++)
                        {
                            if (cursors[i].id == id)
                            {
                                cursors[i].X = X;
                                cursors[i].Y = Y;
                                cursors[i].line = line;                                
                                break;
                            }
                        }
                        break;
                }
                // 작업중인 파일에 다른사람 커서 그리기 
                Drow_Cursors(0);
            }
        }
        
        private void SetStartingOperation(int selectkey) //준혁
        {
            int line = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);
            startIndex = ContentsBox.SelectionStart - ContentsBox.GetFirstCharIndexFromLine(line);

            operation.Mode = selectkey;
            if (selectkey == (int)OperatorSignal.BACK)
            {
                operation.Starting_Line_Number = line;
                operation.Starting_Index_Number = startIndex;
                operation.Message = ContentsBox.Lines[line];
                startLength = ContentsBox.Lines[line].Length;
            }
            else
                operation.Ending_Line_Number = line;

            startDelete = true;
        }

        private void Sequential_InsertAndEnter()
        {
            int line = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);
            int index = ContentsBox.SelectionStart - ContentsBox.GetFirstCharIndexFromLine(line);

            operation.ending_line_number = line + 1;
            operation.ending_index_number = index;
        }

        private void ExecuteEOF(int Judge)
        {
            int line = ContentsBox.GetLineFromCharIndex(ContentsBox.SelectionStart);
            int index = ContentsBox.SelectionStart - ContentsBox.GetFirstCharIndexFromLine(line);

            if (Judge == 1)
            {
                EOF = true;
                operation.mode = (int)OperatorSignal.ENTER;
                operation.starting_line_number = line;
                operation.starting_index_number = index;
            }
            else if (Judge == 2)
            {
                EOF = false;
                operation.mode = (int)OperatorSignal.ENTER;
                operation.ending_line_number = line;
            }
        }
       
        // Control에 커서 라벨 추가
        private void Controls_Add_Cursor(int temp)
        {
            temp = 0;

            if (ContentsBox.InvokeRequired)
            {
                var d = new CrossThreadDelegate_Cursor(Controls_Add_Cursor);
                Invoke(d, new object[] { temp });
            }
            else
            {
                Controls.Add(cursors.Last().caret);
                cursors.Last().caret.BringToFront();
            }
        }

        // Control에서 커서 라벨 삭제
        private void Controls_Remove_Cursor(int index)
        {
            if (ContentsBox.InvokeRequired)
            {
                var d = new CrossThreadDelegate_Cursor(Controls_Remove_Cursor);
                Invoke(d, new object[] { index });
            }
            else
            {
                Controls.Remove(cursors[index].caret);
            }
        }

        // 작업중인 파일에 다른 사람 커서 그리기 
        private void Drow_Cursors(int temp)
        {
            temp = 0;

            if (ContentsBox.InvokeRequired)
            {
                var d = new CrossThreadDelegate_Cursor(Drow_Cursors);
                Invoke(d, new object[] { temp });
            }
            else
            {
                foreach (CursorInfo cur in cursors)
                {
                    cur.caret.Location = new Point(ContentsBox.Location.X + cur.X + 2, ContentsBox.Location.Y + cur.Y + 2);
                }
            }
        }
    }

    // 커서 정보 담는 클래스
    class CursorInfo
    {
        public int id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Label caret;
        public int line { get; set; }

        public CursorInfo(int id, int X, int Y, string color, int font_height, int line)
        {
            this.id = id;
            this.X = X;
            this.Y = Y;
            this.line = line;
            caret = new Label();
            caret.BackColor = Color.FromName(color);
            caret.Size = new Size(2, font_height + 3);
        }
    }
    
    // 점유 라인 정보 담는 클래스
    class LineInfo
    {
        public int my_position_line { get; set; }

        public LineInfo()
        {
            my_position_line = -1;
        }
    }
}
