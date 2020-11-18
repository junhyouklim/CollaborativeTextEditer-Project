#include "Server.h"

//================================================
//               서버 관련 메서드
//================================================

Server::Server()
{
	// Serv_MainForm에 클라이언트로 연결해서 프로세스 생성 Signal 받기위한 소켓 설정
	ProcSignal_sock = socket(PF_INET, SOCK_STREAM, 0);
	ProcInfo_sock = socket(PF_INET, SOCK_STREAM, 0);

	memset(&MainServ_adr, 0, sizeof(MainServ_adr));
	MainServ_adr.sin_family = AF_INET;
	MainServ_adr.sin_addr.s_addr = inet_addr(SERV_MAINFORM_IP);
	MainServ_adr.sin_port = htons(SERV_MAINFORM_PORT);

	if (connect(ProcInfo_sock, (struct sockaddr *)&MainServ_adr, sizeof(MainServ_adr)) == -1)
		Error_Handling("connect() error");
	int type = TYPE::PROC_INFO;
	write(ProcInfo_sock, &type, sizeof(type)); // 소켓 타입 전송
}

Server::~Server()
{
	close(ProcSignal_sock);
}

// 프로세스 종료시 Serv_MainForm에 전송
void Read_EndProc(int sig)
{
	int status;
	pid_t id = waitpid(-1, &status, WNOHANG);
	if (WIFEXITED(status))
	{
		cout << "프로세스 종료 PID : " << id << endl;
		int val = WEXITSTATUS(status);
		cout << "Return Value : " << val << endl;
	}
}

// Serv_MainForm에 클라이언트로 연결해서 프로세스 생성 Signal 받기위한 연결
void Server::Connect_MainServ()
{
	// Signal Read 소켓 연결
	if (connect(ProcSignal_sock, (struct sockaddr *)&MainServ_adr, sizeof(MainServ_adr)) == -1)
		Error_Handling("connect() error");
	int type = TYPE::EDIT_SERV;
	write(ProcSignal_sock, &type, sizeof(type)); // 소켓 타입 전송

	// 프로세스 종료 sigaction 등록
	struct sigaction act;
	act.sa_handler = Read_EndProc;
	sigemptyset(&act.sa_mask);
	act.sa_flags = 0;
	sigaction(SIGCHLD, &act, 0);

	clnt_id = 1;
	bool isNeed_Setting = true;

	pid_t pid = -1;
	bool Working = true;
	while (Working)
	{
		if (pid == 0) // 자식 프로세스
		{
			cout << "[자식 프로세스]" << endl;
			if (isNeed_Setting)
			{
				Send_Process_Create_Signal();				 // 메인서버에 생성 정보 전달
				thread thd(&Server::Operation_Thread, this); // 오퍼레이션 쓰레드 실행
				thd.detach();
				Process_Server_On(); // 서버 소켓 생성 및 Listen
				isNeed_Setting = false;
			}
			Process_Server_Working();
		}
		else // 부모 프로세스
		{
			cout << "[부모 프로세스]" << endl;
			Working = Read_Process_Create_Signal();
			if (Working)
				pid = fork();
		}
	}
}

//================================================
//             부모 프로세스 관련
//================================================

// Serv_MainForm으로부터 생성 Signal 받음
bool Server::Read_Process_Create_Signal()
{
	int data_size, signal = 0;
	data_size = read(ProcSignal_sock, &signal, sizeof(int));
	cout << "read!!  data_size : " << data_size << endl;

	if (data_size == 0) // 클라이언트 종료
	{
		return false;
	}
	else if (signal == PROC_SIGNAL::CREATE) // 전송한 신호가 있는경우
	{
		read(ProcSignal_sock, &proc_port, sizeof(proc_port));
		char temp[BUF];
		memset(temp, 0, sizeof(temp));
		read(ProcSignal_sock, &temp, sizeof(temp));
		string file(temp);
		proc_file = file;
		cout << "프로세스 생성 PORT:" << proc_port << "  FILE:" << proc_file << endl;
	}
	return true;
}

