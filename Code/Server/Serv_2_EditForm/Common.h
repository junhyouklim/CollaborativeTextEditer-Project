#pragma once
#include <iostream>
#include <string>
#include <cstring>
#include <cstdlib>
#include <unistd.h>
#include <arpa/inet.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/signal.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <dirent.h>
#include <fstream>
#include <thread>
#include <mutex>
#include <vector>
#include <list>
#include <chrono>
#include <sstream>

#define BUF 256
#define CURSOR_BUF 30
#define SERV_MAINFORM_IP "10.10.20.58"
//#define SERV_MAINFORM_IP "10.10.20.240"
#define SERV_MAINFORM_PORT 9555 
#define DIR_PATH "../TextFiles/"
using namespace std;

typedef struct
{
    string file; // 클라가 작업중인 파일
    int id; // 클라id, 첫 연결시 서버에서 랜덤하게 생성
    int edit_sock; // 클라 Signal write → 서버 Signal read 소켓 
    int operation_sock; // 서버 Operation write → 클라 Operation read 소켓
    int acknowledge_sock; // 클라 Acknowledge 완료 write → 서버 Acknowledge read 소켓     
    int cursor_sock; // 서버 CursorInfo write → 클라 CursorInfo read 소켓
    int cursor_X; // 클라 커서 위치 
    int cursor_Y; // 클라 커서 위치 
    string cursor_color; // 클라 커서 색깔
    int line; // 점유 라인
} EditorInfo;

namespace PROC_SIGNAL 
{
    enum { CREATE, CREATE_SUCCESS, CREATE_FAIL, TERMINATE };
}

namespace TYPE // 소켓 타입
{
    enum { EDIT_SERV, PROC_INFO, MAIN, MAIN_READ }; // Serv_MainForm쪽에 연결하는 클라이언트 소켓 타입 
    enum { EDIT, OPERATION, ACKNOWLEDGE, CURSOR }; // 프로세스 서버에 연결하는 클라이언트 소켓 타입 
}   

namespace EDIT_SIGNAL
{   
    enum { OPEN, NEW_OPERATION, RECEIVE_ACK_FROM_CLNT, REQUEST_CURSOR, SEND_CURSOR, DISCONNECT, FIANL_ACK_CHECK };    
}

namespace OPERATOR_SIGNAL
{ 
   enum{ DEFAULT, INSERT, BACK, DELETE, ENTER };
}

namespace CURSOR_SIGNAL
{
    enum { INSERT, DELETE, UPDATE };
}