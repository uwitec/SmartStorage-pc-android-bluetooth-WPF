using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace RFIDBackground
{
    public class StorageDB
    {
        private DataSet dataSet;
        private String connectionString = Properties.Settings.Default.db_StorageConnectionString;
        private SqlConnection connection;
        private SqlCommand command;
        private SqlDataAdapter adapter;
        public DataTable DetailedRegisterTable
        {
            get
            {
                return dataSet.Tables["DetailedRegisterTable"];
            }
        }

        public StorageDB()
        {
            dataSet = new DataSet();
            connection = new SqlConnection(connectionString);
            command = new SqlCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.Connection = connection;
            command.CommandTimeout = 15;
            adapter = new SqlDataAdapter();
            adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            dataSet.Tables.Add("DetailedRegisterTable");
            GetTable("DetailedRegisterTable");
        }

        public void GetTable(String TableName)
        {
            try
            {
                switch (TableName)
                {
                    case "DetailedRegisterTable":
                        command.CommandText = "GetDetailedRegisterTableProcedure";
                        adapter.SelectCommand = command;
                        adapter.Fill(dataSet, "DetailedRegisterTable");
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }
            finally
            {
                
            }
        }
    }
}