//================================================
//             자식 프로세스 관련
//================================================

// 메인서버에 성공적으로 프로세스 생성했다고 정보 전송
void Server::Send_Process_Create_Signal()
{
	int signal = PROC_SIGNAL::CREATE_SUCCESS;
	write(ProcInfo_sock, &signal, sizeof(signal));
	write(ProcInfo_sock, &proc_port, sizeof(proc_port));
	char temp[BUF];
	memset(temp, 0, sizeof(temp));
	strcpy(temp, proc_file.c_str());
	write(ProcInfo_sock, temp, sizeof(temp));
	cout << "프로세스 생성 정보 전달 PORT:" << proc_port << " FILE:" << proc_file << endl;
}

// 프로세스 서버 On
void Server::Process_Server_On()
{
	int nSockOpt = 1;
	ProcServ_sock = socket(PF_INET, SOCK_STREAM, 0);

	memset(&ProcServ_adr, 0, sizeof(ProcServ_adr));
	ProcServ_adr.sin_family = AF_INET;
	ProcServ_adr.sin_addr.s_addr = htonl(INADDR_ANY);
	ProcServ_adr.sin_port = htons(proc_port);

	// 포트 bind에러나도 바로 접속가능하게 설정
	setsockopt(ProcServ_sock, SOL_SOCKET, SO_REUSEADDR, &nSockOpt, sizeof(nSockOpt));
	if (bind(ProcServ_sock, (struct sockaddr *)&ProcServ_adr, sizeof(ProcServ_adr)) == -1)
		Error_Handling("bind error");

	if (listen(ProcServ_sock, 5) == -1)
		Error_Handling("listen error");
}

// 일한다 프로세스 서버 열심히
void Server::Process_Server_Working()
{
	int clnt_sock;
	struct sockaddr_in clnt_adr;
	int clnt_adr_sz;
	int type;

	clnt_adr_sz = sizeof(clnt_adr);
	clnt_sock = accept(ProcServ_sock, (struct sockaddr *)&clnt_adr, (socklen_t *)&clnt_adr_sz);

	type = 0;
	read(clnt_sock, &type, sizeof(type)); // 클라이언트 타입 받음

	if (type == TYPE::EDIT)
	{
		// 클라 Signal write → 서버 Signal read 소켓
		edit_mtx.lock(); // Edit Lock
		New_EditorInfo(clnt_sock);
		thread thd(&Server::EditForm_Thread, this, clnt_sock);
		thd.detach();
	}
	else if (type == TYPE::OPERATION)
	{
		// 서버 Operation write → 클라 Operation read 소켓
		editor.back().operation_sock = clnt_sock;
	}
	else if (type == TYPE::ACKNOWLEDGE)
	{
		// 클라 Acknowledge 완료 write → 서버 Acknowledge read 소켓
		editor.back().acknowledge_sock = clnt_sock;
		thread thd(&Server::Acknowledge_Thread, this, clnt_sock);
		thd.detach();
	}
	else if (type == TYPE::CURSOR) // 커서 좌표정보 read 소켓
	{
		// 서버 CursorInfo write → 클라 CursorInfo read 소켓
		editor.back().cursor_sock = clnt_sock;
		editor.back().cursor_color = Set_CursorColor(editor.back().file);
		edit_mtx.unlock(); // Edit Unlock
	}
}

// 새로운 메모장 폼 클라이언트 접속시 생성, Vector에 추가
void Server::New_EditorInfo(int clnt_sock)
{
	EditorInfo temp;
	memset(&temp, 0, sizeof(temp));
	temp.id = clnt_id;
	clnt_id++;
	temp.cursor_X = 0;
	temp.cursor_Y = 0;
	temp.edit_sock = clnt_sock;	
	temp.line = -1;
	editor.push_back(temp);
}

