#include "Server.h"

//================================================
//               서버 관련 메서드
//================================================

Server::Server()
{	
	proc_port = START_PROC_PORT; // 프로세스에서 사용할 포트번호, 1씩 증가 

	int nSockOpt = 1;
	serv_sock = socket(PF_INET, SOCK_STREAM, 0);

	memset(&serv_adr, 0, sizeof(serv_adr));
	serv_adr.sin_family = AF_INET;
	serv_adr.sin_addr.s_addr = htonl(INADDR_ANY);
	serv_adr.sin_port = htons(PORT);

	// 포트 bind에러나도 바로 접속가능하게 설정
	setsockopt(serv_sock, SOL_SOCKET, SO_REUSEADDR, &nSockOpt, sizeof(nSockOpt));
	if (bind(serv_sock, (struct sockaddr *)&serv_adr, sizeof(serv_adr)) == -1)
		Error_Handling("bind error");
}

Server::~Server()
{
	close(serv_sock);
}

void Server::Server_On()
{
	int clnt_sock;
	struct sockaddr_in clnt_adr;
	int clnt_adr_sz;
	int type;
	
	if (listen(serv_sock, 5) == -1)
		Error_Handling("listen error");

	while (1)
	{
		clnt_adr_sz = sizeof(clnt_adr);
		clnt_sock = accept(serv_sock, (struct sockaddr *)&clnt_adr, (socklen_t *)&clnt_adr_sz);

		type = 0;
		read(clnt_sock, &type, sizeof(type)); // 클라이언트 타입 받음

		if (type == TYPE::EDIT_SERV) // Serv_EditForm 서버 소켓
		{
			// MainForm서버 프로세스 생성 Signal write → EditForm서버 Signal read 소켓
			EditServ_sock = clnt_sock;
		}
		else if(type == TYPE::PROC_INFO) // 프로세스 생성,종료 정보 받는 소켓
		{
			// EditForm클라 프로세스 정보 Signal write → MainForm서버 Signal read 소켓
			thread thd(&Server::ProcessInfo_Thread, this, clnt_sock);
			thd.detach();	
		}
	    else if (type == TYPE::MAIN) // 메인폼
		{
			// 클라 Signal write → 서버 Signal read 소켓
			main_mtx.lock(); // Main Lock
			main_socks.push_back(clnt_sock);
			thread thd(&Server::MainForm_Thread, this, clnt_sock);
			thd.detach();			
		}
		else if (type == TYPE::MAIN_READ) // 메인폼 read 소켓
		{
			//서버 .txt File 변경 정보 Signal write → 클라 Signal read 소켓
			main_read.push_back(clnt_sock);
			main_mtx.unlock(); // Main Unlock
		}			
	}
}

//================================================
//               MainForm 관련 메서드
//================================================

// 메인폼 쓰레드
void Server::MainForm_Thread(int clnt_sock)
{
	cout << "[ 메인 쓰레드 / 소켓 : " << clnt_sock << " ] " << endl;

	int data_size, signal = 0;
	bool check;
	MainForm main;

	main.Send_FileList(clnt_sock); // 클라이언트에게 현재 .txt파일 목록 보냄

	while (true)
	{
		check = false;
		data_size = read(clnt_sock, &signal, sizeof(int));

		if (data_size == 0) // 클라이언트 종료
		{
			break;
		}
		else if (data_size != -1) // 전송한 신호가 있는경우
		{
			switch (signal)
			{
			case MAIN_SIGNAL::CREATE_FILE:
				check = main.Create_File(clnt_sock);
				if (check)
				{
					for(int i = 0; i < main_read.size(); i++)
					{						
						main.Send_FileList(main_read[i]);
					}
				}
				break;
			case MAIN_SIGNAL::DELETE_FILE: // 파일 삭제
				check = main.Delete_File(clnt_sock);
				if (check)
				{
					for(int i=0; i<main_read.size(); i++)
					{
						main.Send_FileList(main_read[i]);
					}
				}
				break;
			case MAIN_SIGNAL::PROC_CHECK: // 선택파일 프로세스 체크, 포트 전송					
				string file = main.Recv_FileName(clnt_sock); // 프로세스 체크할 파일명 받음
				int idx = Get_VectorIndex(proc_files, file); // 인덱스 받음 
				cout << "클라가 선택한 파일명 : " << file << "  인덱스 : " << idx << endl;

				if(idx == -1) // 해당 파일 프로세스가 없는경우 
				{
					int signal = PROC_SIGNAL::CREATE;
					write(EditServ_sock, &signal, sizeof(signal)); // 프로세스 생성 신호 전송 
					write(EditServ_sock, &proc_port, sizeof(proc_port)); // 생성할 프로세스 포트 전송
					char temp[BUF];
					memset(temp, 0, sizeof(temp));
					strcpy(temp, file.c_str());
					write(EditServ_sock, &temp, sizeof(temp)); // 생성할 프로세스 파일명 전송

					usleep(500000); // 프로세스 생성 시간 >> 나중에 테스트해보고 조정 or 삭제
					write(clnt_sock, &proc_port, sizeof(proc_port)); // 클라에게 해당 프로세스 포트 전송		
					proc_port++;
				}
				else // 있는 경우
				{
					write(clnt_sock, &proc_ports[idx], sizeof(int)); // 클라에게 해당 프로세스 포트 전송			
				}
				break; 
			}
		}
	}
	Client_Disconnect(clnt_sock, TYPE::MAIN);
}

