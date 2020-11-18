#include "EditForm.h"

// 파일 오픈
string EditForm::Open_File(int sock)
{
    ifstream ifs;
    string file_path = DIR_PATH;

    char temp[BUF];
    memset(temp, 0, sizeof(temp));
    read(sock, temp, sizeof(temp)); // 클라이언트에게 선택한 or 생성할 파일명 받음
    string str(temp);
    edit_file = str;
    cout << "소켓번호 :" << sock << endl;
    cout << "클라가 선택한 파일 : " << edit_file << endl;

    file_path += edit_file;
    cout << file_path << endl;
    ifs.open(file_path);

    string contents = "";
    if (ifs.is_open())
    {
        string str;
        while (!ifs.eof())
        {
            getline(ifs, str);
            contents += str;
            contents += "\r\n";
            // cout << "str : [" << str << "] /  size : " << str.size() << endl;
        }

        int len = contents.size();
        write(sock, &len, sizeof(len)); // 내용 바이트 크기 전달

        char temp[len];
        memset(temp, 0, sizeof(temp));
        strcpy(temp, contents.c_str());
        write(sock, temp, sizeof(temp)); // 내용 전달

        ifs.close();
    }
    else
    {
        cout << "파일이 없음 " << endl;
    }
    return edit_file;
}

// 같은 파일 작업중인 모든 커서 정보 전송
void EditForm::Send_All_CursorInfo(int signal, int index, vector<EditorInfo> editor)
{
    string str1, str2;
    
    str1 = "";
    str1 += to_string(signal) + "/";
    str1 += to_string(editor[index].id) + "/";
    str1 += to_string(editor[index].cursor_X) + "/";
    str1 += to_string(editor[index].cursor_Y) + "/";
    str1 += editor[index].cursor_color + "/";
    str1 += to_string(editor[index].line) + "/";

    char for_requested_client[CURSOR_BUF];
    char for_other_clients[CURSOR_BUF];
    memset(for_other_clients, 0, sizeof(for_other_clients));
    strcpy(for_other_clients, str1.c_str());

    for (int i = 0; i < editor.size(); i++)
    {
        str2.clear();
        memset(for_requested_client, 0, sizeof(for_requested_client));
        if (i == index)
            continue;
        if (editor[i].file == edit_file)
        {
            str2 += to_string(signal) + "/";
            str2 += to_string(editor[i].id) + "/";
            str2 += to_string(editor[i].cursor_X) + "/";
            str2 += to_string(editor[i].cursor_Y) + "/";
            str2 += editor[i].cursor_color + "/";           
            str2 += to_string(editor[index].line) + "/"; 
            // cout << "받을 커서 정보: "<< str2 << endl;

            strcpy(for_requested_client, str2.c_str());
            write(editor[index].cursor_sock, for_requested_client, sizeof(for_requested_client));               
            usleep(50000);            
            write(editor[i].cursor_sock, for_other_clients, sizeof(for_other_clients)); 
            usleep(50000);
        }
    }
}

// 같은 파일 작업중인 클라들에게 연결 끊은 커서 삭제하라고 전송
void EditForm::Send_Delete_Cursor(int signal, int index, vector<EditorInfo> editor)
{
    string str = "";
    str += to_string(signal) + "/";
    str += to_string(editor[index].id) + "/";
    str += to_string(editor[index].cursor_X) + "/";
    str += to_string(editor[index].cursor_Y) + "/";
    str += editor[index].cursor_color + "/";
    str += to_string(editor[index].line) + "/"; 

    char temp[CURSOR_BUF];
    memset(temp, 0, sizeof(temp));
    strcpy(temp, str.c_str());
    // cout << "DELETE 커서 정보 : "<< temp << endl;

    for (int i = 0; i < editor.size(); i++)
    {
        if (i == index)
            continue;
        if (editor[i].file == edit_file)
        {
            write(editor[i].cursor_sock, temp, sizeof(temp));
            usleep(50000);
        }
    }
}

// 변경 좌표 받음
string EditForm::Recv_CursorPos(int clnt_sock)
{
    // X,Y 좌표 받음
    char temp[CURSOR_BUF];
    memset(temp, 0, sizeof(temp));
    read(clnt_sock, temp, sizeof(temp));
    string str(temp);

    return str;
}

// 같은 파일 작업중인 클라들에게 커서 변경 전송
void EditForm::Send_Update_Cursor(int signal, int index, vector<EditorInfo> editor)
{
    string str = "";
    str += to_string(signal) + "/";
    str += to_string(editor[index].id) + "/";
    str += to_string(editor[index].cursor_X) + "/";
    str += to_string(editor[index].cursor_Y) + "/";
    str += editor[index].cursor_color + "/";
    str += to_string(editor[index].line) + "/";

    char temp[CURSOR_BUF];
    memset(temp, 0, sizeof(temp));
    strcpy(temp, str.c_str());
    // cout << "UPDATE 커서 정보 : "<< temp << endl;

    for (int i = 0; i < editor.size(); i++)
    {
        if (i == index)
            continue;
        if (editor[i].file == edit_file)
        {
            write(editor[i].cursor_sock, temp, sizeof(temp));
            usleep(50000);
        }
    }
}
