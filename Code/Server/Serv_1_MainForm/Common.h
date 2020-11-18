#pragma once
#include <iostream>
#include <string>
#include <cstring>
#include <unistd.h>
#include <arpa/inet.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <sys/types.h>
#include <fstream>
#include <dirent.h>
#include <thread>
#include <mutex>
#include <vector>

#define BUF 256
#define PORT 9555 
#define DIR_PATH "../TextFiles/"
#define START_PROC_PORT 9556
using namespace std;

namespace TYPE // 소켓 타입 
{    
    enum { EDIT_SERV, PROC_INFO, MAIN, MAIN_READ }; 
}

namespace PROC_SIGNAL
{
    enum { CREATE, CREATE_SUCCESS, CREATE_FAIL, TERMINATE };
}

namespace MAIN_SIGNAL 
{
    enum { CREATE_FILE, DELETE_FILE, PROC_CHECK };
}

namespace FILE_CHANGED
{
    enum { SUCCESS = 1, FAIL };
}