//================================================
//             Process Info 관련 메서드
//================================================

void Server::ProcessInfo_Thread(int clnt_sock)
{
	cout << "[ 프로세스 정보 쓰레드 / 소켓 : " << clnt_sock << " ] " << endl;
	int data_size, signal = 0;	
	int port;

	while (true)
	{
		data_size = read(clnt_sock, &signal, sizeof(int));

		if (data_size == 0) // 클라이언트 종료
		{
			break;
		}
		else if (data_size != -1) // 전송한 신호가 있는경우
		{
			switch (signal)
			{
			case PROC_SIGNAL::CREATE_SUCCESS:
				{
				read(clnt_sock, &port, sizeof(port));
				proc_ports.push_back(port);
				char temp[BUF];
				memset(temp, 0, sizeof(temp));
				read(clnt_sock, temp, sizeof(temp));
				string file_name(temp);
				proc_files.push_back(file_name);
				cout << "새로운 프로세스 생성" << endl;
				cout << "PORT : " << port << "  FILE : " << file_name << endl;
				break;
				}
			case PROC_SIGNAL::TERMINATE:
				cout << "프로세스 종료" << endl;
				read(clnt_sock, &port, sizeof(port));
				int idx = Get_VectorIndex(proc_ports, port);
				Erase_VectorValue(proc_ports, idx);
				Erase_VectorValue(proc_files, idx);
				break;
			}
		}
	}
	close(clnt_sock);
}

//================================================
//               공용 메서드
//================================================

// 클라이언트 연결 끊김 처리
void Server::Client_Disconnect(int sock, int type)
{
	if (type == TYPE::MAIN)
	{
		cout << "[ MainForm 클라 OUT!! / 소켓 : " << sock << "]" << endl;
		main_mtx.lock();
		int idx = Get_VectorIndex(main_socks, sock);
		Erase_VectorValue(main_read, idx);
		Erase_VectorValue(main_socks, idx);
		main_mtx.unlock();
	}	
	close(sock);
}

// Vector에서 자신의 인덱스 받아옴
int Server::Get_VectorIndex(vector<int> socks, int sock)
{
	vector<int>::iterator iter;
	int index = -1;

	for (iter = socks.begin(); iter != socks.end(); iter++)
	{
		if (*iter == sock)
			index = std::distance(socks.begin(), iter);
	}
	return index;
}
// Vector에서 자신의 인덱스 받아옴
int Server::Get_VectorIndex(vector<string> proc_files, string file)
{
	vector<string>::iterator iter;
	int index = -1;

	for (iter = proc_files.begin(); iter != proc_files.end(); iter++)
	{
		if (*iter == file)
			index = std::distance(proc_files.begin(), iter);
	}
	return index;
}

// Vector에서 해당 인덱스 erase
int Server::Erase_VectorValue(vector<int> socks, int index)
{
	int temp;
	vector<int>::iterator iter = socks.begin();
	advance(iter, index);
	temp = *iter;
	socks.erase(iter);

	return temp;
}
// Vector에서 해당 인덱스 erase
string Server::Erase_VectorValue(vector<string> str, int index)
{
	string temp;
	vector<string>::iterator iter = str.begin();
	advance(iter, index);
	temp = *iter;
	str.erase(iter);

	return temp;
}

// 오류 메시지
void Server::Error_Handling(string msg)
{
	cout << msg << endl;
	exit(1);
}
