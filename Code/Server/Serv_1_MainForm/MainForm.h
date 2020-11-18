#pragma once
#include "Common.h"

class MainForm
{
private:
    string files; // 파일명 목록 
public:
    void Send_FileList(int sock); // 파일 리스트 클라에게 전송
    void Get_FileList(const char *path); // 디렉토리에 있는 txt파일 찾기
    bool Create_File(int sock); // 새파일 생성
    bool Delete_File(int sock); // 파일 삭제
    string Recv_FileName(int clnt_sock);
};