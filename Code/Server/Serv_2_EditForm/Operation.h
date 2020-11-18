#pragma once
#include "Common.h"

class Operation
{
private:
    int id_performed_clnt;
    int mode;
    int starting_line_number;
    int starting_index_number;
    int ending_line_number;
    int ending_index_number;
    int startingLength;
    char deletemessage[100];
    char work_zone_name[50];
    char message[100];
public:
    Operation(int clnt_id) { id_performed_clnt=clnt_id; mode = 0; starting_line_number=0; starting_index_number=0; ending_line_number=0; ending_index_number=0; startingLength=0; memset(deletemessage,0,sizeof(char)*100); memset(work_zone_name,0,sizeof(char)*50); memset(message,0,sizeof(char)*100);}
    Operation() { id_performed_clnt = 0; mode = 0; starting_line_number=0; starting_index_number=0; ending_line_number=0; ending_index_number=0; startingLength=0; memset(deletemessage,0,sizeof(char)*100); memset(work_zone_name,0,sizeof(char)*50); memset(message,0,sizeof(char)*100);}
    int get_id_performed_clnt() { return id_performed_clnt; }
    char* get_work_zone_name() { return work_zone_name; }
    int get_mode() { return mode; }
    int get_starting_line_number() { return starting_line_number; }
    int get_starting_index_number() { return starting_index_number; }
    int get_ending_line_number() { return ending_line_number; }
    int get_ending_index_number() { return ending_index_number; }
    char* get_message() { return message; }
    int get_starting_length() { return startingLength; }
    char* get_deletemessage() { return deletemessage; }

    void init_workzone_name() {memset(this->work_zone_name,0,sizeof(char)*50);}
    void set_id_performed_clnt(int clnt_id) { id_performed_clnt = clnt_id; }
    void set_work_zone_name(const char* name) { strcpy(this->work_zone_name,name); }
    void set_mode(int mode) { this->mode = mode; }
    void set_starting_line_number(int line_number) { this->starting_line_number = line_number; }
    void set_starting_index_number(int index_number) { this->starting_index_number = index_number; }
    void set_ending_line_number(int line_number) { this->ending_line_number = line_number; }
    void set_ending_index_number(int index_number) { this->ending_index_number = index_number;}
    void set_message(const char* message) { strcpy(this->message,message); } 
    void set_starting_length(int length) { startingLength = length;}
    void set_deletemessage(const char* deletemessage) { strcpy(this->deletemessage,deletemessage); } 

    Operation DeepCopy()
    {
        Operation *newCopy = new Operation();
        newCopy->id_performed_clnt = id_performed_clnt;
        newCopy->mode = mode;
        newCopy->starting_index_number = starting_index_number;
        newCopy->starting_line_number = starting_line_number;
        newCopy->ending_index_number = ending_index_number;
        newCopy->ending_line_number = ending_line_number;
        newCopy->startingLength = startingLength;
        strcpy(newCopy->deletemessage,deletemessage);
        strcpy(newCopy->work_zone_name,work_zone_name);
        strcpy(newCopy->message,message);
        return *newCopy;
    }
};