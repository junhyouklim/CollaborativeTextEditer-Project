using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TextEditor
{
    // 통합
    class MainForm_Tcp
    {
        const string serverIP = "10.10.20.58"; // 서버 IP
        const int serverPort = 9555; // 서버와 통신할 Port
        IPEndPoint serv_addr;
        TcpClient clnt;
        NetworkStream stream; // read용 서버와 데이터를 주고 받을 스트림 객체 
        const int BUF = 256;

        enum MAIN_SIG { CREATE_FILE, DELETE_FILE, PROC_CHECK };
        enum FILE_CHANGED { SUCCESS = 1, FAIL };
        
        // 클라이언트 세팅
        public MainForm_Tcp()
        {
            // 민근 
           // const string bindIP = "10.10.20.36"; // 실습실 
            // const string bindIP = "192.36.90.240"; // 기숙사 

            // 형섭
            //const string bindIP = "10.10.20.35"; // 실습실 
            // const string bindIP = "192.36.90.241"; // 기숙사 

            // 태환
            // const string bindIP = "10.10.20.42"; // 실습실 
            //const string bindIP = "192.36.90.239"; // 기숙사 

            // 준혁
             const string bindIP = "10.10.20.55"; // 실습실 
            //const string bindIP = "192.36.90.237"; // 기숙사

            const int bindPort = 0;

            IPEndPoint clnt_addr = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);
            clnt = new TcpClient(clnt_addr);           
        }

        // 소켓 닫기
        ~MainForm_Tcp()
        {
            stream.Close();
            clnt.Close();
        }

        // 서버에 접속 
        public void Connect_To_Server(int type)
        {
            serv_addr = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            clnt.Connect(serv_addr);
            stream = clnt.GetStream();

            byte[] data = BitConverter.GetBytes(type);
            stream.Write(data, 0, data.Length);
        }

        //================================================
        //          MainForm 관련 메서드 
        //================================================
        
        // 파일명 목록 받기 
        public void Get_FileNames(ref string files)
        {
            byte[] data = new byte[BUF];
            int bytes = stream.Read(data, 0, data.Length);
            files = Encoding.UTF8.GetString(data, 0, bytes);
            //Console.WriteLine("수신내용 : [{0}]  사이즈 : {1}", files, bytes);
        }

        // 새파일 생성
        public bool Create_File(string name)
        {
            int signal = (int)MAIN_SIG.CREATE_FILE;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);

            // 생성할 파일명 보냄 
            data = Encoding.UTF8.GetBytes(name);
            stream.Write(data, 0, data.Length);

            // 결과 받음
            data = new byte[sizeof(int)];
            stream.Read(data, 0, data.Length);
            int result = BitConverter.ToInt32(data, 0);

            if (result == (int)FILE_CHANGED.SUCCESS)
                return true;
            else
                return false;
        }   

        // 선택한 파일 삭제
        public bool Delete_File(string name)
        {
            int signal = (int)MAIN_SIG.DELETE_FILE;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);

            // 삭제할 파일명 보냄 
            data = Encoding.UTF8.GetBytes(name);
            stream.Write(data, 0, data.Length);

            // 결과 받음
            data = new byte[sizeof(int)];
            stream.Read(data, 0, data.Length);
            int result = BitConverter.ToInt32(data, 0);

            if (result == (int)FILE_CHANGED.SUCCESS)
                return true;
            else
                return false;
        }       

        // EditForm 연결할 서버 포트 번호 받기 
        public int Get_ProcessServer_Port(string file_name)
        {
            int signal = (int)MAIN_SIG.PROC_CHECK;
            byte[] data = BitConverter.GetBytes(signal);
            stream.Write(data, 0, data.Length);

            // 오픈할 파일명 전송 
            data = Encoding.UTF8.GetBytes(file_name);
            stream.Write(data, 0, data.Length);

            // 접속해야할 포트번호 받음 
            data = new byte[sizeof(int)];
            stream.Read(data, 0, data.Length);

            int port = BitConverter.ToInt32(data, 0);

            return port;
        }
    }
}
