#include "MainForm.h"

// 클라이언트에게 파일 리스트 전송
void MainForm::Send_FileList(int sock)
{  
    Get_FileList(DIR_PATH); // TextFiles 디렉토리에 있는 파일명 추출 			
	// cout << "파일명 목록 사이즈 : " << files.size() << endl;
	// cout << "파일명 [" << files << "]" << endl;
    if(files == "") files += "/";
	char temp[files.size()];
	strcpy(temp, files.c_str());
	write(sock, &temp, sizeof(temp)); // 마지막 '/'문자 하나 제거 
}

// TextFiles 디렉토리에 있는 .txt파일 전부 찾기 
void MainForm::Get_FileList(const char *path)
{
	struct dirent *entry;
	ssize_t pos; 

	DIR *dir = opendir(path);
	if (dir == NULL) return;	

	files = "";    
    if((dir = opendir(path)) == NULL)
    {
        perror("opendir() failed");
        return;
    }

	while ((entry = readdir(dir)) != NULL)
	{
		string file_name = entry->d_name;		
		pos = file_name.find(".txt"); // 첫 번째로 일치하는 문자의 위치를 return 해 줍니다.
        //일치하는 위치를 찾지 못한 경우 string::npos를 return합니다.
		if(pos != string::npos)
		{
			files += file_name;
			files += '/';
			//cout << file_name << endl;
		}
	}
	closedir(dir);
}

// 새 파일 생성
bool MainForm::Create_File(int sock)
{           
    ofstream ofs; 
    ifstream ifs;
    string file_path = DIR_PATH;
    char file_name[BUF];
    int result;

    // 생성하려는 파일명 받음 
    memset(file_name, 0, sizeof(file_name));
    read(sock, file_name, sizeof(file_name));
    file_path += file_name; 
    file_path += ".txt";

    // 같은 파일명 있나 확인
    ifs.open(file_path);
    if(ifs.is_open())
    {
        cout << "이미 존재하는 파일 " << endl;
        ifs.close();
        result = FILE_CHANGED::FAIL;
    }
    else
    {
        cout << "이전에 없던 파일" << endl;
        result = FILE_CHANGED::SUCCESS;
    }

    // 생성 가능시 파일 생성 
    if(result == FILE_CHANGED::SUCCESS)
    {
        ofs.open(file_path);
        if(ofs.is_open()) // 파일 생성 성공
        {            
            cout << "파일 생성 : " << file_name << endl;
            ofs.close();
        }
        else // 파일 생성 실패  ex) 파일명에 들어갈 수 없는 문자 포함 
        {
            cout << "파일 생성 실패 " << endl;
            result = FILE_CHANGED::FAIL;
        }
    }

    // 생성 결과 전송
    write(sock, &result, sizeof(result));  

    if(result == FILE_CHANGED::SUCCESS) 
        return true;
    else 
        return false;    
}

// 파일 삭제
bool MainForm::Delete_File(int sock)
{
    ofstream ofs; 
    ifstream ifs;
    string file_path = DIR_PATH;
    char file_name[BUF];
    int check;
    int result = 0;

    // 삭제하려는 파일명 받음 
    memset(file_name, 0, sizeof(file_name));
    read(sock, file_name, sizeof(file_name));
    file_path += file_name;

    char ch_file_path[file_path.size()];
    strcpy(ch_file_path, file_path.c_str());
    check = remove(ch_file_path);
    
    if(check == 0)
    {
        cout<<"파일 삭제 성공"<<endl;
        result = FILE_CHANGED::SUCCESS;
    }
    else if(check == -1)
    {
        cout<<"파일 삭제 실패"<<endl;
        result = FILE_CHANGED::FAIL;
    }        
    write(sock, &result, sizeof(result));  
    
    return result;
}

string MainForm::Recv_FileName(int clnt_sock)
{
    char temp[BUF];
    memset(temp, 0, sizeof(temp));
    read(clnt_sock, temp, sizeof(temp)); // 클라로부터 선택한 파일명 받음    
    string file(temp);
    
    return file;
}