// 새로운 클라이언트 접속시 파일별로 커서 색깔 지정
string Server::Set_CursorColor(string file)
{
	// 일단 테스트용 8색상
	// 하나의 txt파일에 8명 이상 접근시 색상 추가
	list<string> colors;
	colors.push_back("Red");
	colors.push_back("Purple");
	colors.push_back("Blue");
	colors.push_back("Green");
	colors.push_back("Pink");
	colors.push_back("Orange");
	colors.push_back("SkyBlue");
	colors.push_back("BurlyWood");

	// 사용하고 있는 색은 지움
	for (int i = 0; i < editor.size() - 1; i++)
	{	
		colors.remove(editor[i].cursor_color);		
	}
	
	return colors.front();
}

//================================================
//               EditForm 관련 메서드
//================================================

// 에디트 폼 쓰레드
void Server::EditForm_Thread(int clnt_sock)
{
	cout << "[ 에디트폼 연결 / 소켓 : " << clnt_sock << " ] " << endl;

	int data_size, signal = 0;
	EditForm edit;

	// Operation 관련 데이터
	Operation operation;
	char operation_info[512]; // 클라로부터 전달될 string형태의 operation을 저장할 문자배열
	string operation_info_str;
	vector<string> oper_info_collection;
	vector<string> line_info; // 형섭 추가

	bool isConnect;
	string str_pos = "";
	
	int idx = Get_VectorIndex(clnt_sock);
	write(clnt_sock, &editor[idx].id, sizeof(int)); // 클라에게 id 전송
	cout << "id : [" << editor[idx].id << "]" << endl;

	// **********새로운 클라이언트 접속에 대한 처리************

	int sync_signal = 3; // 입력대기 시그널

	// 접속한 클라이언트 외의 클라이언트들 에게 입력대기명령 시그널을 전달한다.
	for(int i=0; i<editor.size(); i++)
	{
		if(editor[i].id!=editor[idx].id)
		{
			write(editor[i].operation_sock,&sync_signal,sizeof(int));
		}
	}
	
	// operations에 남아있는 operation이 적용될 때까지 접속하려는 클라이언트는 접속 및 입력대기
	while(true)
	{
		if(operations.size() == 0)
			break;
		std::this_thread::sleep_for(std::chrono::duration<int>(1));
	}

	// 처리대기중인 operation이 없다면
	sync_signal = 4;
	
	// 입장한 클라이언트는 Wait_For_Sync() (EditForm.cs - 83) 에서 대기중인 상태 해제후 파일내용을 불러오게 한다.	
	// 입력대기 중이였던 나머지 클라이언트에게 입력대기해제 시그널을 전달한다. 
	for(int i=0; i<editor.size(); i++)
	{
		if(editor[i].id!=editor[idx].id)
		{
			write(editor[i].operation_sock,&sync_signal,sizeof(int));
		}
		else
		{
			write(clnt_sock, &sync_signal, sizeof(int)); 
		}	
	}

	isConnect = true;
	while (isConnect)
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
			case EDIT_SIGNAL::OPEN:
				idx = Get_VectorIndex(clnt_sock);
				editor[idx].file = edit.Open_File(clnt_sock);
				break;
			// 클라이언트로가 생성한 Operation전달받기
			case EDIT_SIGNAL::NEW_OPERATION:
				// '/' 로 구분지어진 Operation 데이터를 읽는다.
				memset(operation_info, 0, sizeof(operation_info));
				read(clnt_sock, &operation_info, sizeof(operation_info));

				// 문자배열 --> 문자열형식으로 변환한다.
				operation_info_str = operation_info;

				// 문자열을 '/' 기준으로 구별한다.
				oper_info_collection = Split(operation_info_str, '/');

				operation_mtx.lock();
				// 구별된 Operation 데이터를 Operation 객체에 넣는다.
				Set_Operation_info_str(operation, oper_info_collection);

				// operation을 추가한다.
				operations.push_back(operation);
				operation_mtx.unlock();
				break;
			case EDIT_SIGNAL::REQUEST_CURSOR: // 같은 파일 작업중인 모든 클라커서 정보 전송
				idx = Get_VectorIndex(clnt_sock);
				edit.Send_All_CursorInfo(CURSOR_SIGNAL::INSERT, idx, editor);
				break;
			case EDIT_SIGNAL::SEND_CURSOR:
				str_pos = edit.Recv_CursorPos(clnt_sock);
				idx = Get_VectorIndex(clnt_sock);				
				line_info.clear();
				line_info = Split(str_pos, '/');
				editor[idx].cursor_X = stoi(line_info[0]);
				editor[idx].cursor_Y = stoi(line_info[1]);
				editor[idx].line = stoi(line_info[2]); 
				// cout << "cursor_X / cursor_Y / line: " << editor[idx].cursor_X << " / " << editor[idx].cursor_Y <<" / " << editor[idx].line <<endl;
				// 변경 정보 나머지 클라들에게 전송
				edit.Send_Update_Cursor(CURSOR_SIGNAL::UPDATE, idx, editor);
				break;
			case EDIT_SIGNAL::DISCONNECT: // EditForm 클라이언트 접속 종료 시그널
				idx = Get_VectorIndex(clnt_sock);
				edit.Send_Delete_Cursor(CURSOR_SIGNAL::DELETE, idx, editor);
				isConnect = false;
				break;
			}
		}
	}
	Client_Disconnect(clnt_sock, TYPE::EDIT);
}

