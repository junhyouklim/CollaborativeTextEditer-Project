#pragma once
#include "Common.h"
#include "MainForm.h"

class Server
{ 
private:
    int serv_sock; // 서버 소켓
    struct sockaddr_in serv_adr; // 주소 구조체 
    
    vector<int> main_socks; // 메인form 소켓
    vector<int> main_read; // main_read_sock 관리할 vector
    mutex main_mtx; // main_socks 변수 뮤텍스      

    vector<string> proc_files; // 현재 프로세스가 동작중인 .txt파일 목록
    vector<int> proc_ports; // 프로세스별 접속 PORT 
    int proc_port; // 프로세스에서 사용할 포트번호, 1씩 증가 
    
    int EditServ_sock; // Serv_Editform 소켓
   
public:  
    // 서버 관련 
     Server(); // 서버 소켓 설정
    ~Server(); // 서버 소켓 close
    void Server_On(); // listen 상태 시작 및 클라이언트 연결 처리   
    
    // 프로세스 생성, 종료 정보 받는 쓰레드
    void ProcessInfo_Thread(int clnt_sock);

    // MainForm 관련
    void MainForm_Thread(int clnt_sock); // 메인폼 쓰레드 생성
    int Get_VectorIndex(vector<int> socks, int sock); // Vector에서 자신의 인덱스 받아옴 
    int Get_VectorIndex(vector<string> proc_files, string file); // Vector에서 자신의 인덱스 받아옴 
    int Erase_VectorValue(vector<int> socks, int index); // Vector에서 해당 인덱스 erase
    string Erase_VectorValue(vector<string> str, int index); // Vector에서 해당 인덱스 erase

    // 공용
    void Client_Disconnect(int clnt_sock, int type); // 클라이언트 접속 종료 처리 
    void Error_Handling(string msg); // 에러시 출력 후 프로그램 종료
};