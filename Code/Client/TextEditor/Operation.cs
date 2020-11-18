using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Runtime.InteropServices;

namespace TextEditor
{
    public class Operation
    {
        public int id_performed_clnt;
        public int mode;
        public int starting_line_number;
        public int starting_index_number;
        public int ending_line_number;
        public int ending_index_number;
        public int deleteCount;
        public string deletemessage;
        public string work_zone_name;
        public string message;

        // 속성
        public int DeleteCount
        {
            get { return deleteCount; }
            set { deleteCount = value; }
        }
        public string DeleteMessage
        {
            get { return deletemessage; }
            set { deletemessage = value; }
        }
        public int Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        public int Starting_Line_Number
        {
            get { return starting_line_number; }
            set { starting_line_number = value; }
        }

        public int Starting_Index_Number
        {
            get { return starting_index_number; }
            set { starting_index_number = value; }
        }

        public int Ending_Line_Number
        {
            get { return ending_line_number; }
            set { ending_line_number = value; }
        }

        public int Ending_Index_Number
        {
            get { return ending_index_number; }
            set { ending_index_number = value; }
        }

        public string WorkZoneName
        {
            get { return work_zone_name; }
            set { work_zone_name = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public Operation()
        {
            InitializeOperation();
        }

        public Operation(string file, int clnt_id)
        {
            InitializeOperation(file, clnt_id);
        }

        public Operation DeepCopy()
        {
            Operation newCopy = new Operation();
            newCopy.id_performed_clnt = this.id_performed_clnt;
            newCopy.mode = this.mode;
            newCopy.starting_index_number = this.starting_index_number;
            newCopy.starting_line_number = this.starting_line_number;
            newCopy.ending_index_number = this.ending_index_number;
            newCopy.ending_line_number = this.ending_line_number;
            newCopy.deleteCount = this.deleteCount;
            newCopy.deletemessage = this.deletemessage;
            newCopy.work_zone_name = this.work_zone_name;
            newCopy.message = this.message;
            return newCopy;
        }

        public void InitializeOperation()
        {
            // Operation 클래스 초기화
            mode = 0;
            starting_line_number = 0;
            starting_index_number = 0;
            ending_line_number = 0;
            ending_index_number = 0;
            deleteCount = 0;
            deletemessage = null;
            message = null;
        }

        public void InitializeOperation(string file, int clnt_id)
        {
            // Operation 클래스 초기화
            id_performed_clnt = clnt_id;
            mode = 0;
            starting_line_number = 0;
            starting_index_number = 0;
            ending_line_number = 0;
            ending_index_number = 0;
            deleteCount = 0;
            deletemessage = null;
            work_zone_name = file;
            message = null;
        }

        public void InitializeOperation(int line_number, int index_number)
        {
            mode = 0;
            starting_line_number = line_number;
            starting_index_number = index_number;
            ending_line_number = 0;
            ending_index_number = 0;
            deleteCount = 0;
            deletemessage = null;
            message = null;
        }
    }
}