// editor Vector에서 자신의 인덱스 받아옴(edit_sock용)
int Server::Get_VectorIndex(int edit_sock)
{
	int index = -1;

	for (int i = 0; i < editor.size(); i++)
	{
		if (editor[i].edit_sock == edit_sock)
		{
			index = i;
			break;
		}
	}
	return index;
}

// editor Vector에서 해당 인덱스 erase
void Server::Erase_VectorValue(int index)
{
	vector<EditorInfo>::iterator iter = editor.begin();
	advance(iter, index);
	editor.erase(iter);
}

//================================================
//               Operation 관련 메서드
//================================================

void Server::Operation_Thread()
{
	vector<Operation> operations_per_workingzone; //  작업중인 메모장의 파일명 (+부가정보) 들을 모아놓는 벡터자료형
	int cnt_sendTo_clnt = 0;
	int edit_op_sock;

	while (true)
	{
		if (operations.size() > 0) // operations : 모든 operation을 관리하는 백터자료형
		{
			cout << "operation처리작업시작" << endl;
			// 수행될 operation의 mode에 따라 '/' 기준으로 문자열 구분짓는다.
			Write_Operation_to_clnts(operations[0], cnt_sendTo_clnt, edit_op_sock);
			Working_with_textfiles(operations[0]);
			while (true)
			{
				if (ackCnt_to_receive == 0)
					break;
			}

			// 수집 완료후 현 operation을 수행한 클라에게 최종 ack을 전달한다.
			int signal = 2;

			write(edit_op_sock, &signal, sizeof(signal));

			cout << "최종 ack 전달함" << endl;
			while (final_ack_check == false)
			{
				std::chrono::seconds dura(1);
				this_thread::sleep_for(dura);
				cout << "최종 ack 받았음을 확인대기" << endl;
			}
			cout << "최종 ack 받았음을 확인" << endl;

			final_ack_check = false;
			// 처리된 Operation을 지운다.
			operation_mtx.lock();
			operations.erase(operations.begin());
			operation_mtx.unlock();
			cout << "operation삭제됨" << endl;
		}
	}
}
void Server::Working_with_textfiles(Operation operation)
{
	switch(operation.get_mode())
	{
		case OPERATOR_SIGNAL::DEFAULT:
			break;
		case OPERATOR_SIGNAL::INSERT:
			Insert_Operation_From_TextFile(operation);
			break;
		case OPERATOR_SIGNAL::BACK:
			Delete_Operation_from_textfile(operation);
			break;
		case OPERATOR_SIGNAL::ENTER:
			Enter_Operation_From_TextFile(operation);
			break;
	}
}

