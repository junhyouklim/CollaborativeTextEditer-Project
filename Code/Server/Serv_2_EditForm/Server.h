#pragma once
#include "Common.h"
#include "Operation.h"
#include "EditForm.h"

class Server
{
private:
  // 메인 서버 연결 관련 변수
  int ProcSignal_sock;             // Serv_MainForm에 연결할 소켓, 프로세스 생성 Signal 받기용
  int ProcInfo_sock;               // 프로세스 생성, 종료 정보 전달 소켓
  struct sockaddr_in MainServ_adr; // Serv_MainForm 주소 구조체

  // 프로세스 생성 관련 변수
  int proc_port;                   // 프로세스에서 작동할 서버 포트
  string proc_file;                // 프로세스에서 작업중인 파일
  int ProcServ_sock;               // 프로세스 서버 소켓
  struct sockaddr_in ProcServ_adr; // 주소 구조체

  // 자식 프로세스에서 사용할 변수
  int clnt_id;               // 메모장 클라이언트 각자 갖는 고유 id 1부터 ++
  vector<EditorInfo> editor; // 메모장 클라이언트 정보관리 구조체
  mutex edit_mtx;            // editor 변수 뮤텍스

  vector<Operation> operations; // 모든 클라이언트가 수행한 operation 이 쌓이는 곳 -- 각 operation을 수행할 때 약간의 텀을 주기
  mutex operation_mtx;

  int ackCnt_to_receive; // 클라이언트로부터 acknowledge를 전달 받아야 할 개수 (현 작업파일에 접속한 총 클라이언트 수 - 1 )
  mutex ackCnt_mtx;

  bool final_ack_check = false;
  mutex final_check;

public:
  // 메인 서버 연결 관련 함수
  Server();                // 소켓 설정
  ~Server();               // 소켓 close
  void Connect_MainServ(); // Serv_MainForm에 클라이언트로 연결해서 프로세스 생성 Signal 받기위한 연결

  // 부모 프로세스 관련 함수
  bool Read_Process_Create_Signal(); // Serv_MainForm으로부터 생성 Signal 받음

  // 자식 프로세스 관련 함수
  void Send_Process_Create_Signal(); // 메인서버에 프로세스 성공 시그널 전송
  void Process_Server_On();          // 프로세스 서버 ON, 소켓 설정
  void Process_Server_Working();     // 클라이언트 accept, 서버 기능 함수

  // EditForm 관련 함수
  void EditForm_Thread(int clnt_sock); // 메모장폼 쓰레드 생성
  void New_EditorInfo(int clnt_sock);  // 새로운 EditorInfo 추가
  string Set_CursorColor(string file); // 같은 파일내에서 커서 색깔 다르게 배정
  int Get_VectorIndex(int sock);       // editor Vector에서 자신의 인덱스 받아옴
  void Erase_VectorValue(int index);   // editor Vector에서 해당 인덱스 erase

  // Operation 관련 함수 
  void Operation_Thread();
  void Acknowledge_Thread(int clnt_sock); // 클라에서 전송하는 완료 Ack받는 쓰레드
  void Setting_File_Names(vector<Operation> *op);
  void Write_Operation_to_clnts(Operation op_to_perform, int &cnt_sendTo_clnt, int &edit_op_sock);
  void Set_Operation_info_str(Operation &new_operation, vector<string> operation_info);
  string Combine_Operation_Info(vector<string> current_oper_info);
  
  // 텍스트 파일
  void Working_with_textfiles(Operation operation);
  void Delete_Operation_from_textfile(Operation operation);
  void Enter_Operation_From_TextFile(Operation operation);
  void Insert_Operation_From_TextFile(Operation operation);
  vector<string> Read_File(vector<string> lines, string filepath);
  void Write_File(vector<string> lines, string filepath);

  // 공용 함수
  vector<string> Split(string input, char delimiter);
  void Client_Disconnect(int clnt_sock, int type); // 클라이언트 접속 종료 처리
  void Error_Handling(string msg);                 // 에러시 출력 후 프로그램 종료
};