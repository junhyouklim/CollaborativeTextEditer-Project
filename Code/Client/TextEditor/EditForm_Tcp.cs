using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace TextEditor
{
    class EditForm_Tcp
    {
        const string serverIP = "10.10.20.58"; // 서버 IP       
        IPEndPoint serv_addr;
        TcpClient clnt;
        NetworkStream stream; // read용 서버와 데이터를 주고 받을 스트림 객체 
        const int BUF = 256;
        const int CURSOR_BUF = 30;
        //
        LineInfo row = new LineInfo(); // 형섭 추가
        List<CursorInfo> cursors = new List<CursorInfo>();
        //
        enum EDIT_SIG { OPEN, NEW_OPERATION, RECEIVE_ACK_FROM_CLNT, REQUEST_CURSOR, SEND_CURSOR, DISCONNECT, FIANL_ACK_CHECK };
        enum THREAD { OBJ = 1, VAR = 2, NEW_CLNT = 3, CLNT_JOIN = 4 };
        enum VAR { FAIL, SUCCESS };
        enum OperatorSignal { DEFAULT = 0, INSERT, BACK, DELETE, ENTER }
        enum SELECTIONKEY { BACKSPACE = 1, DELETE }

        // main form 이외의 스레드에서 EditForm을 조작하기 위해 선언
        EditForm edit_form = null;

        // 클라이언트 세팅
        public EditForm_Tcp(EditForm edit = null)
        {
            // 민근 
            //const string bindIP = "10.10.20.36"; // 실습실 
            // const string bindIP = "192.36.90.240"; // 기숙사 

            // 형섭
            // const string bindIP = "10.10.20.35"; // 실습실 
            // const string bindIP = "192.36.90.241"; // 기숙사 

            // 태환
            //const string bindIP = "10.10.20.42"; // 실습실 
            //const string bindIP = "192.36.90.239"; // 기숙사 

            // 준혁
             const string bindIP = "10.10.20.55"; // 실습실 
            // const string bindIP = "192.36.90.237"; // 기숙사

            const int bindPort = 0;

            IPEndPoint clnt_addr = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);
            clnt = new TcpClient(clnt_addr);

            edit_form = edit;
        }
        
        public EditForm_Tcp(EditForm edit, ref List<CursorInfo> c, ref LineInfo r)
        {
            // 민근 
            //const string bindIP = "10.10.20.36"; // 실습실 
            // const string bindIP = "192.36.90.240"; // 기숙사 

            // 형섭
            //const string bindIP = "10.10.20.35"; // 실습실 
            // const string bindIP = "192.36.90.241"; // 기숙사 

            // 태환
            //const string bindIP = "10.10.20.42"; // 실습실 
            //const string bindIP = "192.36.90.239"; // 기숙사 

            // 준혁
            const string bindIP = "10.10.20.55"; // 실습실 
            //const string bindIP = "192.36.90.237"; // 기숙사

            const int bindPort = 0;

            IPEndPoint clnt_addr = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);
            clnt = new TcpClient(clnt_addr);

            edit_form = edit;
            cursors = c;
            row = r;
        }

        // 소켓 닫기
        ~EditForm_Tcp()
        {
            stream.Close();
            clnt.Close();
        }

        // 서버에 접속 
        public void Connect_To_Server(int type, int serverPort)
        {
            serv_addr = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            clnt.Connect(serv_addr);
            stream = clnt.GetStream();

            byte[] data = BitConverter.GetBytes(type);
            stream.Write(data, 0, data.Length);
        }

        //================================================
        //          EditForm 관련 메서드 
        //================================================

        // 클라 연결 끊김 시그널 전송 
        public void Send_Disconnect_Signal()
        {
            int signal = (int)EDIT_SIG.DISCONNECT;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);
        }

        // 서버에서 배정한 id 받기
        public void Get_ID(ref int id)
        {
            byte[] data = new byte[sizeof(int)];
            stream.Read(data, 0, data.Length);
            id = BitConverter.ToInt32(data, 0);
            //Console.WriteLine("받은 ID [{0}]", id);
        }

        public void Wait_For_Sync()
        {
            int check = 0;
            // 대기 메시지 박스         
            byte[] data = new byte[sizeof(int)];
            stream.Read(data, 0, data.Length);
            check = BitConverter.ToInt32(data, 0);
            // 대기 메시지 박스 해제
        }


        // 선택한 파일 열기(내용 받기)
        public void Open_File(string file, ref string contents)
        {
            //Console.WriteLine("오픈할 파일명 : [{0}]", file);
            int signal = (int)EDIT_SIG.OPEN;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);

            data = System.Text.Encoding.UTF8.GetBytes(file);
            stream.Write(data, 0, data.Length);
            // Console.WriteLine("파일명 : {0}, len : {1}", data.ToString(), data.Length);

            data = new byte[sizeof(int)];
            stream.Read(data, 0, data.Length);
            int size = BitConverter.ToInt32(data, 0);

            data = new byte[size];
            int bytes = stream.Read(data, 0, data.Length);
            string temp = Encoding.UTF8.GetString(data, 0, bytes);

            // Console.WriteLine("temp : {0} bytes : {1}", temp, bytes);            
            contents = temp;
        }

        // 같은파일 작업하고있는 클라들의 커서 정보 서버에 요청 
        public void Request_All_CursorInfo()
        {
            int signal = (int)EDIT_SIG.REQUEST_CURSOR;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);

            Console.WriteLine("서버에 커서 정보 요청");
        }

        // 변경된 내 커서 정보 서버에 전송 
        public void Send_Cursor_Info(int X, int Y, int my_position_line)
        {
            string str = "";
            int signal = (int)EDIT_SIG.SEND_CURSOR;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);

            data = new byte[CURSOR_BUF];           
            str = X.ToString() + "/" + Y.ToString() + "/" + my_position_line.ToString();
            data = Encoding.UTF8.GetBytes(str);
            stream.Write(data, 0, data.Length);
        }

        //================================================
        //          Operation 관련 메서드 
        //================================================
        // operation을 서버에 전달한다.
        public void Write_Operation_To_Server(string operation_str)
        {
            // operation 전달신호 : 1
            int signal = (int)EDIT_SIG.NEW_OPERATION;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);

            // Operation 정보가 담긴 문자열데이터 전달
            byte[] operation_data = System.Text.Encoding.UTF8.GetBytes(operation_str);
            stream.Write(operation_data, 0, operation_data.Length);
        }
        //
        public string[] Sequential_InsertAndEnter(RichTextBox item, Operation op, string[] lines) // 텍스트 파일 받아올 때 2줄로 시작...
        {
            string EOF = lines[op.starting_line_number].Insert(op.ending_index_number, "/");
            string[] Divide = EOF.Split("/".ToCharArray());

            string[] rebuilding = new string[lines.Length + 1];
            Array.Copy(lines, 0, rebuilding, 0, lines.Length);

            rebuilding[op.starting_line_number] = Divide[0];
            rebuilding[op.starting_line_number + 1] = Divide[1];

            Array.Copy(lines, op.starting_line_number + 1, rebuilding, op.starting_line_number + 2, rebuilding.Length - (op.starting_line_number + 2));

            return rebuilding;
        }
        
        delegate void SettingDele(RichTextBox item, Operation operation, ref EditForm_Tcp operation_write);
        
        public void Set_Operation_TextBox(RichTextBox item, Operation operation, ref EditForm_Tcp operation_write)
        {
            Debug.WriteLine("Set_Operation_TextBox 모드 => " + operation.mode + ", operation.Ending_Line_Number => " + operation.Ending_Line_Number);
            // TextBox에 operation 적용전 내 커서가 위치한 라인
            int current_line = item.GetLineFromCharIndex(item.SelectionStart);
            // 해당 라인의 몇번째 인덱스
            int current_index = item.SelectionStart - item.GetFirstCharIndexFromLine(current_line);

            string[] lines;
            // Operation적용전 사용자의 입력을 방지
            //item.ReadOnly = true;
            lines = item.Lines;
            // 읽은 Operation의 mode에 맞게 textbox에 적용한다.
            if (operation.mode == (int)OperatorSignal.DEFAULT)
                return;
            else if (operation.mode == (int)OperatorSignal.INSERT)
            {
                int cur = item.SelectionStart;
                if (operation.Starting_Line_Number != operation.Ending_Line_Number && operation.Ending_Line_Number != 0)
                {
                    int point = edit_form.ContentsBox.SelectionStart; // 현재 작업 중인 커서의 인덱스...
                    int line = edit_form.ContentsBox.GetLineFromCharIndex(edit_form.ContentsBox.SelectionStart); // 현재 작업 중인 커서의 줄...

                    Console.WriteLine($"총 TextBox 라인길이 : {item.Lines.Length}");
                    Console.WriteLine($"입력된 라인번호 : {operation.Starting_Line_Number}");

                    lines[operation.Starting_Line_Number] = operation.message;
                    //Debug.WriteLine("메세지 : " + lines[operation.Starting_Line_Number]);

                    if (operation.starting_line_number < row.my_position_line && row.my_position_line != -1)
                    {
                        row.my_position_line += 1;
                    }

                    foreach (CursorInfo prime in cursors)
                    {
                        if (row.my_position_line - 1 < prime.line && row.my_position_line != -1)
                        {
                            prime.line += 1;
                        }
                    }

                    lines = Sequential_InsertAndEnter(item, operation, lines);
                    Refresh_The_Screen(lines, operation, point, line);
                }
                else
                {
                    Console.WriteLine($"총 TextBox 라인길이 : {item.Lines.Length}");
                    Console.WriteLine($"입력된 라인번호 : {operation.Starting_Line_Number}");

                    lines[operation.Starting_Line_Number] = operation.message;
                    Debug.WriteLine("메세지 : " + lines[operation.Starting_Line_Number]);

                    item.Lines = lines;

                    // 커서위치를 조정한다.
                    if (operation.starting_line_number > current_line)
                    {
                        item.SelectionStart = cur;
                    }
                    else if (operation.starting_line_number == current_line)
                    {
                        //
                    }
                    else if (operation.starting_line_number < current_line)
                    {
                        item.SelectionStart = cur + (operation.ending_index_number - operation.starting_index_number);
                    }

                    Console.WriteLine($"cur : {cur}");
                    Console.WriteLine($"operation.ending_index_number : {operation.ending_index_number}");
                    Console.WriteLine($"operation.starting_index_number : {operation.starting_index_number}");
                    //item.Focus();
                }
            }
            else if (operation.mode == (int)OperatorSignal.ENTER)
            {
                int point = edit_form.ContentsBox.SelectionStart; // 현재 작업 중인 커서의 인덱스...
                int line = edit_form.ContentsBox.GetLineFromCharIndex(edit_form.ContentsBox.SelectionStart); // 현재 작업 중인 커서의 줄...

                if (operation.starting_line_number < row.my_position_line && row.my_position_line != -1)
                {
                    row.my_position_line += 1;
                }

                foreach (CursorInfo prime in cursors)
                {
                    if (row.my_position_line -1 < prime.line && row.my_position_line != -1)
                    {
                        prime.line += 1;
                    }
                }

                lines = shiftEOF(item, operation);
                Refresh_The_Screen(lines, operation, point, line);
            }
            else if (operation.mode == (int)OperatorSignal.BACK || operation.mode == (int)OperatorSignal.DELETE) // 삭제
            {
                int curindex = item.SelectionStart;
                int curline = item.GetLineFromCharIndex(item.SelectionStart); // 현재 작업 중인 커서의 줄.
                Console.WriteLine("Delete in");
                Refresh_The_Screen(item, operation, lines);

                item.SelectionStart = SetCusorLocation(operation, curline, curindex);
            }
            // Operation 내용대로 TextBox에 적용됐다면 서버에 ack신호를 전달한다.
            int signal = (int)EDIT_SIG.RECEIVE_ACK_FROM_CLNT;
            byte[] data = BitConverter.GetBytes(signal);
            operation_write.stream.Write(data, 0, data.Length);

            //item.ReadOnly = false;
        }

        public void Refresh_The_Screen(RichTextBox item, Operation operation, string[] lines)
        {
            Console.WriteLine("Refresh_The_Screen in");
            int startline = operation.Starting_Line_Number;
            int endline = operation.Ending_Line_Number;
            int totalLength = item.Lines.Length;
            Console.WriteLine("startline:{0}", startline);
            Console.WriteLine("endline:{0}", endline);
            if (operation.deletemessage != null)
            {
                if (startline != endline)
                {
                    lines = SetDeleteLines(startline, totalLength, lines);
                }

                lines[endline] = operation.deletemessage;

                item.Lines = lines;
            }
        }
        private string[] SetDeleteLines(int startline, int totalLength, string[] lines)
        {
            for (int i = startline; i < totalLength - 1; i++)
            {
                lines[i] = lines[i + 1];
            }
            return lines;
        }
        private int SetCusorLocation(Operation operation, int cline, int cindex)
        {
            int index = 0;
            int deletecount = operation.starting_index_number - operation.ending_index_number;
            if (operation.ending_line_number < cline)
            {
                index = cindex - deletecount;
            }
            else if (operation.ending_line_number > cline)
            {
                index = cindex;
            }

            return index;
        }
        private static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }

        delegate void WaitForInput(RichTextBox item);

        public void SetClientInputWait(RichTextBox richTextBox)
        {
            // 입력을 막는다. 
            richTextBox.ReadOnly = true;
            // 클라이언트 입장 메시지박스를 띄운다. 
            AutoClosingMessageBox.Show("새 유저 입장...동기화중...", "알림", 500);
        }

        public void SetClientInputUnlock(RichTextBox richTextBox)
        {
            richTextBox.ReadOnly = false;
            MessageBox.Show("동기화 완료!");
        }

        public void queThread(ref Queue<Operation> Task, ref Operation current_op, ref bool acknowledge, ref EditForm_Tcp WRITE)
        {
            Operation operation_to_received = new Operation();

            // 시그널을 전달받음 
            byte[] data = new byte[sizeof(int)];
            stream.Read(data, 0, data.Length);
            int result = BitConverter.ToInt32(data, 0);
 
            string oper_info_received;
            string[] oper_info_array;

            if (result == (int)THREAD.OBJ)
            {
                // 다른 클라이언트의 Operation을 읽는다.
                byte[] data2 = new byte[512];

                System.Array.Clear(data2, 0, data2.Length);

                int bytes = stream.Read(data2, 0, data2.Length);
                oper_info_received = Encoding.UTF8.GetString(data2, 0, bytes);

                // '/'를 기준으로 Operation의 속성을 구분한다.
                oper_info_array = oper_info_received.Split('/');

                Debug.WriteLine("Int32.Parse(oper_info_array[1]): " + Int32.Parse(oper_info_array[1]));
                // 구분된 operation의 mode에 맞게 Operation객체에 속성을 저장한다.
                switch (Int32.Parse(oper_info_array[1]))
                {
                    // 메세지, 메세지가 추가 될 라인번호
                    case (int)OperatorSignal.INSERT:
                        operation_to_received.Mode = (int)OperatorSignal.INSERT;
                        operation_to_received.Starting_Line_Number = Int32.Parse(oper_info_array[2]);
                        operation_to_received.Message = oper_info_array[4];
                        operation_to_received.Ending_Line_Number = Int32.Parse(oper_info_array[5]);
                        operation_to_received.Ending_Index_Number = Int32.Parse(oper_info_array[6]);
                        operation_to_received.Starting_Index_Number = Int32.Parse(oper_info_array[7]);

                        if (operation_to_received.Starting_Line_Number < current_op.Starting_Line_Number)
                        {
                            if (current_op.mode == (int)OperatorSignal.INSERT && operation_to_received.Starting_Line_Number != operation_to_received.Ending_Line_Number && operation_to_received.Ending_Line_Number != 0)
                            {
                                current_op.Starting_Line_Number += 1;
                            }
                        }
                        break;
                    case (int)OperatorSignal.DELETE:
                    case (int)OperatorSignal.BACK:
                        operation_to_received.Mode = Int32.Parse(oper_info_array[1]);
                        operation_to_received.Starting_Line_Number = Int32.Parse(oper_info_array[2]);
                        operation_to_received.Starting_Index_Number = Int32.Parse(oper_info_array[3]);
                        operation_to_received.Ending_Line_Number = Int32.Parse(oper_info_array[4]);
                        operation_to_received.Ending_Index_Number = Int32.Parse(oper_info_array[5]);
                        operation_to_received.Message = oper_info_array[6];
                        operation_to_received.DeleteMessage = oper_info_array[7];
                        break;
                    case (int)OperatorSignal.ENTER:
                        operation_to_received.id_performed_clnt = Int32.Parse(oper_info_array[0]);
                        operation_to_received.Mode = (int)OperatorSignal.ENTER;
                        operation_to_received.Starting_Line_Number = Int32.Parse(oper_info_array[2]);
                        operation_to_received.Starting_Index_Number = Int32.Parse(oper_info_array[3]);
                        operation_to_received.Ending_Line_Number = Int32.Parse(oper_info_array[4]);

                        if (operation_to_received.Starting_Line_Number < current_op.Starting_Line_Number)
                        {
                            if (current_op.mode == (int)OperatorSignal.INSERT)
                            {
                                current_op.Starting_Line_Number += 1;
                            }
                        }
                        break;
                }

                if (edit_form.ContentsBox.InvokeRequired)
                {
                    SettingDele dele = Set_Operation_TextBox;
                    edit_form.ContentsBox.Invoke(dele, edit_form.ContentsBox, operation_to_received, WRITE);
                }
                else
                {
                }
            }
            else if (result == (int)THREAD.VAR)
            {
                int signal = (int)EDIT_SIG.FIANL_ACK_CHECK;
                byte[] data2 = BitConverter.GetBytes(signal);
                WRITE.stream.Write(data2, 0, data2.Length);
                Delay(250);

                Task.Dequeue();
                acknowledge = true;
            }
            // 입장하는 클라이언트의 동기화 작업이 끝날 때까지 입력중단
            else if (result == (int)THREAD.NEW_CLNT)
            {
                // 입력을 못하게 한다. 
                Console.WriteLine($"[새로운 클라이언트 접속시도중,,,입력이 제한됨]");
                // 클라이언트 입장중 대기 메시지박스
                if (edit_form.ContentsBox.InvokeRequired)
                {
                    WaitForInput del = SetClientInputWait;

                    edit_form.ContentsBox.Invoke(del, edit_form.ContentsBox);
                }
                else
                {
                    Console.WriteLine("??");
                }

            }
            else if (result == (int)THREAD.CLNT_JOIN)
            {
                // 새 클라이언트 입장완료 
                Console.WriteLine($"[클라이언트 접속 및 동기화 완료,,,입력제한 해제됨]");
                // 대기 매시지박스 자동종료
                if (edit_form.ContentsBox.InvokeRequired)
                {
                    WaitForInput del = SetClientInputUnlock;
                    edit_form.ContentsBox.Invoke(del, edit_form.ContentsBox);
                }
                else
                {
                    Console.WriteLine("!!");
                }
            }
        }
        //
        public void Refresh_The_Screen(string[] lines, Operation operatoin, int point, int line)
        {
            edit_form.ContentsBox.Lines = lines;

            if (operatoin.starting_line_number > line)
            {
                edit_form.ContentsBox.SelectionStart = point;
            }
            else if (operatoin.starting_line_number == line) // 같은 줄에서 작업 중일 경우...
            {
                if (operatoin.starting_index_number > point)
                {
                    edit_form.ContentsBox.SelectionStart = point;
                }
                else if (operatoin.starting_index_number <= point)
                {
                    edit_form.ContentsBox.SelectionStart = point + 1;
                }
            }
            else // operatoin.line_number 아랫 줄에서 작업 중일 경우...
            {
                edit_form.ContentsBox.SelectionStart = point + 1;
            }
            // edit_form.ContentsBox.ScrollToCaret();
        }
        
        public string[] shiftEOF(RichTextBox item, Operation op) // 텍스트 파일 받아올 때 2줄로 시작...
        {
            string[] copy = new string[edit_form.ContentsBox.Lines.Length];

            Array.Copy(edit_form.ContentsBox.Lines, 0, copy, 0, copy.Length);

            string EOF = copy[op.starting_line_number].Insert(op.starting_index_number, "/");
            string[] Divide = EOF.Split("/".ToCharArray());

            string[] lines;
            
            if (copy.Length < 2)
            {
                lines = new string[op.ending_line_number + 2];
            }
            else
            {
                lines = new string[copy.Length + (op.ending_line_number - op.starting_line_number)];
            }

            Array.Copy(copy, 0, lines, 0, copy.Length);

            lines[op.starting_line_number] = Divide[0];
            lines[op.starting_line_number + 1] = Divide[1];

            Array.Copy(copy, op.starting_line_number + 1, lines, op.starting_line_number + 2, lines.Length - (op.starting_line_number + 2));

            return lines;
        }

        //================================================
        //          커서 Read 관련 메서드 
        //================================================

        // 서버에서 보낸 커서 정보 받음
        public string Read_CursorInfo()
        {
            byte[] data = new byte[CURSOR_BUF];
            int bytes = stream.Read(data, 0, data.Length);
            string result = Encoding.UTF8.GetString(data, 0, bytes);
            // Console.WriteLine($"커서 정보 받음 [{result}]");
            return result;
        }
    }
}