void Server::Enter_Operation_From_TextFile(Operation operation)
{   
    ifstream readFile;
    ofstream writeFile;  
	vector<string> all_lines; 

	string file_name(operation.get_work_zone_name());
	string file_path = DIR_PATH + file_name;

	int starting_line_number = operation.get_starting_line_number();
	int starting_index_number = operation.get_starting_index_number();
	int i = 0;

	readFile.open(file_path);

	if(readFile.is_open())
	{
		string line_str;
		// 파일의 모든 줄을 읽는다
		while(!readFile.eof())
		{			
			getline(readFile, line_str);
			line_str += "\n";
			all_lines.push_back(line_str);
		}
		// 파일을 닫는다.
		readFile.close();
	}

	// 엔터친 라인의 인덱스에 개행을 추가시킨다.
	all_lines[starting_line_number].insert(starting_index_number,"\n");
			
    // 파일을 쓴다.
    writeFile.open(file_path);
	if(writeFile.is_open())
	{
		for(int i = 0; i < all_lines.size(); i++)
		{
			writeFile.write(all_lines[i].c_str(), all_lines[i].size());
		}
	}
    // 파일을 닫는다.
    writeFile.close();
}

void Server::Insert_Operation_From_TextFile(Operation operation)
{

    ifstream readFile;
    ofstream writeFile;  
	vector<string> all_lines; 

	string file_name(operation.get_work_zone_name());
	string file_path = DIR_PATH + file_name;

	// 파일을 연다
    readFile.open(file_path);

	if(readFile.is_open())
	{
		string line_str;
		// 파일의 모든 줄을 읽는다
		while(!readFile.eof())
		{			
			getline(readFile, line_str);
			line_str += "\n";
			all_lines.push_back(line_str);
		}
		// 파일을 닫는다.
		readFile.close();
	}

	// Starting_Line_Number번째 칸에 Message를 쓴다.
	all_lines[operation.get_starting_line_number()] = operation.get_message();
	all_lines[operation.get_starting_line_number()] += "\n";

    // 파일을 쓴다.
    writeFile.open(file_path);

	if(writeFile.is_open())
	{
		for(int i = 0; i < all_lines.size(); i++)
		{
			writeFile.write(all_lines[i].c_str(), all_lines[i].size());
		}
	}
    // 파일을 닫는다.
    writeFile.close();
}


void Server::Delete_Operation_from_textfile(Operation operation)
{
	string filePath = DIR_PATH;
	filePath += operation.get_work_zone_name();
	vector<string> lines;
	int startline = operation.get_starting_line_number();
	int endline = operation.get_ending_line_number();
	int totalLength = 0;

	lines = Read_File(lines, filePath);
	totalLength = lines.size();
	cout << "totalLength:" << totalLength << endl;
	if (!lines.empty())
	{
		//내용 수정
		if (operation.get_deletemessage() != "")
		{
			if (startline != endline)
			{
				for (int i = startline; i < totalLength - 1; i++)
				{
					lines[i] = lines[i + 1];
				}
			}
			lines[endline] = operation.get_deletemessage();
		}
	}
	Write_File(lines, filePath);
}
vector<string> Server::Read_File(vector<string> lines, string filePath)
{
	// read File
	ifstream openFile(filePath.data());
	if (openFile.is_open()) {
		string line;
		while (getline(openFile, line)) {
			lines.push_back(line);
			cout << line << endl;
		}
		openFile.close();
	}
	return lines;
}

