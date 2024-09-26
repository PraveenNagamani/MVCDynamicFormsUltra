

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace MVCDynamicFormsUltra.DBContext
{
    public class DBConnect
    {

        public DBConnect()
        {
        }

        public class OrclParameters
        {
            public string paramname;
            public object paramvalue;
            public OracleDbType type;

            public OrclParameters(string paramname, object paramvalue, OracleDbType type)
            {
                this.paramname = paramname;
                this.paramvalue = paramvalue;
                this.type = type;
            }
        }
        public DataTable GetDatatable(string query, string connstring, ref string ErrMsg, string[,] sqlparams, bool validatedata = false)
        {
            if (query == "") { return null; }
            updatequeryparams(ref query, sqlparams);
            DataTable dt = new DataTable();
            OracleConnection conn = new OracleConnection(connstring);
            try
            {

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                OracleDataAdapter da = new OracleDataAdapter(query, conn);

                da.Fill(dt);
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
            catch (Exception e)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
                ErrMsg += e.Message;
            }

            if (validatedata)
            {
                if (!validatedatatable(dt)) {
                    ErrMsg = "No Data Available"; 
                }
            }
            return dt;
        }

        public bool validatedatatable(DataTable dt)
        {
            if (dt == null) {  return false; }
            if(dt.Rows.Count == 0) { return false; }
            return true;
        }

        public string GetNoSQLBson(string Connstring, string TableName, ref string ErrMsg, string[,] filterparams = null, string DBName = "local")
        {

            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> combinedFilter = filterBuilder.Empty;

            for (int i = 0; i < filterparams.GetLength(0); i++)
            {
                var field = filterparams[i, 0];
                var op = filterparams[i, 1];
                var value = filterparams[i, 2];

                // Parse the value to the appropriate BSON type if necessary
                BsonValue bsonValue;
                if (int.TryParse(value, out int intValue))
                {
                    bsonValue = intValue;
                }
                else if (bool.TryParse(value, out bool boolValue))
                {
                    bsonValue = boolValue;
                }
                else
                {
                    bsonValue = value;
                }

                FilterDefinition<BsonDocument> filter = op switch
                {
                    "Eq" => filterBuilder.Eq(field, bsonValue),
                    "Ne" => filterBuilder.Ne(field, bsonValue),
                    "Gt" => filterBuilder.Gt(field, bsonValue),
                    "Gte" => filterBuilder.Gte(field, bsonValue),
                    "Lt" => filterBuilder.Lt(field, bsonValue),
                    "Lte" => filterBuilder.Lte(field, bsonValue),
                    _ => throw new ArgumentException($"Unknown operator: {op}")
                };

                combinedFilter = combinedFilter & filter;
            }


            if (Connstring == "") Connstring = "mongodb://localhost:27017/";
            MongoClient conn = new MongoClient(Connstring);
            IMongoDatabase db = conn.GetDatabase(DBName);
            try
            {

                var resultlist = db.GetCollection<BsonDocument>(TableName);

                BsonDocument doc = resultlist.Find(combinedFilter).FirstOrDefault();
                if (doc != null)
                {
                    string bsonresult = doc.ToString();
                    return bsonresult;
                }
                else
                {
                    ErrMsg = "No document found";
                    return "";
                }

            }
            catch (Exception e)
            {

                ErrMsg += e.Message;
            }

            return "";
        }

        public string BindDropDown(string query, string Orclconnstring ,string[,] sqlparams, ref string ErrMsg, string selectedval = "", DataTable dt = null)
        {
            if(!string.IsNullOrEmpty(ErrMsg)) { return ""; }
            if(!validatedatatable(dt))
            {
                if(string.IsNullOrEmpty(query))
                {
                    return "";
                }
                else if(query.ToLower().StartsWith("select") || query.ToLower().StartsWith("with"))
                {
                    dt = GetDatatable(query, Orclconnstring, ref ErrMsg, sqlparams, true);
                }
                else
                {
                    dt = new DataTable();
                    dt.Columns.Add("Text"); dt.Columns.Add("Value");

                    if (query.Contains(","))
                    {

                        foreach (string val in query.Split(","))
                        {
                            if (val.Contains(":"))
                            {
                                string text = "", dval = "";
                                var parts = val.Split(":");
                                if (parts.Length > 0) { text = parts[0].Trim(); }
                                if (parts.Length > 1) { dval = parts[1].Trim(); }
                                dt.Rows.Add(text,dval);
                               
                            }
                            else
                            {
                                dt.Rows.Add(val, val);
                            }
                        }
                        
                        
                    }
                    else
                    {
                        if (query.Contains(":"))
                        {
                            string text = "", dval = "";
                            var parts = query.Split(":");
                            if (parts.Length > 0) { text = parts[0].Trim(); }
                            if (parts.Length > 1) { dval = parts[1].Trim(); }
                            dt.Rows.Add(text, dval);

                        }
                        else
                        {
                            dt.Rows.Add(query, query);
                        }
                    }
                }
                
            }
            
            
            if (ErrMsg != "") { return ""; }
            string result = "";
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Columns.Count == 2)
                {
                    if (!selectedval.Equals(dt.Rows[i][1].ToString())) 
                    { 
                        result += dt.Rows[i][0].ToString() + ":" + dt.Rows[i][1].ToString() + ",";
                    }
                    else { 
                        result += dt.Rows[i][0].ToString() + ":" + dt.Rows[i][1].ToString() + ":" + (selectedval.Equals(dt.Rows[i][1].ToString()) ? "1" : "") + ","; 
                    }
                }
                else if (dt.Columns.Count == 3)
                {
                    if (!selectedval.Equals(dt.Rows[i][1].ToString()))
                    {
                        result += dt.Rows[i][0].ToString() + ":" + dt.Rows[i][1].ToString() + ":" + dt.Rows[i][2].ToString() + ",";
                    }
                    else
                    {
                        result += dt.Rows[i][0].ToString() + ":" + dt.Rows[i][1].ToString() + ":" + (selectedval.Equals(dt.Rows[i][1].ToString()) ? "1" : "") + ",";
                    }
                }
                else
                {
                    result += dt.Rows[i][0].ToString() + ",";
                }

            }

            return (result.EndsWith(",") ? result.Remove(result.Length - 1) : result);

        }

        public void BindTextBox(DataTable dt, ref String BindValue, ref String ErrMsg)
        {
            BindValue = (String)dt.Rows[0][0];
        }
        public void BindTextBox(String SourceValue, ref String BindValue, ref String ErrMsg)
        {
            BindValue = SourceValue;
        }

        public int ExecuteNonQuery(string query, string connectionString, string[,] sqlparams, ref string ErrMsg)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        updatequeryparams(ref query, sqlparams);
                        command.CommandText = query;
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected;
                    }
                }
            }
            catch (Exception e)
            {
                ErrMsg += e.Message;
            }
            return 0;

        }

        public class ExecuteNonQueryAsyncResult
        {
            public int rowsAffected;
            public string ErrMsg;
        }

        public async Task<ExecuteNonQueryAsyncResult> ExecuteNonQueryAsync(string query, string connectionString,string[,] stringparams, OracleParameter[] sqlparams,  string ErrMsg)
        {
            updatequeryparams(ref query, stringparams);
            try
            {
                using (OracleConnection connection = new OracleConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        if(sqlparams != null && sqlparams.Length > 0)
                        {
                            command.Parameters.AddRange(sqlparams);
                        }

                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return new ExecuteNonQueryAsyncResult { rowsAffected = rowsAffected, ErrMsg = "" };
                    }
                }
            }
            catch (Exception e)
            {
                ErrMsg += e.Message;
            }
            

            return new ExecuteNonQueryAsyncResult { rowsAffected = 0, ErrMsg = ErrMsg };
            
        }

        public string ExecuteScalar(string query, string connectionString, string[,] sqlparams, ref string ErrMsg)
        {
            string result = "";
            try
            {
                using (OracleConnection connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        updatequeryparams(ref query, sqlparams);
                        command.CommandText = query;
                        result = (string)command.ExecuteScalar();

                    }
                }
            }
            catch (Exception e)
            {
                ErrMsg += e.Message;
            }
            return result;

        }

        public void updatequeryparams(ref string query, string[,] sqlparams)
        {
            if (sqlparams != null)
            {
                for (int i = 0; i < sqlparams.GetLength(0); i++)
                {
                    if (sqlparams[i, 0] == null) { continue; }
                    query = query.Replace(sqlparams[i, 0], sqlparams[i, 1]);
                }
            }
        }

        public string[,] prepareparamsbyarg(ref string ErrMsg, params string[] args)
        {
            string[,] sqlparams = new string[args.Length + 1, 2];
            try
            {
                for (int i = 0; i < args.Length; i = i + 2)
                {
                    sqlparams[i, 0] = args[i];
                    sqlparams[i, 1] = args[i + 1];
                }
            }
            catch (Exception e) { ErrMsg += e.Message; }


            return sqlparams;
        }

        public string[,] Appendparamsbyarg(string[,] MainParams ,ref string ErrMsg, params string[] args)
        {
            string[,] sqlparams = new string[args.Length + 1, 2];
            sqlparams = prepareparamsbyarg(ref ErrMsg, args);
            string[,] Mergedparams = MergeParams(MainParams, sqlparams, ref ErrMsg);

            return Mergedparams;
        }

        public string[,] MergeParams( string[,] RootParams, string[,] ChildParams, ref string ErrMsg)
        {
            // not working
            string[,] SQLParamsTemp = new string[(RootParams.Length + ChildParams.Length), 3];
            int j = 0, k = 0;
            for (int i = 0; i < SQLParamsTemp.Length; i++)
            {
                if (j == RootParams.GetUpperBound(0))
                {
                    if (RootParams[j, 0] != null)
                    {
                        SQLParamsTemp[i, 0] = RootParams[j, 0].ToString();

                        SQLParamsTemp[i, 1] = RootParams[j, 1].ToString();
                        j++;
                    }
                }

                else if (k == ChildParams.GetUpperBound(0))
                {
                    if (ChildParams[k, 0] != null)
                    {
                        SQLParamsTemp[i, 0] = ChildParams[k, 0].ToString();

                        SQLParamsTemp[i, 1] = ChildParams[k, 1].ToString();
                        k++;
                    }

                }

            }

            RootParams = SQLParamsTemp;
            return RootParams;

        }


        public string[,] prepareparmasbydictionary(ref string ErrMsg, Dictionary<string, string> dictparams)
        {
            string[,] sqlparams = new string[dictparams.Keys.Count + 1, 2];
            try
            {
                int i= 0;
                foreach(KeyValuePair<string,string> kvp in dictparams)
                {
                    sqlparams[i,0] = kvp.Key;
                    sqlparams[i,1] = kvp.Value;
                    i++;
                }

            }
            catch (Exception e) { ErrMsg += e.Message; }


            return sqlparams;
        }

        public void addByteArrayToParam(ref string ErrMsg, ref List<OracleParameter> sqlParams, string paramName, byte[] paramValue)
        {
            if (paramValue != null)
            {
                // Create a new OracleParameter for the byte array (BLOB)
                OracleParameter byteArrayParam = new OracleParameter
                {
                    ParameterName = paramName,
                    OracleDbType = OracleDbType.Blob,  // Specify that this is a BLOB type
                    Value = paramValue  // Set the byte array as the parameter value
                };

                // Add the byte array parameter to the parameter list
                sqlParams.Add(byteArrayParam);
            }
        }
    }
}

