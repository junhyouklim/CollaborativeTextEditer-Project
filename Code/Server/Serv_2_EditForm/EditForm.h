#pragma once
#include "Common.h"

class EditForm
{
private:
    string edit_file; // 작업중인 파일명
public:
    string Open_File(int sock);                                                 // 클라이언트가 처음에 선택하거나 오픈할 파일 설정
    void Send_All_CursorInfo(int signal, int index, vector<EditorInfo> editor); // 커서 정보 전송
    void Send_Delete_Cursor(int signal, int index, vector<EditorInfo> editor);  // 커서 삭제 시그널 전송
    string Recv_CursorPos(int clnt_sock);                                       // 변경 좌표 받음
    void Send_Update_Cursor(int signal, int index, vector<EditorInfo> editor);  // 커서 변경 시그널 전송
};