void Server::Write_File(vector<string> lines, string filePath)
{
	// write File
	ofstream writeFile(filePath.data());
	if (writeFile.is_open()) {
		vector<string>::iterator itor = lines.begin();
		for (; itor != lines.end(); itor++)
		{
			writeFile << *itor + "\n";
			cout << *itor << endl;
		}
		writeFile.close();
	}
}
void Server::Acknowledge_Thread(int clnt_sock)
{
	cout << "[ Acknowledge 연결 / 소켓 : " << clnt_sock << " ] " << endl;
	int data_size, signal = 0;
	bool check;

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
			case EDIT_SIGNAL::RECEIVE_ACK_FROM_CLNT:
				ackCnt_mtx.lock();
				ackCnt_to_receive--;
				ackCnt_mtx.unlock();
				break;
			case EDIT_SIGNAL::FIANL_ACK_CHECK:
				cout << "final 체크 도착" << endl;
				if (final_ack_check == false)
				{
					final_ack_check = true;
				}
				else
				{
					cout << "final_ack_check error" << endl;
				}
				break;
			}
		}
	}
	Client_Disconnect(clnt_sock, TYPE::ACKNOWLEDGE);
}

void Server::Setting_File_Names(vector<Operation> *operations_filename)
{
	Operation operation_to_push; // 파일명만 갱신할 임시 operation
	char file_name[50];			 // operations자료형 요소의 파일이름
	bool isFileExist;			 //

	// 현재 작업중인 클라이언트 수만큼 반복한다.
	for (int i = 0; i < editor.size(); i++)
	{
		// 초기화
		isFileExist = false;
		operation_to_push.init_workzone_name();

		memset(file_name, 0, sizeof(char) * 50);
		strcpy(file_name, editor[i].file.c_str());
		cout << "file_name :" << file_name << endl;
		for (int j = 0; j < operations_filename->size(); j++)
		{
			if (strcmp(file_name, (operations_filename->begin() + j)->get_work_zone_name()) == 0)
			{
				cout << "get_work_zone_name : " << (operations_filename->begin() + j)->get_work_zone_name() << endl;
				isFileExist = true;
				break;
			}
		}
		// 존재하지 않는 파일명이라면
		if (isFileExist == false)
		{
			cout << "파일명 :" << operation_to_push.get_work_zone_name() << endl;
			operation_to_push.set_work_zone_name(file_name);
			operations_filename->push_back(operation_to_push);
		}
	}
}

// Operation 속성을 '/' 로 구분짓다
string Server::Combine_Operation_Info(vector<string> current_oper_info)
{
	int i;
	string combined_oper_info;

	for (i = 0; i < current_oper_info.size(); i++)
	{
		combined_oper_info += current_oper_info[i];
		combined_oper_info += "/";
	}

	return combined_oper_info;
}

void Server::Write_Operation_to_clnts(Operation op_to_perform, int &cnt_sendTo_clnt, int &edit_op_sock)
{
	// 처리해야 할 Operation 의 mode에 맞게 필요한 데이터를 '/'로 구분지어 문자열화 시킨다.
	vector<string> operation_property;
	string oper_property_info;
	char oper_property_info_ch[512];

	string work_zone_temp;
	string message_temp;

	switch (op_to_perform.get_mode())
	{
	case OPERATOR_SIGNAL::INSERT:
		// id_performed_clnt,mode,starting_line_number,work_zone_name,message
		operation_property.push_back(to_string(op_to_perform.get_id_performed_clnt()));
		operation_property.push_back(to_string(op_to_perform.get_mode()));
		operation_property.push_back(to_string(op_to_perform.get_starting_line_number()));
		work_zone_temp = op_to_perform.get_work_zone_name();
		message_temp = op_to_perform.get_message();
		operation_property.push_back(work_zone_temp);
		operation_property.push_back(message_temp);
		operation_property.push_back(to_string(op_to_perform.get_ending_line_number()));
		operation_property.push_back(to_string(op_to_perform.get_ending_index_number()));
		operation_property.push_back(to_string(op_to_perform.get_starting_index_number()));
		oper_property_info = Combine_Operation_Info(operation_property);
		cout << "Combine_operation_info : " << oper_property_info << endl;
		strcpy(oper_property_info_ch, oper_property_info.c_str());
		break;
	case OPERATOR_SIGNAL::BACK:
	case OPERATOR_SIGNAL::DELETE:
		operation_property.push_back(to_string(op_to_perform.get_id_performed_clnt()));
		operation_property.push_back(to_string(op_to_perform.get_mode()));
		operation_property.push_back(to_string(op_to_perform.get_starting_line_number()));
		operation_property.push_back(to_string(op_to_perform.get_starting_index_number()));
		operation_property.push_back(to_string(op_to_perform.get_ending_line_number()));
		operation_property.push_back(to_string(op_to_perform.get_ending_index_number()));
		operation_property.push_back(op_to_perform.get_message());
		operation_property.push_back(op_to_perform.get_deletemessage());
		operation_property.push_back(op_to_perform.get_work_zone_name());
		oper_property_info = Combine_Operation_Info(operation_property);
		cout << "Combine_operation_info : " << oper_property_info << endl;
		strcpy(oper_property_info_ch, oper_property_info.c_str());
		break;
	case OPERATOR_SIGNAL::ENTER:
		operation_property.push_back(to_string(op_to_perform.get_id_performed_clnt()));
		operation_property.push_back(to_string(op_to_perform.get_mode()));
		operation_property.push_back(to_string(op_to_perform.get_starting_line_number()));
		operation_property.push_back(to_string(op_to_perform.get_starting_index_number()));
		operation_property.push_back(to_string(op_to_perform.get_ending_line_number()));
		oper_property_info = Combine_Operation_Info(operation_property);
		cout << "ENTER => Combine_operation_info : " << oper_property_info << endl;
		strcpy(oper_property_info_ch, oper_property_info.c_str());
		break;
	default:
		break;
	}
	// 수행되어야 할 Operation을 전달해야 할 클라이언트 수
	cnt_sendTo_clnt = 0;

	for (int i = 0; i < editor.size(); i++)
	{
		// 동일한 파일에서 작업중인 모든 클라이언트 골라내기 (해당operation을 수행한 클라는 제외)
		if (strcmp(op_to_perform.get_work_zone_name(), editor[i].file.c_str()) == 0 && op_to_perform.get_id_performed_clnt() != editor[i].id)
		{
			cout << "Operation을 전달받을 클라 찾음" << endl;
			// 시그널 전달
			int signal = 1;
			write(editor[i].operation_sock, &signal, sizeof(signal));
			// operation 정보전달
			write(editor[i].operation_sock, &oper_property_info_ch, sizeof(oper_property_info_ch));
			cnt_sendTo_clnt++; // ack을 되돌려 받아야하는 개수
		}
		// 현재 operation을 수행한 클라이언트 소켓을 설정
		else if (strcmp(op_to_perform.get_work_zone_name(), editor[i].file.c_str()) == 0 && op_to_perform.get_id_performed_clnt() == editor[i].id)
		{
			cout << "Operation 클라 찾음" << endl;
			edit_op_sock = editor[i].operation_sock;
		}
	}
	// 클라로부터 돌려받아야 할 ack개수 할당
	ackCnt_to_receive = cnt_sendTo_clnt;
	cout << "클라로부터 돌려받아야 할 총 ack 개수: " << ackCnt_to_receive << endl;
}

void Server::Set_Operation_info_str(Operation &new_operation, vector<string> operation_info)
{
	int new_oper_mode = std::stoi(operation_info.at(1));

	switch (new_oper_mode)
	{
	// id_performed_clnt,mode,starting_line_number,work_zone_name,message
	case OPERATOR_SIGNAL::INSERT:
		new_operation.set_id_performed_clnt(std::stoi(operation_info.at(0)));
		new_operation.set_mode(new_oper_mode);
		new_operation.set_starting_line_number(std::stoi(operation_info.at(2)));
		new_operation.set_work_zone_name(operation_info.at(3).c_str());
		cout << "message length :" << operation_info.at(4).length() << endl;
		new_operation.set_message(operation_info.at(4).c_str());
		new_operation.set_ending_line_number(std::stoi(operation_info.at(5)));
		new_operation.set_ending_index_number(std::stoi(operation_info.at(6)));
		new_operation.set_starting_index_number(std::stoi(operation_info.at(7)));
		cout << "INSERT→ new_operation.get_ending_line_number() => " << new_operation.get_ending_line_number() << endl;
		cout << "INSERT→ new_operation.get_ending_index_number() => " << new_operation.get_ending_index_number() << endl;
		cout << "INSERT→ new_operation.get_starting_index_number() => " << new_operation.get_starting_index_number() << endl;
		break;
	case OPERATOR_SIGNAL::DELETE:
	case OPERATOR_SIGNAL::BACK:
		new_operation.set_id_performed_clnt(std::stoi(operation_info[0]));
		new_operation.set_mode(new_oper_mode);
		new_operation.set_starting_line_number(stoi(operation_info[2]));
		new_operation.set_starting_index_number(stoi(operation_info[3]));
		new_operation.set_ending_line_number(stoi(operation_info[4]));
		new_operation.set_ending_index_number(stoi(operation_info[5]));
		new_operation.set_message(operation_info[6].c_str());
		new_operation.set_deletemessage(operation_info[7].c_str());
		new_operation.set_work_zone_name(operation_info[8].c_str());
		break;
	case OPERATOR_SIGNAL::ENTER:
		new_operation.set_id_performed_clnt(std::stoi(operation_info.at(0)));
		new_operation.set_mode(new_oper_mode);
		new_operation.set_starting_line_number(std::stoi(operation_info.at(2)));
		new_operation.set_starting_index_number(std::stoi(operation_info.at(3)));
		new_operation.set_ending_line_number(std::stoi(operation_info.at(4)));
		new_operation.set_work_zone_name(operation_info.at(5).c_str());
		cout << "new_operation.get_ending_line_number() => " << new_operation.get_ending_line_number() << endl;
		break;
	}
	cout << "---------------[new operation]----------------" << endl;
	cout << "id : " << new_operation.get_id_performed_clnt() << endl;
	cout << "mode :" << new_operation.get_mode() << endl;
	cout << "starting_line :" << new_operation.get_starting_line_number() << endl;
	cout << "work_zone :" << new_operation.get_work_zone_name() << endl;
	cout << "message :" << new_operation.get_message() << endl;
	cout << "----------------------------------------------" << endl;
}

//================================================
//             		공용 함수
//================================================

// 클라이언트 연결 끊김 처리
void Server::Client_Disconnect(int sock, int type)
{
	if (type == TYPE::EDIT)
	{
		cout << "[ EditForm 클라 OUT!! / 소켓 : " << sock << "]" << endl;
		// 해당 클라이언트 관련 소켓들 정리
		edit_mtx.lock();
		int idx = Get_VectorIndex(sock);
		close(editor[idx].operation_sock);
		close(editor[idx].acknowledge_sock);
		close(editor[idx].cursor_sock);
		Erase_VectorValue(idx);
		edit_mtx.unlock();
	}
	close(sock);
}

// String Split 함수
vector<string> Server::Split(string input, char delimiter)
{
	vector<string> answer;
	stringstream ss(input);
	string temp;

	while (getline(ss, temp, delimiter))
	{
		answer.push_back(temp);
	}
	return answer;
}

// 오류 메시지
void Server::Error_Handling(string msg)
{
	cout << msg << endl;
	exit(1);
}
